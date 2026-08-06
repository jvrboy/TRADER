package com.sandbox.core;

/**
 * Represents the result of a sandbox execution.
 * Contains exit code, output streams, timing, and metadata.
 */
public class ExecutionResult {

    private final int exitCode;
    private final String stdout;
    private final String stderr;
    private final long executionTimeMs;
    private final String command;
    private final boolean timedOut;
    private final Throwable exception;
    private final long pid;

    public ExecutionResult(int exitCode, String stdout, String stderr,
                           long executionTimeMs, String command, boolean timedOut,
                           Throwable exception, long pid) {
        this.exitCode = exitCode;
        this.stdout = stdout;
        this.stderr = stderr;
        this.executionTimeMs = executionTimeMs;
        this.command = command;
        this.timedOut = timedOut;
        this.exception = exception;
        this.pid = pid;
    }

    public boolean isSuccess() { return exitCode == 0 && !timedOut && exception == null; }
    public int getExitCode() { return exitCode; }
    public String getStdout() { return stdout; }
    public String getStderr() { return stderr; }
    public long getExecutionTimeMs() { return executionTimeMs; }
    public String getCommand() { return command; }
    public boolean isTimedOut() { return timedOut; }
    public Throwable getException() { return exception; }
    public long getPid() { return pid; }

    public String getOutput() {
        if (stderr == null || stderr.isEmpty()) return stdout;
        if (stdout == null || stdout.isEmpty()) return stderr;
        return stdout + "\n--- STDERR ---\n" + stderr;
    }

    @Override
    public String toString() {
        return "ExecutionResult{" +
                "exitCode=" + exitCode +
                ", success=" + isSuccess() +
                ", timedOut=" + timedOut +
                ", executionTimeMs=" + executionTimeMs +
                ", pid=" + pid +
                ", command='" + command + '\'' +
                '}';
    }
}
