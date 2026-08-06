package com.microkernel.memory;

import com.microkernel.core.*;

import java.nio.ByteBuffer;
import java.util.Queue;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Nano-kernel: Pools direct ByteBuffers by size bucket to eliminate
 * allocation overhead in I/O-heavy paths.
 *
 * Standard Java ByteBuffer.allocateDirect() goes through JNI and the
 * GC — this kernel recycles buffers so hot I/O loops never allocate.
 *
 * Usage:
 *   ByteBufferPoolKernel bufPool = registry.get("bytebuffer-pool");
 *   ByteBuffer buf = bufPool.borrow(4096);
 *   // ... use buf ...
 *   bufPool.release(buf);
 */
public class ByteBufferPoolKernel implements MicroKernel {

    /** Size buckets (powers of 2) */
    private static final int[] BUCKET_SIZES = {
        64, 128, 256, 512, 1024, 2048, 4096, 8192,
        16384, 32768, 65536, 131072, 262144
    };

    private final ConcurrentHashMap<Integer, Queue<ByteBuffer>> pools = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Integer, AtomicInteger> poolCounts = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Integer, Integer> maxPerBucket = new ConcurrentHashMap<>();
    private KernelMetrics metrics;
    private boolean useDirect = true;

    @Override
    public String name() { return "bytebuffer-pool"; }

    @Override
    public String description() {
        return "Recycles direct ByteBuffers to eliminate JNI alloc overhead in I/O paths";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("bytebuffer-pool");
        for (int size : BUCKET_SIZES) {
            pools.put(size, new ConcurrentLinkedQueue<>());
            poolCounts.put(size, new AtomicInteger(0));
            maxPerBucket.put(size, 64);
        }
    }

    /** Find the smallest bucket that fits the requested size */
    private static int bucketFor(int size) {
        for (int bucket : BUCKET_SIZES) {
            if (bucket >= size) return bucket;
        }
        // Round up to next power of 2
        int b = BUCKET_SIZES[BUCKET_SIZES.length - 1];
        while (b < size) b <<= 1;
        return b;
    }

    /** Borrow a buffer of at least the given size */
    public ByteBuffer borrow(int minSize) {
        long start = System.nanoTime();
        int bucket = bucketFor(minSize);
        Queue<ByteBuffer> pool = pools.get(bucket);
        ByteBuffer buf = (pool != null) ? pool.poll() : null;

        if (buf != null) {
            buf.clear();
            poolCounts.get(bucket).decrementAndGet();
        } else {
            buf = useDirect
                ? ByteBuffer.allocateDirect(bucket)
                : ByteBuffer.allocate(bucket);
            metrics.recordAllocation();
        }
        metrics.recordOperation(System.nanoTime() - start);
        return buf;
    }

    /** Return a buffer to the pool */
    public void release(ByteBuffer buf) {
        if (buf == null) return;
        long start = System.nanoTime();
        try {
            int capacity = buf.capacity();
            int bucket = bucketFor(capacity);
            Queue<ByteBuffer> pool = pools.get(bucket);
            AtomicInteger count = poolCounts.get(bucket);
            Integer max = maxPerBucket.get(bucket);

            if (pool != null && count != null && max != null && count.get() < max) {
                buf.clear();
                pool.offer(buf);
                count.incrementAndGet();
                metrics.recordRecycle();
            }
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Execute a task with a borrowed buffer (auto-release) */
    public <R> R with(int size, java.util.function.Function<ByteBuffer, R> action) {
        ByteBuffer buf = borrow(size);
        try {
            return action.apply(buf);
        } finally {
            release(buf);
        }
    }

    /** Toggle between direct and heap buffers */
    public void setUseDirect(boolean flag) { this.useDirect = flag; }

    @Override
    public void shutdown() {
        pools.clear();
        poolCounts.clear();
    }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }

    public int availableBuffers(int bucketSize) {
        AtomicInteger c = poolCounts.get(bucketFor(bucketSize));
        return c == null ? 0 : c.get();
    }
}
