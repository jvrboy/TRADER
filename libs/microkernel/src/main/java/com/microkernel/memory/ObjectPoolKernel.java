package com.microkernel.memory;

import com.microkernel.core.*;

import java.util.Queue;
import java.util.concurrent.*;
import java.util.function.Supplier;

/**
 * Nano-kernel: High-performance object pool that eliminates GC pressure
 * by recycling mutable objects instead of creating new ones.
 *
 * Usage:
 *   ObjectPoolKernel pool = registry.get("object-pool");
 *   StringBuilder sb = pool.borrow(StringBuilder.class, StringBuilder::new);
 *   sb.append("hello");
 *   pool.release(StringBuilder.class, sb);  // resets and returns to pool
 */
public class ObjectPoolKernel implements MicroKernel {

    private final ConcurrentHashMap<Class<?>, Queue<Object>> pools = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Class<?>, Supplier<?>> factories = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Class<?>, java.util.function.Consumer<?>> resetters = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Class<?>, Integer> maxSize = new ConcurrentHashMap<>();
    private KernelMetrics metrics;

    @Override
    public String name() { return "object-pool"; }

    @Override
    public String description() {
        return "Recycles mutable objects to eliminate GC pressure in hot paths";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("object-pool");
        // Pre-register common types
        registerPool(StringBuilder.class, StringBuilder::new,
            sb -> sb.setLength(0), 256);
        registerPool(StringBuffer.class, StringBuffer::new,
            sb -> sb.setLength(0), 128);
        registerPool(byte[].class, () -> new byte[4096],
            arr -> java.util.Arrays.fill(arr, (byte) 0), 512);
        registerPool(char[].class, () -> new char[4096],
            arr -> java.util.Arrays.fill(arr, (char) 0), 256);
    }

    /**
     * Register a pool for a specific type.
     * @param clazz    the type to pool
     * @param factory  supplier that creates new instances
     * @param resetter consumer that resets an instance before reuse
     * @param max      max pooled objects per type
     */
    public <T> void registerPool(Class<T> clazz, Supplier<T> factory,
                                  java.util.function.Consumer<T> resetter, int max) {
        factories.put(clazz, factory);
        resetters.put(clazz, resetter);
        maxSize.put(clazz, max);
        pools.put(clazz, new ConcurrentLinkedQueue<>());
    }

    /** Borrow an object from the pool, or create a new one if empty */
    @SuppressWarnings("unchecked")
    public <T> T borrow(Class<T> clazz) {
        long start = System.nanoTime();
        Queue<Object> pool = pools.get(clazz);
        T obj = null;
        if (pool != null) {
            obj = (T) pool.poll();
        }
        if (obj == null) {
            Supplier<?> factory = factories.get(clazz);
            if (factory != null) {
                obj = (T) factory.get();
                metrics.recordAllocation();
            } else {
                throw new IllegalArgumentException(
                    "No pool registered for " + clazz.getName());
            }
        }
        metrics.recordOperation(System.nanoTime() - start);
        return obj;
    }

    /** Return an object to the pool (resets it first) */
    @SuppressWarnings("unchecked")
    public <T> void release(Class<T> clazz, T obj) {
        if (obj == null) return;
        long start = System.nanoTime();
        try {
            java.util.function.Consumer<?> resetter = resetters.get(clazz);
            if (resetter != null) {
                ((java.util.function.Consumer<T>) resetter).accept(obj);
            }
            Queue<Object> pool = pools.get(clazz);
            Integer max = maxSize.get(clazz);
            if (pool != null && max != null && pool.size() < max) {
                pool.offer(obj);
                metrics.recordRecycle();
            }
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Execute a task with a borrowed object (auto-release) */
    public <T, R> R with(Class<T> clazz, java.util.function.Function<T, R> action) {
        T obj = borrow(clazz);
        try {
            return action.apply(obj);
        } finally {
            release(clazz, obj);
        }
    }

    @Override
    public void shutdown() {
        pools.clear();
    }

    @Override
    public KernelStatus status() {
        return KernelStatus.RUNNING;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }

    /** Number of pooled objects per type */
    public int poolSize(Class<?> clazz) {
        Queue<Object> pool = pools.get(clazz);
        return pool == null ? 0 : pool.size();
    }
}
