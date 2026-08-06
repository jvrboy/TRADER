package com.microkernel.cache;

import com.microkernel.core.*;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.Function;

/**
 * Micro-kernel: Two-level cache hierarchy (L1 in-memory + L2 soft-reference).
 * L1 is a fast bounded concurrent map, L2 uses SoftReference entries that
 * survive GC pressure but get reclaimed when memory is tight.
 *
 * This gives you the hit rate of a large cache with the memory safety
 * of a small one.
 */
public class MultiLevelCacheKernel<K, V> implements MicroKernel {

    private final int l1MaxSize;
    private final ConcurrentHashMap<K, V> l1;
    private final ConcurrentHashMap<K, java.lang.ref.SoftReference<V>> l2;
    private final AtomicLong l1Hits = new AtomicLong(0);
    private final AtomicLong l2Hits = new AtomicLong(0);
    private final AtomicLong misses = new AtomicLong(0);
    private KernelMetrics metrics;

    public MultiLevelCacheKernel() {
        this(10_000);
    }

    public MultiLevelCacheKernel(int l1MaxSize) {
        this.l1MaxSize = l1MaxSize;
        this.l1 = new ConcurrentHashMap<>(l1MaxSize);
        this.l2 = new ConcurrentHashMap<>(l1MaxSize * 4);
    }

    @Override
    public String name() { return "multi-level-cache"; }

    @Override
    public String description() {
        return "Two-level cache (L1=concurrent map, L2=soft references) for high hit-rate + low GC";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("multi-level-cache");
    }

    /** Get from cache; returns null if not found */
    public V get(K key) {
        long start = System.nanoTime();
        try {
            // L1
            V val = l1.get(key);
            if (val != null) {
                l1Hits.incrementAndGet();
                return val;
            }
            // L2
            java.lang.ref.SoftReference<V> ref = l2.get(key);
            if (ref != null) {
                val = ref.get();
                if (val != null) {
                    l2Hits.incrementAndGet();
                    // Promote to L1
                    if (l1.size() < l1MaxSize) {
                        l1.put(key, val);
                    }
                    return val;
                } else {
                    // SoftReference was cleared
                    l2.remove(key);
                }
            }
            misses.incrementAndGet();
            return null;
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Put into both L1 and L2 */
    public void put(K key, V value) {
        long start = System.nanoTime();
        try {
            l1.put(key, value);
            l2.put(key, new java.lang.ref.SoftReference<>(value));
            // Evict from L1 if over capacity
            if (l1.size() > l1MaxSize) {
                // Simple: clear half of L1 (it's backed by L2 anyway)
                int count = 0;
                for (Map.Entry<K, V> e : l1.entrySet()) {
                    if (count++ >= l1MaxSize / 2) break;
                    l1.remove(e.getKey());
                }
            }
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Get or compute and cache */
    public V getOrCompute(K key, Function<K, V> loader) {
        V val = get(key);
        if (val != null) return val;
        val = loader.apply(key);
        if (val != null) put(key, val);
        return val;
    }

    /** Invalidate from both levels */
    public void invalidate(K key) {
        l1.remove(key);
        l2.remove(key);
    }

    /** Stats */
    public long l1Hits()     { return l1Hits.get(); }
    public long l2Hits()     { return l2Hits.get(); }
    public long misses()     { return misses.get(); }
    public int  l1Size()     { return l1.size(); }
    public int  l2Size()     { return l2.size(); }
    public double hitRate() {
        long total = l1Hits.get() + l2Hits.get() + misses.get();
        return total == 0 ? 0.0 : (double)(l1Hits.get() + l2Hits.get()) / total;
    }

    @Override
    public void shutdown() { l1.clear(); l2.clear(); }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
