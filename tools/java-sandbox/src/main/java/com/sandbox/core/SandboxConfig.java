package com.sandbox.core;

import java.io.File;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;

/**
 * Configuration for the sandbox system.
 * Controls all sandbox behavior including timeouts, working directories,
 * resource limits, and security settings.
 */
public class SandboxConfig {

    private File workingDirectory;
    private long defaultTimeout;
    private TimeUnit timeoutUnit;
    private int maxConcurrentProcesses;
    private int maxMemoryMB;
    private boolean allowNetworkAccess;
    private boolean allowFileWrite;
    private boolean allowFileRead;
    private boolean allowCommandExecution;
    private Map<String, String> environmentVariables;
    private int maxOutputBufferSize;
    private boolean captureStderr;
    private String defaultShell;
    private boolean redirectErrorStream;
    private long idleTimeout;
    private TimeUnit idleTimeoutUnit;

    public SandboxConfig() {
        this.workingDirectory = new File(System.getProperty("user.dir"), ".sandbox");
        this.defaultTimeout = 60;
        this.timeoutUnit = TimeUnit.SECONDS;
        this.maxConcurrentProcesses = 50;
        this.maxMemoryMB = 512;
        this.allowNetworkAccess = true;
        this.allowFileWrite = true;
        this.allowFileRead = true;
        this.allowCommandExecution = true;
        this.environmentVariables = new HashMap<>(System.getenv());
        this.maxOutputBufferSize = 10 * 1024 * 1024; // 10MB
        this.captureStderr = true;
        this.defaultShell = detectDefaultShell();
        this.redirectErrorStream = false;
        this.idleTimeout = 300;
        this.idleTimeoutUnit = TimeUnit.SECONDS;
    }

    private static String detectDefaultShell() {
        String os = System.getProperty("os.name").toLowerCase();
        if (os.contains("win")) {
            return "cmd.exe";
        }
        return "/bin/bash";
    }

    // --- Builder pattern methods ---

    public SandboxConfig workingDirectory(File dir) {
        this.workingDirectory = dir;
        return this;
    }

    public SandboxConfig defaultTimeout(long timeout, TimeUnit unit) {
        this.defaultTimeout = timeout;
        this.timeoutUnit = unit;
        return this;
    }

    public SandboxConfig maxConcurrentProcesses(int max) {
        this.maxConcurrentProcesses = max;
        return this;
    }

    public SandboxConfig maxMemoryMB(int mb) {
        this.maxMemoryMB = mb;
        return this;
    }

    public SandboxConfig allowNetworkAccess(boolean allow) {
        this.allowNetworkAccess = allow;
        return this;
    }

    public SandboxConfig allowFileWrite(boolean allow) {
        this.allowFileWrite = allow;
        return this;
    }

    public SandboxConfig allowFileRead(boolean allow) {
        this.allowFileRead = allow;
        return this;
    }

    public SandboxConfig allowCommandExecution(boolean allow) {
        this.allowCommandExecution = allow;
        return this;
    }

    public SandboxConfig envVar(String key, String value) {
        this.environmentVariables.put(key, value);
        return this;
    }

    public SandboxConfig maxOutputBufferSize(int bytes) {
        this.maxOutputBufferSize = bytes;
        return this;
    }

    public SandboxConfig defaultShell(String shell) {
        this.defaultShell = shell;
        return this;
    }

    public SandboxConfig redirectErrorStream(boolean redirect) {
        this.redirectErrorStream = redirect;
        return this;
    }

    // --- Getters ---

    public File getWorkingDirectory() { return workingDirectory; }
    public long getDefaultTimeout() { return defaultTimeout; }
    public TimeUnit getTimeoutUnit() { return timeoutUnit; }
    public int getMaxConcurrentProcesses() { return maxConcurrentProcesses; }
    public int getMaxMemoryMB() { return maxMemoryMB; }
    public boolean isAllowNetworkAccess() { return allowNetworkAccess; }
    public boolean isAllowFileWrite() { return allowFileWrite; }
    public boolean isAllowFileRead() { return allowFileRead; }
    public boolean isAllowCommandExecution() { return allowCommandExecution; }
    public Map<String, String> getEnvironmentVariables() { return environmentVariables; }
    public int getMaxOutputBufferSize() { return maxOutputBufferSize; }
    public boolean isCaptureStderr() { return captureStderr; }
    public String getDefaultShell() { return defaultShell; }
    public boolean isRedirectErrorStream() { return redirectErrorStream; }
    public long getIdleTimeout() { return idleTimeout; }
    public TimeUnit getIdleTimeoutUnit() { return idleTimeoutUnit; }

    @Override
    public String toString() {
        return "SandboxConfig{" +
                "workingDirectory=" + workingDirectory +
                ", defaultTimeout=" + defaultTimeout + " " + timeoutUnit +
                ", maxConcurrentProcesses=" + maxConcurrentProcesses +
                ", maxMemoryMB=" + maxMemoryMB +
                ", allowNetworkAccess=" + allowNetworkAccess +
                ", allowFileWrite=" + allowFileWrite +
                ", allowFileRead=" + allowFileRead +
                ", defaultShell='" + defaultShell + '\'' +
                '}';
    }
}
