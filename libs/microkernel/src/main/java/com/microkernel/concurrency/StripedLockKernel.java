package com.microkernel.concurrency;

import com.microkernel.core.*;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.locks.ReentrantLock;

/**
 * Nano-kernel: Striped lock mechanism that spreads contention across
 * multiple independent locks (stripes). Instead of one lock protecting
 * a big map, you get N locks each protecting a segment.
 *
 * This is the core of high-throughput data structures.
 *
 * Usage:
 *   StripedLockKernel locks = registry.get("striped-lock");
 *   int stripe = locks.stripeForKey(key);
 *   locks.withLock(stripe, () -> map.put(key, value));
 */
public class StripedLockKernel implements MicroKernel {

    private final ReentrantLock[] stripes;
    private final int stripeCount;
    private KernelMetrics metrics;

    public StripedLockKernel() {
        this(64);
    }

    public StripedLockKernel(int stripeCount) {
        this.stripeCount = Math.max(1, Integer.highestOneBit(stripeCount));
        this.stripes = new ReentrantLock[this.stripeCount];
        for (int i = 0; i < this.stripeCount; i++) {
            stripes[i] = new ReentrantLock();
        }
    }

    @Override
    public String name() { return "striped-lock"; }

    @Override
    public String description() {
        return "Striped lock mechanism that spreads contention across N independent lock segments";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("striped-lock");
    }

    /** Compute stripe index for an object key */
    public int stripeForKey(Object key) {
        return (key.hashCode() & 0x7FFFFFFF) % stripeCount;
    }

    /** Execute action under the stripe lock for this key */
    public void withLock(Object key, Runnable action) {
        withLock(stripeForKey(key), action);
    }

    /** Execute action under a specific stripe */
    public void withLock(int stripeIndex, Runnable action) {
        long start = System.nanoTime();
        ReentrantLock lock = stripes[stripeIndex];
        lock.lock();
        try {
            action.run();
        } finally {
            lock.unlock();
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Execute callable under the stripe lock */
    public <T> T withLock(Object key, java.util.concurrent.Callable<T> action) throws Exception {
        return withLock(stripeForKey(key), action);
    }

    /** Execute callable under a specific stripe */
    public <T> T withLock(int stripeIndex, java.util.concurrent.Callable<T> action) throws Exception {
        long start = System.nanoTime();
        ReentrantLock lock = stripes[stripeIndex];
        lock.lock();
        try {
            return action.call();
        } finally {
            lock.unlock();
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Try to acquire lock without blocking */
    public boolean tryLock(Object key, Runnable action) {
        return tryLock(stripeForKey(key), action);
    }

    /** Try to acquire specific stripe without blocking */
    public boolean tryLock(int stripeIndex, Runnable action) {
        ReentrantLock lock = stripes[stripeIndex];
        if (lock.tryLock()) {
            try {
                action.run();
            } finally {
                lock.unlock();
            }
            return true;
        }
        return false;
    }

    /** Number of stripes */
    public int stripeCount() { return stripeCount; }

    /** Check if any stripe is currently locked */
    public boolean anyLocked() {
        for (ReentrantLock lock : stripes) {
            if (lock.isLocked()) return true;
        }
        return false;
    }

    @Override
    public void shutdown() {}

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
