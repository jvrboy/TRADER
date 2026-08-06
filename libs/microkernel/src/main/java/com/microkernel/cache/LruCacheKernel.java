package com.microkernel.cache;

import com.microkernel.core.*;

import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.locks.ReentrantReadWriteLock;

/**
 * Nano-kernel: Lock-free-ish LRU cache with O(1) get/put/evict.
 * Uses a segmented LinkedHashMap approach for concurrency.
 *
 * Usage:
 *   LruCacheKernel<String, Data> cache = registry.get("lru-cache");
 *   cache.put("key", data);
 *   Data d = cache.get("key");
 */
public class LruCacheKernel<K, V> implements MicroKernel {

    private final int maxSize;
    private final int segments;
    private final Segment<K, V>[] segmentArray;
    private KernelMetrics metrics;

    public LruCacheKernel() {
        this(10_000, 16);
    }

    public LruCacheKernel(int maxSize, int segments) {
        this.maxSize = maxSize;
        this.segments = segments;
        this.segmentArray = new Segment[segments];
        for (int i = 0; i < segments; i++) {
            segmentArray[i] = new Segment<>(maxSize / segments + 1);
        }
    }

    @Override
    public String name() { return "lru-cache"; }

    @Override
    public String description() {
        return "Segmented LRU cache with O(1) operations and lock-striping for concurrency";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("lru-cache");
    }

    @SuppressWarnings("unchecked")
    public void put(K key, V value) {
        long start = System.nanoTime();
        int seg = segmentIndex(key);
        segmentArray[seg].put(key, value);
        metrics.recordOperation(System.nanoTime() - start);
    }

    @SuppressWarnings("unchecked")
    public V get(K key) {
        long start = System.nanoTime();
        int seg = segmentIndex(key);
        V result = segmentArray[seg].get(key);
        metrics.recordOperation(System.nanoTime() - start);
        return result;
    }

    @SuppressWarnings("unchecked")
    public V remove(K key) {
        int seg = segmentIndex(key);
        return segmentArray[seg].remove(key);
    }

    public void clear() {
        for (Segment<K, V> seg : segmentArray) seg.clear();
    }

    public int size() {
        int total = 0;
        for (Segment<K, V> seg : segmentArray) total += seg.size();
        return total;
    }

    private int segmentIndex(K key) {
        return (key.hashCode() & 0x7FFFFFFF) % segments;
    }

    /** Each segment is a synchronized LRU map */
    private static class Segment<K, V> {
        private final int maxSegmentSize;
        private final LinkedHashMap<K, V> map;

        Segment(int maxSegmentSize) {
            this.maxSegmentSize = maxSegmentSize;
            // Access-order LinkedHashMap: on get(), entry moves to end
            this.map = new LinkedHashMap<>(maxSegmentSize, 0.75f, true);
        }

        private final ReentrantReadWriteLock lock = new ReentrantReadWriteLock();

        void put(K key, V value) {
            lock.writeLock().lock();
            try {
                map.put(key, value);
                if (map.size() > maxSegmentSize) {
                    // Remove oldest (first) entry
                    Map.Entry<K, V> oldest = map.entrySet().iterator().next();
                    map.remove(oldest.getKey());
                }
            } finally {
                lock.writeLock().unlock();
            }
        }

        V get(K key) {
            lock.readLock().lock();
            try {
                return map.get(key);
            } finally {
                lock.readLock().unlock();
            }
        }

        V remove(K key) {
            lock.writeLock().lock();
            try {
                return map.remove(key);
            } finally {
                lock.writeLock().unlock();
            }
        }

        void clear() {
            lock.writeLock().lock();
            try {
                map.clear();
            } finally {
                lock.writeLock().unlock();
            }
        }

        int size() {
            lock.readLock().lock();
            try {
                return map.size();
            } finally {
                lock.readLock().unlock();
            }
        }
    }

    @Override
    public void shutdown() { clear(); }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
