package com.microkernel.cache;

import com.microkernel.core.*;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Nano-kernel: TTL-based cache that expires entries after a fixed
 * time-to-live. Uses a single sweeper thread to evict expired entries.
 *
 * Usage:
 *   TtlCacheKernel<String, String> cache = registry.get("ttl-cache");
 *   cache.put("session:123", token, 30, TimeUnit.SECONDS);
 *   String t = cache.get("session:123");  // null if expired
 */
public class TtlCacheKernel<K, V> implements MicroKernel {

    private static class Entry<V> {
        V value;
        long expireAtNanos;
        Entry(V value, long ttlNanos) {
            this.value = value;
            this.expireAtNanos = System.nanoTime() + ttlNanos;
        }
        boolean isExpired() { return System.nanoTime() > expireAtNanos; }
    }

    private final ConcurrentHashMap<K, Entry<V>> store = new ConcurrentHashMap<>();
    private final AtomicLong evictionCount = new AtomicLong(0);
    private ScheduledExecutorService sweeper;
    private KernelMetrics metrics;

    public TtlCacheKernel() {}

    @Override
    public String name() { return "ttl-cache"; }

    @Override
    public String description() {
        return "TTL-based cache with lazy eviction on access and periodic sweep";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("ttl-cache");
        sweeper = Executors.newSingleThreadScheduledExecutor(r -> {
            Thread t = new Thread(r, "ttl-sweeper");
            t.setDaemon(true);
            return t;
        });
        // Sweep every 5 seconds
        sweeper.scheduleAtFixedRate(this::sweep, 5, 5, TimeUnit.SECONDS);
    }

    /** Put with explicit TTL */
    public void put(K key, V value, long ttl, TimeUnit unit) {
        store.put(key, new Entry<>(value, unit.toNanos(ttl)));
    }

    /** Put with default 60-second TTL */
    public void put(K key, V value) {
        put(key, value, 60, TimeUnit.SECONDS);
    }

    /** Get if not expired (lazy eviction) */
    public V get(K key) {
        Entry<V> entry = store.get(key);
        if (entry == null) return null;
        if (entry.isExpired()) {
            store.remove(key, entry);
            evictionCount.incrementAndGet();
            return null;
        }
        return entry.value;
    }

    /** Remove explicitly */
    public V remove(K key) {
        Entry<V> entry = store.remove(key);
        return entry == null ? null : entry.value;
    }

    /** Periodic sweep of expired entries */
    private void sweep() {
        long start = System.nanoTime();
        for (Map.Entry<K, Entry<V>> e : store.entrySet()) {
            if (e.getValue().isExpired()) {
                store.remove(e.getKey(), e.getValue());
                evictionCount.incrementAndGet();
            }
        }
        metrics.recordOperation(System.nanoTime() - start);
    }

    public int size() { return store.size(); }
    public long evictionCount() { return evictionCount.get(); }

    @Override
    public void shutdown() {
        if (sweeper != null) sweeper.shutdownNow();
        store.clear();
    }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
