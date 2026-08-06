package com.microkernel.threading;

import com.microkernel.core.*;

import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Nano-kernel: Lightweight virtual thread-like fibers backed by a cached
 * thread pool. Provides ultra-lightweight task scheduling with minimal
 * overhead for short-lived concurrent tasks.
 *
 * On JVM 21+ this can be replaced with virtual threads, but this kernel
 * provides the same lightweight feel on any Java 8+ runtime.
 */
public class FiberKernel implements MicroKernel {

    private ExecutorService executor;
    private final int maxFibers;
    private final AtomicInteger activeFibers = new AtomicInteger(0);
    private KernelMetrics metrics;

    public FiberKernel() {
        this(256);
    }

    public FiberKernel(int maxFibers) {
        this.maxFibers = maxFibers;
    }

    @Override
    public String name() { return "fiber"; }

    @Override
    public String description() {
        return "Ultra-lightweight task fibers for massive concurrency with minimal overhead";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("fiber");
        executor = new ThreadPoolExecutor(
            0, maxFibers,
            60L, TimeUnit.SECONDS,
            new SynchronousQueue<>(),
            new ThreadFactory() {
                private final AtomicInteger tid = new AtomicInteger(0);
                @Override
                public Thread newThread(Runnable r) {
                    Thread t = new Thread(r, "fiber-" + tid.incrementAndGet());
                    t.setDaemon(true);
                    return t;
                }
            },
            new ThreadPoolExecutor.CallerRunsPolicy()
        );
    }

    /** Spawn a fiber (returns immediately) */
    public CompletableFuture<Void> spawn(Runnable task) {
        return CompletableFuture.runAsync(task, executor)
            .whenComplete((v, e) -> {
                activeFibers.decrementAndGet();
                if (e != null) metrics.recordError();
            });
    }

    /** Spawn a fiber that returns a value */
    public <T> CompletableFuture<T> spawn(Callable<T> task) {
        activeFibers.incrementAndGet();
        return CompletableFuture.supplyAsync(() -> {
            try {
                return task.call();
            } catch (Exception e) {
                throw new CompletionException(e);
            } finally {
                activeFibers.decrementAndGet();
            }
        }, executor);
    }

    /** Spawn many fibers and wait for all */
    public void spawnAll(Runnable... tasks) {
        CompletableFuture<?>[] futures = new CompletableFuture[tasks.length];
        for (int i = 0; i < tasks.length; i++) {
            futures[i] = spawn(tasks[i]);
        }
        CompletableFuture.allOf(futures).join();
    }

    /** Current active fiber count */
    public int activeCount() { return activeFibers.get(); }

    @Override
    public void shutdown() {
        if (executor != null) executor.shutdownNow();
    }

    @Override
    public KernelStatus status() {
        return executor.isShutdown() ? KernelStatus.STOPPED : KernelStatus.RUNNING;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
