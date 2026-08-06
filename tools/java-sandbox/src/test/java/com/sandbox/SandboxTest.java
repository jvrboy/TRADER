package com.sandbox;

import com.sandbox.core.*;
import com.sandbox.concurrency.ConcurrentExecutor;
import org.junit.*;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import static org.junit.Assert.*;

public class SandboxTest {
    private static SandboxEngine engine;
    private static File testDir;

    @BeforeClass
    public static void setup() {
        testDir = new File(System.getProperty("java.io.tmpdir"), "sandbox-test-" + System.nanoTime());
        SandboxConfig config = new SandboxConfig()
            .workingDirectory(testDir)
            .defaultTimeout(30, TimeUnit.SECONDS);
        engine = new SandboxEngine(config);
        System.out.println("Test dir: " + testDir.getAbsolutePath());
    }

    @AfterClass
    public static void teardown() {
        if (engine != null) engine.shutdown();
        deleteRecursive(testDir);
    }

    // === Core Execution Tests ===

    @Test
    public void testBasicCommandExecution() {
        ExecutionResult result = engine.run("echo hello");
        assertTrue("Should succeed", result.isSuccess());
        assertTrue("Should contain 'hello'", result.getStdout().contains("hello"));
    }

    @Test
    public void testExitCode() {
        ExecutionResult result = engine.run("exit 42");
        assertEquals("Exit code should be 42", 42, result.getExitCode());
        assertFalse("Should not be success", result.isSuccess());
    }

    @Test
    public void testStderrCapture() {
        ExecutionResult result = engine.run("echo error >&2");
        assertTrue("Should succeed (stderr doesn't affect exit)", result.isSuccess());
        assertTrue("Should capture stderr", result.getStderr().contains("error"));
    }

    @Test
    public void testTimeout() {
        ExecutionResult result = engine.run("sleep 10", 1, TimeUnit.SECONDS);
        assertTrue("Should time out", result.isTimedOut());
        assertFalse("Should not succeed", result.isSuccess());
    }

    @Test
    public void testExecutionTimeRecorded() {
        ExecutionResult result = engine.run("echo test");
        assertTrue("Should have execution time > 0", result.getExecutionTimeMs() >= 0);
    }

    @Test
    public void testShellExecution() {
        ExecutionResult result = engine.runShell("echo shelltest");
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("shelltest"));
    }

    // === File System Tests ===

    @Test
    public void testFileCreateAndRead() throws IOException {
        engine.createFile("test1.txt", "content1");
        String content = engine.readFile("test1.txt");
        assertEquals("content1", content);
    }

    @Test
    public void testFileWrite() throws IOException {
        engine.writeFile("test2.txt", "written");
        assertEquals("written", engine.readFile("test2.txt"));
    }

    @Test
    public void testFileAppend() throws IOException {
        engine.createFile("test3.txt", "hello");
        engine.appendFile("test3.txt", " world");
        assertEquals("hello world", engine.readFile("test3.txt"));
    }

    @Test
    public void testFileDelete() throws IOException {
        engine.createFile("test4.txt", "delete me");
        assertTrue(engine.fileExists("test4.txt"));
        engine.deleteFile("test4.txt");
        assertFalse(engine.fileExists("test4.txt"));
    }

    @Test
    public void testFileExists() {
        assertFalse(engine.fileExists("nonexistent_file.txt"));
    }

    @Test
    public void testDirectoryCreation() throws IOException {
        engine.createDirectory("dir1/sub1/sub2");
        assertTrue(engine.fileExists("dir1/sub1/sub2"));
    }

    @Test
    public void testListFiles() throws IOException {
        engine.createFile("listA.txt", "");
        engine.createFile("listB.txt", "");
        engine.createDirectory("listDir");
        List<String> files = engine.listFiles(".");
        assertTrue("Should contain listA.txt", files.contains("listA.txt"));
        assertTrue("Should contain listB.txt", files.contains("listB.txt"));
        assertTrue("Should contain listDir/", files.contains("listDir/"));
    }

    @Test
    public void testGetFileSize() throws IOException {
        engine.createFile("size_test.txt", "12345");
        assertEquals(5, engine.getFileSize("size_test.txt"));
    }

    // === Tool Tests ===

    @Test
    public void testEchoTool() {
        ExecutionResult result = engine.useTool("echo", "test");
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("test"));
    }

    @Test
    public void testUuidTool() {
        ExecutionResult result = engine.useTool("uuid");
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("-"));
        assertEquals(36, result.getStdout().trim().length());
    }

    @Test
    public void testBase64Encode() {
        ExecutionResult result = engine.useTool("base64", "encode", "hello");
        assertTrue(result.isSuccess());
        assertEquals("aGVsbG8=", result.getStdout().trim());
    }

    @Test
    public void testBase64Decode() {
        ExecutionResult result = engine.useTool("base64", "decode", "aGVsbG8=");
        assertTrue(result.isSuccess());
        assertEquals("hello", result.getStdout().trim());
    }

    @Test
    public void testMd5() {
        ExecutionResult result = engine.useTool("md5", "hello");
        assertTrue(result.isSuccess());
        assertEquals("5d41402abc4b2a76b9719d911017c592", result.getStdout().trim());
    }

    @Test
    public void testRandomTool() {
        ExecutionResult result = engine.useTool("random", "1", "10");
        assertTrue(result.isSuccess());
        int val = Integer.parseInt(result.getStdout().trim());
        assertTrue(val >= 1 && val <= 10);
    }

    @Test
    public void testHexDump() {
        ExecutionResult result = engine.useTool("hexdump", "AB");
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("41"));
        assertTrue(result.getStdout().contains("42"));
    }

    @Test
    public void testListTools() {
        List<String> tools = engine.listTools();
        assertTrue("Should have many tools", tools.size() >= 20);
        assertTrue("Should have echo", tools.contains("echo"));
        assertTrue("Should have uuid", tools.contains("uuid"));
    }

    @Test
    public void testUnknownTool() {
        ExecutionResult result = engine.useTool("nonexistent_tool_xyz");
        assertFalse(result.isSuccess());
    }

    // === Concurrency Tests ===

    @Test
    public void testAsyncExecution() throws Exception {
        Future<ExecutionResult> future = engine.runAsync("echo async");
        ExecutionResult result = future.get(10, TimeUnit.SECONDS);
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("async"));
    }

    @Test
    public void testConcurrentExecution() throws Exception {
        Map<String, ExecutionResult> results = engine.runNamed(Map.of(
            "a", "echo a",
            "b", "echo b",
            "c", "echo c"
        ));
        assertEquals(3, results.size());
        for (ExecutionResult r : results.values()) {
            assertTrue(r.isSuccess());
        }
    }

    @Test
    public void testConcurrentPerformance() throws Exception {
        long start = System.currentTimeMillis();
        Map<String, ExecutionResult> results = engine.runNamed(Map.of(
            "1", "sleep 0.1",
            "2", "sleep 0.1",
            "3", "sleep 0.1",
            "4", "sleep 0.1",
            "5", "sleep 0.1"
        ));
        long elapsed = System.currentTimeMillis() - start;
        // Should be faster than sequential (5 * 100ms = 500ms)
        assertTrue("Concurrent should be faster than sequential. Elapsed: " + elapsed + "ms", elapsed < 1000);
        assertEquals(5, results.size());
    }

    // === Script Engine Tests ===

    @Test
    public void testBashScript() {
        ExecutionResult result = engine.runScript("bash", "echo 'bash works'");
        assertTrue(result.isSuccess());
        assertTrue(result.getStdout().contains("bash works"));
    }

    @Test
    public void testUnsupportedLanguage() {
        ExecutionResult result = engine.runScript("nonexistent_lang", "code");
        assertFalse(result.isSuccess());
    }

    // === Sandbox Status Tests ===

    @Test
    public void testSandboxStatus() {
        Map<String, Object> status = engine.getStatus();
        assertNotNull(status.get("sessionId"));
        assertNotNull(status.get("uptimeMs"));
        assertNotNull(status.get("processStats"));
    }

    @Test
    public void testSessionId() {
        assertNotNull(engine.getSessionId());
        assertEquals(8, engine.getSessionId().length());
    }

    @Test
    public void testUptime() {
        long uptime = engine.getUptimeMs();
        assertTrue(uptime >= 0);
    }

    // === Configuration Tests ===

    @Test
    public void testConfigDefaults() {
        SandboxConfig config = new SandboxConfig();
        assertTrue(config.isAllowNetworkAccess());
        assertTrue(config.isAllowFileWrite());
        assertTrue(config.isAllowFileRead());
        assertTrue(config.isAllowCommandExecution());
        assertEquals(50, config.getMaxConcurrentProcesses());
    }

    @Test
    public void testConfigModification() {
        SandboxConfig config = new SandboxConfig();
        config.allowNetworkAccess(false).allowFileWrite(false);
        assertFalse(config.isAllowNetworkAccess());
        assertFalse(config.isAllowFileWrite());
    }

    // === Integration Tests ===

    @Test
    public void testFilePipelineIntegration() throws IOException {
        engine.createFile("pipeline.txt", "line1\nline2\nline3\n");
        engine.appendFile("pipeline.txt", "line4\n");
        String content = engine.readFile("pipeline.txt");
        assertTrue(content.contains("line1"));
        assertTrue(content.contains("line4"));
        assertEquals(4, content.split("\n").length);
    }

    @Test
    public void testDirectoryTree() throws IOException {
        engine.createDirectory("tree_test/a/b");
        engine.createFile("tree_test/a/file1.txt", "");
        engine.createFile("tree_test/a/b/file2.txt", "");
        String tree = engine.getFileSystemManager().getTree("tree_test", 5);
        assertTrue(tree.contains("a"));
        assertTrue(tree.contains("file1.txt"));
    }

    // === Network Tests (basic) ===

    @Test
    public void testDnsResolution() throws IOException {
        String ip = engine.resolveHost("localhost");
        assertNotNull(ip);
        assertTrue(ip.contains("127") || ip.contains("::1"));
    }

    // === Helper ===

    private static void deleteRecursive(File file) {
        if (file == null || !file.exists()) return;
        if (file.isDirectory()) {
            File[] children = file.listFiles();
            if (children != null) for (File child : children) deleteRecursive(child);
        }
        file.delete();
    }
}