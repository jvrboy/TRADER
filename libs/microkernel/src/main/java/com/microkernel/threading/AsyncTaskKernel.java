package com.microkernel.threading;

import com.microkernel.core.*;

import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.Consumer;

/**
 * Nano-kernel: Rate-limited async task scheduler that prevents thread
 * explosion while allowing high-throughput fire-and-forget work.
 *
 * Internally uses a bounded queue + a single dispatcher thread that
 * fans out to the work-stealing pool — preventing the common mistake
 * of submitting millions of tasks to an unbounded executor.
 */
public class AsyncTaskKernel implements MicroKernel {

    private final int maxPending;
    private final BlockingQueue<Runnable> pendingQueue;
    private final AtomicInteger pendingCount = new AtomicInteger(0);
    private ExecutorService dispatcher;
    private volatile ExecutorService workerPool;
    private KernelMetrics metrics;
    private volatile boolean running = false;

    public AsyncTaskKernel() {
        this(100_000);
    }

    public AsyncTaskKernel(int maxPending) {
        this.maxPending = maxPending;
        this.pendingQueue = new LinkedBlockingQueue<>(maxPending);
    }

    @Override
    public String name() { return "async-task"; }

    @Override
    public String description() {
        return "Rate-limited async dispatcher that prevents thread explosion under load";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("async-task");
        // Use work-stealing kernel if available and initialized, else common pool
        WorkStealingKernel ws = context.getKernel("work-stealing");
        ExecutorService wsPool = (ws != null) ? ws.getPool() : null;
        workerPool = (wsPool != null) ? wsPool : ForkJoinPool.commonPool();
        running = true;

        dispatcher = Executors.newSingleThreadExecutor(r -> {
            Thread t = new Thread(r, "async-dispatcher");
            t.setDaemon(true);
            return t;
        });
        dispatcher.submit(this::dispatchLoop);
    }

    private void dispatchLoop() {
        while (running || !pendingQueue.isEmpty()) {
            try {
                Runnable task = pendingQueue.poll(100, TimeUnit.MILLISECONDS);
                if (task != null) {
                    workerPool.execute(() -> {
                        try {
                            task.run();
                        } finally {
                            pendingCount.decrementAndGet();
                        }
                    });
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                break;
            }
        }
    }

    /** Submit an async task. Returns false if queue is full (back-pressure). */
    public boolean submit(Runnable task) {
        if (pendingCount.get() >= maxPending) {
            metrics.recordError();
            return false;
        }
        pendingCount.incrementAndGet();
        boolean offered = pendingQueue.offer(task);
        if (!offered) {
            pendingCount.decrementAndGet();
            metrics.recordError();
            return false;
        }
        return true;
    }

    /** Submit with callback on completion */
    public void submit(Runnable task, Consumer<Throwable> onError) {
        submit(() -> {
            try {
                task.run();
            } catch (Throwable t) {
                if (onError != null) onError.accept(t);
            }
        });
    }

    /** Number of tasks waiting in the queue */
    public int pendingCount() { return pendingCount.get(); }

    /** Queue capacity remaining */
    public int remainingCapacity() {
        return maxPending - pendingCount.get();
    }

    @Override
    public void shutdown() {
        running = false;
        if (dispatcher != null) dispatcher.shutdownNow();
    }

    @Override
    public KernelStatus status() {
        return running ? KernelStatus.RUNNING : KernelStatus.STOPPED;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
