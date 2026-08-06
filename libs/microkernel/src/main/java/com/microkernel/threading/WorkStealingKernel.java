package com.microkernel.threading;

import com.microkernel.core.*;

import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Micro-kernel: Work-stealing thread pool that balances load across cores
 * without a centralized task queue. Each worker has its own deque and can
 * steal from others when idle.
 *
 * This eliminates contention on a single shared queue and keeps all cores busy.
 */
public class WorkStealingKernel implements MicroKernel {

    private ForkJoinPool pool;
    private int parallelism;
    private KernelMetrics metrics;

    public WorkStealingKernel() {
        this(Runtime.getRuntime().availableProcessors());
    }

    public WorkStealingKernel(int parallelism) {
        this.parallelism = Math.max(1, parallelism);
    }

    @Override
    public String name() { return "work-stealing"; }

    @Override
    public String description() {
        return "Lock-free work-stealing executor that balances load across all CPU cores";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("work-stealing");
        pool = new ForkJoinPool(
            parallelism,
            ForkJoinPool.defaultForkJoinWorkerThreadFactory,
            null,
            true  // asyncMode
        );
    }

    /** Submit a task and get a future */
    public <T> CompletableFuture<T> submit(Callable<T> task) {
        long start = System.nanoTime();
        return CompletableFuture.supplyAsync(() -> {
            try {
                return task.call();
            } catch (Exception e) {
                throw new CompletionException(e);
            }
        }, pool).whenComplete((r, e) -> metrics.recordOperation(System.nanoTime() - start));
    }

    /** Submit a fire-and-forget task */
    public void execute(Runnable task) {
        long start = System.nanoTime();
        pool.execute(() -> {
            try {
                task.run();
            } finally {
                metrics.recordOperation(System.nanoTime() - start);
            }
        });
    }

    /** Submit many tasks and wait for all to complete */
    public void invokeAll(Runnable... tasks) {
        ForkJoinTask<?>[] fjTasks = new ForkJoinTask[tasks.length];
        for (int i = 0; i < tasks.length; i++) {
            fjTasks[i] = pool.submit(tasks[i]);
        }
        for (ForkJoinTask<?> t : fjTasks) {
            t.join();
        }
    }

    /** Parallel stream over a range [from, to) applying the action */
    public void parallelFor(int from, int to, IntConsumerWithException action) {
        int range = to - from;
        if (range <= 0) return;

        int chunkSize = Math.max(1, range / parallelism);
        AtomicInteger idx = new AtomicInteger(from);

        for (int i = 0; i < parallelism; i++) {
            execute(() -> {
                while (true) {
                    int start = idx.getAndAdd(chunkSize);
                    if (start >= to) break;
                    int end = Math.min(start + chunkSize, to);
                    for (int j = start; j < end; j++) {
                        try {
                            action.accept(j);
                        } catch (Exception e) {
                            metrics.recordError();
                        }
                    }
                }
            });
        }
    }

    /** Functional interface that can throw checked exceptions */
    @FunctionalInterface
    public interface IntConsumerWithException {
        void accept(int value) throws Exception;
    }

    /** Get the underlying ForkJoinPool (for advanced use) */
    public ForkJoinPool getPool() { return pool; }

    @Override
    public void shutdown() {
        if (pool != null) pool.shutdownNow();
    }

    @Override
    public KernelStatus status() {
        return pool.isShutdown() ? KernelStatus.STOPPED : KernelStatus.RUNNING;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
