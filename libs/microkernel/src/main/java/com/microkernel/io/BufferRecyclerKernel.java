package com.microkernel.io;

import com.microkernel.core.*;

import java.nio.ByteBuffer;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Nano-kernel: Recycler for char[] arrays used in String encoding/decoding.
 * Eliminates allocation in high-frequency string processing loops.
 *
 * Usage:
 *   BufferRecyclerKernel recycler = registry.get("buffer-recycler");
 *   char[] chars = recycler.borrowChars(1024);
 *   // ... use chars ...
 *   recycler.releaseChars(chars);
 */
public class BufferRecyclerKernel implements MicroKernel {

    private final ConcurrentHashMap<Integer, ConcurrentLinkedQueue<char[]>> charPools = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Integer, ConcurrentLinkedQueue<byte[]>> bytePools = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Integer, Integer> maxPerBucket = new ConcurrentHashMap<>();
    private final AtomicLong totalRecycled = new AtomicLong(0);
    private KernelMetrics metrics;

    public BufferRecyclerKernel() {}

    @Override
    public String name() { return "buffer-recycler"; }

    @Override
    public String description() {
        return "Recycles char[] and byte[] buffers used in string processing to cut GC pressure";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("buffer-recycler");
        // Pre-create buckets for common sizes
        for (int size : new int[]{64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384}) {
            charPools.put(size, new ConcurrentLinkedQueue<>());
            bytePools.put(size, new ConcurrentLinkedQueue<>());
            maxPerBucket.put(size, 128);
        }
    }

    /** Borrow char[] of at least minSize */
    public char[] borrowChars(int minSize) {
        int bucket = bucketFor(minSize);
        ConcurrentLinkedQueue<char[]> pool = charPools.get(bucket);
        char[] arr = (pool != null) ? pool.poll() : null;
        if (arr == null) {
            arr = new char[bucket];
            metrics.recordAllocation();
        }
        return arr;
    }

    /** Release char[] back to pool */
    public void releaseChars(char[] arr) {
        if (arr == null) return;
        int bucket = bucketFor(arr.length);
        ConcurrentLinkedQueue<char[]> pool = charPools.get(bucket);
        Integer max = maxPerBucket.get(bucket);
        if (pool != null && max != null && pool.size() < max) {
            java.util.Arrays.fill(arr, '\0');
            pool.offer(arr);
            totalRecycled.incrementAndGet();
            metrics.recordRecycle();
        }
    }

    /** Borrow byte[] of at least minSize */
    public byte[] borrowBytes(int minSize) {
        int bucket = bucketFor(minSize);
        ConcurrentLinkedQueue<byte[]> pool = bytePools.get(bucket);
        byte[] arr = (pool != null) ? pool.poll() : null;
        if (arr == null) {
            arr = new byte[bucket];
            metrics.recordAllocation();
        }
        return arr;
    }

    /** Release byte[] back to pool */
    public void releaseBytes(byte[] arr) {
        if (arr == null) return;
        int bucket = bucketFor(arr.length);
        ConcurrentLinkedQueue<byte[]> pool = bytePools.get(bucket);
        Integer max = maxPerBucket.get(bucket);
        if (pool != null && max != null && pool.size() < max) {
            java.util.Arrays.fill(arr, (byte) 0);
            pool.offer(arr);
            totalRecycled.incrementAndGet();
            metrics.recordRecycle();
        }
    }

    /** Execute action with a borrowed char[] (auto-release) */
    public <R> R withChars(int size, java.util.function.Function<char[], R> action) {
        char[] arr = borrowChars(size);
        try { return action.apply(arr); }
        finally { releaseChars(arr); }
    }

    /** Execute action with a borrowed byte[] (auto-release) */
    public <R> R withBytes(int size, java.util.function.Function<byte[], R> action) {
        byte[] arr = borrowBytes(size);
        try { return action.apply(arr); }
        finally { releaseBytes(arr); }
    }

    private static int bucketFor(int size) {
        // Round up to nearest power of 2 (minimum 64)
        int b = 64;
        while (b < size) b <<= 1;
        return b;
    }

    public long totalRecycled() { return totalRecycled.get(); }

    @Override
    public void shutdown() {
        charPools.clear();
        bytePools.clear();
    }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
