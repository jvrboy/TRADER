package com.sandbox.core;

import com.sandbox.concurrency.ConcurrentExecutor;
import com.sandbox.filesystem.FileSystemManager;
import com.sandbox.network.NetworkManager;
import com.sandbox.scripting.ScriptEngine;
import com.sandbox.tools.ToolManager;

import java.io.File;
import java.io.IOException;
import java.util.*;
import java.util.concurrent.Future;

/**
 * Central sandbox engine that orchestrates all subsystems.
 * Provides a unified API for command execution, file operations,
 * network access, scripting, and tool management.
 */
public class SandboxEngine {

    private final SandboxConfig config;
    private final ProcessExecutor processExecutor;
    private final FileSystemManager fileSystemManager;
    private final NetworkManager networkManager;
    private final ScriptEngine scriptEngine;
    private final ToolManager toolManager;
    private final ConcurrentExecutor concurrentExecutor;
    private final String sessionId;
    private final long createdAt;

    public SandboxEngine() {
        this(new SandboxConfig());
    }

    public SandboxEngine(SandboxConfig config) {
        this.config = config;
        this.sessionId = UUID.randomUUID().toString().substring(0, 8);
        this.createdAt = System.currentTimeMillis();

        // Ensure working directory exists
        config.getWorkingDirectory().mkdirs();

        this.processExecutor = new ProcessExecutor(config);
        this.fileSystemManager = new FileSystemManager(config);
        this.networkManager = new NetworkManager(config);
        this.scriptEngine = new ScriptEngine(this);
        this.toolManager = new ToolManager(this);
        this.concurrentExecutor = new ConcurrentExecutor(config);
    }

    // === Command Execution ===

    public ExecutionResult run(String command) {
        return processExecutor.execute(command);
    }

    public ExecutionResult run(String command, long timeout, java.util.concurrent.TimeUnit unit) {
        return processExecutor.execute(command, timeout, unit);
    }

    public ExecutionResult runShell(String script) {
        return processExecutor.executeShell(script, config.getDefaultTimeout(), config.getTimeoutUnit());
    }

    public ExecutionResult runShell(String script, long timeout, java.util.concurrent.TimeUnit unit) {
        return processExecutor.executeShell(script, timeout, unit);
    }

    // === Concurrent Execution ===

    public Future<ExecutionResult> runAsync(String command) {
        return concurrentExecutor.submit(() -> processExecutor.execute(command));
    }

    public Future<ExecutionResult> runAsync(String command, long timeout, java.util.concurrent.TimeUnit unit) {
        return concurrentExecutor.submit(() -> processExecutor.execute(command, timeout, unit));
    }

    public List<ExecutionResult> runAll(List<String> commands) {
        List<Future<ExecutionResult>> futures = new ArrayList<>();
        for (String cmd : commands) {
            futures.add(concurrentExecutor.submit(() -> processExecutor.execute(cmd)));
        }
        List<ExecutionResult> results = new ArrayList<>();
        for (Future<ExecutionResult> f : futures) {
            try { results.add(f.get()); } catch (Exception e) {
                results.add(new ExecutionResult(-1, "", e.getMessage(), 0, "", false, e, -1));
            }
        }
        return results;
    }

    public Map<String, ExecutionResult> runNamed(Map<String, String> namedCommands) {
        Map<String, Future<ExecutionResult>> futures = new LinkedHashMap<>();
        for (Map.Entry<String, String> entry : namedCommands.entrySet()) {
            futures.put(entry.getKey(), concurrentExecutor.submit(
                    () -> processExecutor.execute(entry.getValue())));
        }
        Map<String, ExecutionResult> results = new LinkedHashMap<>();
        for (Map.Entry<String, Future<ExecutionResult>> entry : futures.entrySet()) {
            try {
                results.put(entry.getKey(), entry.getValue().get());
            } catch (Exception e) {
                results.put(entry.getKey(), new ExecutionResult(-1, "", e.getMessage(), 0,
                        namedCommands.get(entry.getKey()), false, e, -1));
            }
        }
        return results;
    }

    // === File System ===

    public String readFile(String path) throws IOException {
        return fileSystemManager.readFile(path);
    }

    public void writeFile(String path, String content) throws IOException {
        fileSystemManager.writeFile(path, content);
    }

    public void appendFile(String path, String content) throws IOException {
        fileSystemManager.appendFile(path, content);
    }

    public void createFile(String path, String content) throws IOException {
        fileSystemManager.createFile(path, content);
    }

    public void deleteFile(String path) throws IOException {
        fileSystemManager.deleteFile(path);
    }

    public boolean fileExists(String path) {
        return fileSystemManager.fileExists(path);
    }

    public List<String> listFiles(String dirPath) throws IOException {
        return fileSystemManager.listFiles(dirPath);
    }

    public void createDirectory(String path) throws IOException {
        fileSystemManager.createDirectory(path);
    }

    public long getFileSize(String path) throws IOException {
        return fileSystemManager.getFileSize(path);
    }

    // === Network ===

    public String httpGet(String url) throws IOException {
        return networkManager.httpGet(url);
    }

    public String httpPost(String url, String body) throws IOException {
        return networkManager.httpPost(url, body);
    }

    public void downloadFile(String url, String savePath) throws IOException {
        networkManager.downloadFile(url, savePath);
    }

    public String resolveHost(String hostname) throws IOException {
        return networkManager.resolveHost(hostname);
    }

    // === Scripting ===

    public ExecutionResult runScript(String language, String script) {
        return scriptEngine.execute(language, script);
    }

    public ExecutionResult runScriptFile(String filePath) {
        return scriptEngine.executeFile(filePath);
    }

    // === Tools ===

    public ExecutionResult useTool(String toolName, String... args) {
        return toolManager.executeTool(toolName, args);
    }

    public List<String> listTools() {
        return toolManager.listTools();
    }

    public String getToolHelp(String toolName) {
        return toolManager.getToolHelp(toolName);
    }

    // === Lifecycle ===

    public void shutdown() {
        concurrentExecutor.shutdown();
    }

    public SandboxConfig getConfig() { return config; }
    public ProcessExecutor getProcessExecutor() { return processExecutor; }
    public FileSystemManager getFileSystemManager() { return fileSystemManager; }
    public NetworkManager getNetworkManager() { return networkManager; }
    public ScriptEngine getScriptEngine() { return scriptEngine; }
    public ToolManager getToolManager() { return toolManager; }
    public ConcurrentExecutor getConcurrentExecutor() { return concurrentExecutor; }
    public String getSessionId() { return sessionId; }
    public long getCreatedAt() { return createdAt; }
    public long getUptimeMs() { return System.currentTimeMillis() - createdAt; }

    public Map<String, Object> getStatus() {
        Map<String, Object> status = new LinkedHashMap<>();
        status.put("sessionId", sessionId);
        status.put("uptimeMs", getUptimeMs());
        status.put("workingDirectory", config.getWorkingDirectory().getAbsolutePath());
        status.put("activeThreads", concurrentExecutor.getActiveCount());
        status.put("completedTasks", concurrentExecutor.getCompletedTaskCount());
        status.put("processStats", processExecutor.getStats());
        status.put("availableTools", toolManager.listTools().size());
        return status;
    }
}
