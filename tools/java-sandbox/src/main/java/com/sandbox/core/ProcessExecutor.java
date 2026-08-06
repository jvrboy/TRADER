package com.sandbox.core;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Core process executor that runs shell commands with timeout support,
 * output capture, and environment control.
 */
public class ProcessExecutor {

    private final SandboxConfig config;
    private final AtomicLong totalExecutions = new AtomicLong(0);
    private final AtomicLong totalExecutionTimeMs = new AtomicLong(0);

    public ProcessExecutor(SandboxConfig config) {
        this.config = config;
    }

    public ExecutionResult execute(String command) {
        return execute(command, config.getDefaultTimeout(), config.getTimeoutUnit());
    }

    public ExecutionResult execute(String command, long timeout, TimeUnit unit) {
        if (!config.isAllowCommandExecution()) {
            return new ExecutionResult(-1, "", "Command execution is disabled", 0, command, false,
                    new SecurityException("Command execution disabled"), -1);
        }

        totalExecutions.incrementAndGet();
        long startTime = System.currentTimeMillis();
        Process process = null;
        long pid = -1;

        try {
            List<String> commandList = buildCommand(command);
            ProcessBuilder pb = new ProcessBuilder(commandList);
            pb.directory(config.getWorkingDirectory());
            pb.redirectErrorStream(config.isRedirectErrorStream());

            // Merge environment
            Map<String, String> env = pb.environment();
            env.putAll(config.getEnvironmentVariables());

            process = pb.start();
            pid = getPid(process);

            // Stream readers with buffer limit
            StreamGobbler outputGobbler = new StreamGobbler(process.getInputStream(), config.getMaxOutputBufferSize());
            StreamGobbler errorGobbler = new StreamGobbler(process.getErrorStream(), config.getMaxOutputBufferSize());

            ExecutorService streamPool = Executors.newFixedThreadPool(2);
            Future<String> stdoutFuture = streamPool.submit(outputGobbler);
            Future<String> stderrFuture = streamPool.submit(errorGobbler);

            boolean finished = process.waitFor(timeout, unit);
            long elapsed = System.currentTimeMillis() - startTime;
            totalExecutionTimeMs.addAndGet(elapsed);

            if (!finished) {
                process.destroyForcibly();
                streamPool.shutdownNow();
                return new ExecutionResult(-1,
                        outputGobbler.getPartial(),
                        "Process timed out after " + unit.toMillis(timeout) + "ms",
                        elapsed, command, true, new ProcessTimeoutException(command, unit.toMillis(timeout)), pid);
            }

            String stdout = stdoutFuture.get(5, TimeUnit.SECONDS);
            String stderr = config.isRedirectErrorStream() ? "" : stderrFuture.get(5, TimeUnit.SECONDS);
            streamPool.shutdown();

            return new ExecutionResult(process.exitValue(), stdout, stderr, elapsed, command, false, null, pid);

        } catch (Exception e) {
            long elapsed = System.currentTimeMillis() - startTime;
            if (process != null) process.destroyForcibly();
            return new ExecutionResult(-1, "", "Error: " + e.getMessage(), elapsed, command, false, e, pid);
        }
    }

    public ExecutionResult executeDirect(List<String> command, long timeout, TimeUnit unit) {
        return executeDirect(command, timeout, unit, String.join(" ", command));
    }

    private ExecutionResult executeDirect(List<String> commandList, long timeout, TimeUnit unit, String displayCommand) {
        if (!config.isAllowCommandExecution()) {
            return new ExecutionResult(-1, "", "Command execution is disabled", 0, displayCommand, false,
                    new SecurityException("Command execution disabled"), -1);
        }
        totalExecutions.incrementAndGet();
        long startTime = System.currentTimeMillis();
        Process process = null;
        long pid = -1;
        try {
            ProcessBuilder pb = new ProcessBuilder(commandList);
            pb.directory(config.getWorkingDirectory());
            pb.redirectErrorStream(config.isRedirectErrorStream());
            Map<String, String> env = pb.environment();
            env.putAll(config.getEnvironmentVariables());
            process = pb.start();
            pid = getPid(process);
            StreamGobbler outputGobbler = new StreamGobbler(process.getInputStream(), config.getMaxOutputBufferSize());
            StreamGobbler errorGobbler = new StreamGobbler(process.getErrorStream(), config.getMaxOutputBufferSize());
            ExecutorService streamPool = Executors.newFixedThreadPool(2);
            Future<String> stdoutFuture = streamPool.submit(outputGobbler);
            Future<String> stderrFuture = streamPool.submit(errorGobbler);
            boolean finished = process.waitFor(timeout, unit);
            long elapsed = System.currentTimeMillis() - startTime;
            totalExecutionTimeMs.addAndGet(elapsed);
            if (!finished) {
                process.destroyForcibly();
                streamPool.shutdownNow();
                return new ExecutionResult(-1, outputGobbler.getPartial(),
                        "Process timed out after " + unit.toMillis(timeout) + "ms",
                        elapsed, displayCommand, true, new ProcessTimeoutException(displayCommand, unit.toMillis(timeout)), pid);
            }
            String stdout = stdoutFuture.get(5, TimeUnit.SECONDS);
            String stderr = config.isRedirectErrorStream() ? "" : stderrFuture.get(5, TimeUnit.SECONDS);
            streamPool.shutdown();
            return new ExecutionResult(process.exitValue(), stdout, stderr, elapsed, displayCommand, false, null, pid);
        } catch (Exception e) {
            long elapsed = System.currentTimeMillis() - startTime;
            if (process != null) process.destroyForcibly();
            return new ExecutionResult(-1, "", "Error: " + e.getMessage(), elapsed, displayCommand, false, e, pid);
        }
    }

    public ExecutionResult executeShell(String script, long timeout, TimeUnit unit) {
        String os = System.getProperty("os.name").toLowerCase();
        List<String> cmd;
        if (os.contains("win")) {
            cmd = Arrays.asList("cmd.exe", "/c", script);
        } else {
            cmd = Arrays.asList("/bin/bash", "-c", script);
        }
        return executeDirect(cmd, timeout, unit, script);
    }

    public Map<String, Object> getStats() {
        Map<String, Object> stats = new LinkedHashMap<>();
        stats.put("totalExecutions", totalExecutions.get());
        stats.put("totalExecutionTimeMs", totalExecutionTimeMs.get());
        stats.put("avgExecutionTimeMs", totalExecutions.get() > 0
                ? (double) totalExecutionTimeMs.get() / totalExecutions.get() : 0);
        return stats;
    }

    private List<String> buildCommand(String command) {
        String os = System.getProperty("os.name").toLowerCase();
        if (os.contains("win")) {
            return Arrays.asList("cmd.exe", "/c", command);
        }
        return Arrays.asList("/bin/bash", "-c", command);
    }

    private long getPid(Process process) {
        try {
            return process.pid();
        } catch (Exception e) {
            return -1;
        }
    }

    /**
     * Reads an InputStream into a String with a maximum buffer size.
     */
    private static class StreamGobbler implements Callable<String> {
        private final InputStream is;
        private final int maxBytes;
        private final StringBuilder buffer = new StringBuilder();

        StreamGobbler(InputStream is, int maxBytes) {
            this.is = is;
            this.maxBytes = maxBytes;
        }

        @Override
        public String call() throws IOException {
            try (BufferedReader reader = new BufferedReader(new InputStreamReader(is, StandardCharsets.UTF_8))) {
                char[] cbuf = new char[8192];
                int read;
                while ((read = reader.read(cbuf)) != -1 && buffer.length() < maxBytes) {
                    int toAdd = Math.min(read, maxBytes - buffer.length());
                    buffer.append(cbuf, 0, toAdd);
                }
            }
            return buffer.toString();
        }

        public String getPartial() {
            return buffer.toString();
        }
    }
}
