package com.sandbox;

import com.sandbox.cli.CommandLineInterface;
import com.sandbox.cli.ColorFormatter;
import com.sandbox.core.*;
import com.sandbox.concurrency.ConcurrentExecutor;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;

/**
 * Main entry point for the Java Sandbox System.
 * Supports interactive mode, command execution, and batch mode.
 */
public class SandboxApplication {

    public static void main(String[] args) {
        SandboxConfig config = new SandboxConfig();
        List<String> positionalArgs = new ArrayList<>();
        boolean batchMode = false;
        boolean testMode = false;
        File batchFile = null;
        String runCommand = null;
        String runScript = null;
        String runScriptLang = null;
        String workDir = null;
        long timeout = -1;

        // Parse arguments
        for (int i = 0; i < args.length; i++) {
            switch (args[i]) {
                case "--batch": case "-b":
                    batchMode = true;
                    if (i + 1 < args.length) batchFile = new File(args[++i]);
                    break;
                case "--test": case "-t":
                    testMode = true;
                    break;
                case "--run": case "-r":
                    if (i + 1 < args.length) runCommand = args[++i];
                    break;
                case "--script": case "-s":
                    if (i + 2 < args.length) {
                        runScript = args[++i];
                        runScriptLang = args[++i];
                    } else if (i + 1 < args.length) {
                        runScript = args[++i];
                        runScriptLang = "bash";
                    }
                    break;
                case "--dir": case "-d":
                    if (i + 1 < args.length) workDir = args[++i];
                    break;
                case "--timeout":
                    if (i + 1 < args.length) timeout = Long.parseLong(args[++i]);
                    break;
                case "--no-color":
                    ColorFormatter.setNoColor(true);
                    break;
                case "--max-concurrent":
                    if (i + 1 < args.length) config.maxConcurrentProcesses(Integer.parseInt(args[++i]));
                    break;
                case "--help": case "-h":
                    printUsage();
                    return;
                default:
                    positionalArgs.add(args[i]);
            }
        }

        if (workDir != null) config.workingDirectory(new File(workDir));
        if (timeout > 0) config.defaultTimeout(timeout, TimeUnit.SECONDS);

        SandboxEngine engine = new SandboxEngine(config);

        // Handle modes
        if (testMode) {
            runTests(engine);
            engine.shutdown();
            return;
        }

        if (runCommand != null) {
            ExecutionResult result = engine.run(runCommand);
            System.out.print(result.getOutput());
            System.exit(result.getExitCode());
            engine.shutdown();
            return;
        }

        if (runScript != null) {
            ExecutionResult result = engine.runScript(runScriptLang, runScript);
            System.out.print(result.getOutput());
            System.exit(result.getExitCode());
            engine.shutdown();
            return;
        }

        if (batchMode && batchFile != null) {
            runBatch(engine, batchFile);
            engine.shutdown();
            return;
        }

        if (!positionalArgs.isEmpty()) {
            // Run positional args as commands
            if (positionalArgs.size() == 1) {
                ExecutionResult result = engine.run(positionalArgs.get(0));
                System.out.print(result.getOutput());
                System.exit(result.getExitCode());
            } else {
                Map<String, ExecutionResult> results = engine.runNamed(
                    positionalArgs.stream().collect(
                        java.util.stream.Collectors.toMap(
                            cmd -> cmd.substring(0, Math.min(20, cmd.length())),
                            cmd -> cmd, (a, b) -> b, LinkedHashMap::new
                        )
                    )
                );
                for (Map.Entry<String, ExecutionResult> entry : results.entrySet()) {
                    String status = entry.getValue().isSuccess() ? "OK" : "FAIL";
                    System.out.println("[" + status + "] " + entry.getKey() + " (" + entry.getValue().getExecutionTimeMs() + "ms)");
                    if (!entry.getValue().isSuccess()) {
                        System.out.println("  " + entry.getValue().getStderr());
                    }
                }
            }
            engine.shutdown();
            return;
        }

        // Interactive mode
        CommandLineInterface cli = new CommandLineInterface(engine);
        cli.start();
    }

    private static void runBatch(SandboxEngine engine, File batchFile) {
        try {
            List<String> lines = Files.readAllLines(batchFile.toPath(), StandardCharsets.UTF_8);
            int passed = 0, failed = 0;
            long totalStart = System.currentTimeMillis();

            for (int i = 0; i < lines.size(); i++) {
                String line = lines.get(i).trim();
                if (line.isEmpty() || line.startsWith("#")) continue;

                System.out.println(ColorFormatter.cyan("[" + (i + 1) + "/" + lines.size() + "] " + line));
                ExecutionResult result = engine.run(line);

                if (result.isSuccess()) {
                    passed++;
                    System.out.println(ColorFormatter.success("  PASSED") + ColorFormatter.dim(" (" + result.getExecutionTimeMs() + "ms)"));
                } else {
                    failed++;
                    System.out.println(ColorFormatter.error("  FAILED") + ColorFormatter.dim(" (" + result.getExitCode() + ", " + result.getExecutionTimeMs() + "ms)"));
                    if (result.getStderr() != null && !result.getStderr().isEmpty()) {
                        System.out.println(ColorFormatter.dim("  > " + result.getStderr().trim()));
                    }
                }
            }

            long totalMs = System.currentTimeMillis() - totalStart;
            System.out.println(ColorFormatter.header("\n=== Batch Results ==="));
            System.out.println("  Total: " + lines.size() + " | " + ColorFormatter.success("Passed: " + passed) +
                    " | " + ColorFormatter.error("Failed: " + failed) + " | Time: " + totalMs + "ms");
        } catch (IOException e) {
            System.out.println(ColorFormatter.error("Error reading batch file: " + e.getMessage()));
        }
    }

    private static void runTests(SandboxEngine engine) {
        System.out.println(ColorFormatter.header("=== Running Sandbox Tests ===\n"));
        int passed = 0, failed = 0;
        List<String> results = new ArrayList<>();

        // Test 1: Basic command execution
        ExecutionResult r1 = engine.run("echo hello");
        boolean t1 = r1.isSuccess() && r1.getStdout().contains("hello");
        results.add((t1 ? "PASS" : "FAIL") + ": Basic command execution");
        if (t1) passed++; else failed++;

        // Test 2: File creation
        try {
            engine.createFile("test_file.txt", "test content");
            String content = engine.readFile("test_file.txt");
            boolean t2 = "test content".equals(content);
            results.add((t2 ? "PASS" : "FAIL") + ": File create/read");
            if (t2) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: File create/read - " + e.getMessage());
            failed++;
        }

        // Test 3: File append
        try {
            engine.appendFile("test_file.txt", " appended");
            String content = engine.readFile("test_file.txt");
            boolean t3 = content.equals("test content appended");
            results.add((t3 ? "PASS" : "FAIL") + ": File append");
            if (t3) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: File append - " + e.getMessage());
            failed++;
        }

        // Test 4: File listing
        try {
            List<String> files = engine.listFiles(".");
            boolean t4 = files.contains("test_file.txt");
            results.add((t4 ? "PASS" : "FAIL") + ": File listing");
            if (t4) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: File listing - " + e.getMessage());
            failed++;
        }

        // Test 5: Directory creation
        try {
            engine.createDirectory("test_dir/sub_dir");
            boolean t5 = engine.fileExists("test_dir/sub_dir");
            results.add((t5 ? "PASS" : "FAIL") + ": Directory creation");
            if (t5) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: Directory creation - " + e.getMessage());
            failed++;
        }

        // Test 6: Tool execution
        ExecutionResult r6 = engine.useTool("echo", "tool test");
        boolean t6 = r6.isSuccess() && r6.getStdout().contains("tool test");
        results.add((t6 ? "PASS" : "FAIL") + ": Tool execution (echo)");
        if (t6) passed++; else failed++;

        // Test 7: UUID generation tool
        ExecutionResult r7 = engine.useTool("uuid");
        boolean t7 = r7.isSuccess() && r7.getStdout().contains("-");
        results.add((t7 ? "PASS" : "FAIL") + ": Tool execution (uuid)");
        if (t7) passed++; else failed++;

        // Test 8: Base64 encoding
        ExecutionResult r8 = engine.useTool("base64", "encode", "hello");
        boolean t8 = r8.isSuccess() && r8.getStdout().trim().equals("aGVsbG8=");
        results.add((t8 ? "PASS" : "FAIL") + ": Base64 encode");
        if (t8) passed++; else failed++;

        // Test 9: MD5 hash
        ExecutionResult r9 = engine.useTool("md5", "hello");
        boolean t9 = r9.isSuccess() && r9.getStdout().trim().equals("5d41402abc4b2a76b9719d911017c592");
        results.add((t9 ? "PASS" : "FAIL") + ": MD5 hash");
        if (t9) passed++; else failed++;

        // Test 10: Concurrent execution
        try {
            Map<String, ExecutionResult> results10 = engine.runNamed(Map.of(
                "cmd1", "echo one",
                "cmd2", "echo two",
                "cmd3", "echo three"
            ));
            boolean t10 = results10.size() == 3 &&
                    results10.values().stream().allMatch(ExecutionResult::isSuccess);
            results.add((t10 ? "PASS" : "FAIL") + ": Concurrent execution");
            if (t10) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: Concurrent execution - " + e.getMessage());
            failed++;
        }

        // Test 11: Script execution (bash)
        ExecutionResult r11 = engine.runScript("bash", "echo 'script works'");
        boolean t11 = r11.isSuccess() && r11.getStdout().contains("script works");
        results.add((t11 ? "PASS" : "FAIL") + ": Script execution (bash)");
        if (t11) passed++; else failed++;

        // Test 12: Timeout handling
        ExecutionResult r12 = engine.run("sleep 10", 1, TimeUnit.SECONDS);
        boolean t12 = r12.isTimedOut();
        results.add((t12 ? "PASS" : "FAIL") + ": Timeout handling");
        if (t12) passed++; else failed++;

        // Test 13: Status
        Map<String, Object> status = engine.getStatus();
        boolean t13 = status.containsKey("sessionId") && status.containsKey("uptimeMs");
        results.add((t13 ? "PASS" : "FAIL") + ": Status reporting");
        if (t13) passed++; else failed++;

        // Test 14: File deletion
        try {
            engine.deleteFile("test_file.txt");
            boolean t14 = !engine.fileExists("test_file.txt");
            results.add((t14 ? "PASS" : "FAIL") + ": File deletion");
            if (t14) passed++; else failed++;
        } catch (Exception e) {
            results.add("FAIL: File deletion - " + e.getMessage());
            failed++;
        }

        // Test 15: Hex dump
        ExecutionResult r15 = engine.useTool("hexdump", "AB");
        boolean t15 = r15.isSuccess() && r15.getStdout().contains("41");
        results.add((t15 ? "PASS" : "FAIL") + ": Hex dump");
        if (t15) passed++; else failed++;

        // Print results
        for (String r : results) {
            if (r.startsWith("PASS")) System.out.println(ColorFormatter.success("  [PASS] " + r.substring(5)));
            else System.out.println(ColorFormatter.error("  [FAIL] " + r.substring(5)));
        }

        System.out.println(ColorFormatter.header("\n=== Test Results ==="));
        System.out.println("  " + ColorFormatter.success("PASSED: " + passed) + " / " + ColorFormatter.error("FAILED: " + failed) + " / TOTAL: " + (passed + failed));
        System.out.println("  Session: " + engine.getSessionId());
        System.out.println("  Uptime: " + engine.getUptimeMs() + "ms");

        if (failed > 0) System.exit(1);
    }

    private static void printUsage() {
        System.out.println(ColorFormatter.header("Java Sandbox System v2.0"));
        System.out.println("""
  A high-quality sandbox environment with internet access, file creation,
  command execution, and concurrent script running capabilities.

  USAGE:
    java -jar java-sandbox-all.jar [OPTIONS] [COMMANDS...]

  MODES:
    (no args)              Interactive REPL mode
    --run, -r <cmd>        Run a single command and exit
    --script, -s <code> <lang>  Run a script and exit
    --batch, -b <file>     Run commands from a batch file
    --test, -t             Run built-in test suite
    <cmd1> <cmd2> ...      Run multiple commands and exit

  OPTIONS:
    --dir, -d <path>       Set working directory
    --timeout <sec>        Set default timeout in seconds
    --max-concurrent <n>   Set max concurrent processes
    --no-color             Disable colored output
    --help, -h             Show this help

  EXAMPLES:
    java -jar java-sandbox-all.jar                    # Interactive mode
    java -jar java-sandbox-all.jar --run "ls -la"     # Single command
    java -jar java-sandbox-all.jar --test               # Run tests
    java -jar java-sandbox-all.jar --batch batch.txt   # Batch mode
    java -jar java-sandbox-all.jar --dir /tmp --run "pwd"
    java -jar java-sandbox-all.jar "echo hi" "date"    # Multiple commands
""");
    }
}
