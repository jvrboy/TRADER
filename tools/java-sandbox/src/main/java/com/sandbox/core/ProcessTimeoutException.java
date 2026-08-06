package com.sandbox.core;

/**
 * Thrown when a process execution exceeds its timeout.
 */
public class ProcessTimeoutException extends RuntimeException {
    private final long timeoutMs;
    private final String command;

    public ProcessTimeoutException(String command, long timeoutMs) {
        super("Process timed out after " + timeoutMs + "ms: " + command);
        this.timeoutMs = timeoutMs;
        this.command = command;
    }

    public long getTimeoutMs() { return timeoutMs; }
    public String getCommand() { return command; }
}
