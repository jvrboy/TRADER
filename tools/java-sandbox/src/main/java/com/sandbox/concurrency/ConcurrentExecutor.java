package com.sandbox.concurrency;

import com.sandbox.core.ExecutionResult;
import com.sandbox.core.SandboxConfig;

import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Manages concurrent execution of multiple commands and scripts.
 * Uses a bounded thread pool with queue management.
 */
public class ConcurrentExecutor {

    private final ThreadPoolExecutor executor;
    private final SandboxConfig config;
    private final AtomicLong totalSubmitted = new AtomicLong(0);
    private final Map<String, Future<ExecutionResult>> runningTasks = new ConcurrentHashMap<>();

    public ConcurrentExecutor(SandboxConfig config) {
        this.config = config;
        int maxThreads = config.getMaxConcurrentProcesses();
        this.executor = new ThreadPoolExecutor(
                Math.max(2, maxThreads / 4),
                maxThreads,
                60L, TimeUnit.SECONDS,
                new LinkedBlockingQueue<>(maxThreads * 10),
                new ThreadFactory() {
                    private final AtomicLong counter = new AtomicLong(0);
                    @Override
                    public Thread newThread(Runnable r) {
                        Thread t = new Thread(r, "sandbox-worker-" + counter.incrementAndGet());
                        t.setDaemon(true);
                        return t;
                    }
                },
                new ThreadPoolExecutor.CallerRunsPolicy()
        );
    }

    public Future<ExecutionResult> submit(Callable<ExecutionResult> task) {
        totalSubmitted.incrementAndGet();
        return executor.submit(task);
    }

    public Future<ExecutionResult> submit(String taskId, Callable<ExecutionResult> task) {
        totalSubmitted.incrementAndGet();
        Future<ExecutionResult> future = executor.submit(task);
        runningTasks.put(taskId, future);
        return future;
    }

    public List<ExecutionResult> runAll(List<Runnable> tasks) {
        List<Future<ExecutionResult>> futures = new ArrayList<>();
        for (Runnable task : tasks) {
            futures.add(executor.submit(() -> {
                task.run();
                return new ExecutionResult(0, "", "", 0, "", false, null, -1);
            }));
        }
        List<ExecutionResult> results = new ArrayList<>();
        for (Future<ExecutionResult> f : futures) {
            try { results.add(f.get()); } catch (Exception e) {
                results.add(new ExecutionResult(-1, "", e.getMessage(), 0, "", false, e, -1));
            }
        }
        return results;
    }

    public void shutdown() {
        executor.shutdown();
        try {
            if (!executor.awaitTermination(30, TimeUnit.SECONDS)) {
                executor.shutdownNow();
            }
        } catch (InterruptedException e) {
            executor.shutdownNow();
        }
    }

    public void shutdownNow() {
        executor.shutdownNow();
    }

    public int getActiveCount() { return executor.getActiveCount(); }
    public long getCompletedTaskCount() { return executor.getCompletedTaskCount(); }
    public int getQueueSize() { return executor.getQueue().size(); }
    public long getTotalSubmitted() { return totalSubmitted.get(); }
    public int getPoolSize() { return executor.getPoolSize(); }
    public int getMaxPoolSize() { return executor.getMaximumPoolSize(); }

    public Map<String, Object> getStats() {
        Map<String, Object> stats = new LinkedHashMap<>();
        stats.put("activeThreads", getActiveCount());
        stats.put("poolSize", getPoolSize());
        stats.put("maxPoolSize", getMaxPoolSize());
        stats.put("queueSize", getQueueSize());
        stats.put("completedTasks", getCompletedTaskCount());
        stats.put("totalSubmitted", getTotalSubmitted());
        return stats;
    }
}