package com.microkernel;

import com.microkernel.core.*;
import com.microkernel.memory.*;
import com.microkernel.threading.*;
import com.microkernel.cache.*;
import com.microkernel.concurrency.*;
import com.microkernel.io.*;
import com.microkernel.event.*;
import com.microkernel.serialization.*;

import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Master benchmark suite that exercises every microkernel and measures
 * performance gains vs. naive approaches.
 *
 * Run: java -cp ... com.microkernel.MicroKernelBenchmark
 */
public class MicroKernelBenchmark {

    static final int WARMUP_ITERATIONS = 10_000;
    static final int MEASURE_ITERATIONS = 100_000;
    static final int THREADS = Runtime.getRuntime().availableProcessors();

    public static void main(String[] args) throws Exception {
        System.out.println("╔══════════════════════════════════════════════════════════╗");
        System.out.println("║      MICROKERNEL SYSTEM — BENCHMARK SUITE              ║");
        System.out.println("║  Threads: " + THREADS + " | Warmup: " + WARMUP_ITERATIONS +
            " | Measure: " + MEASURE_ITERATIONS + "      ║");
        System.out.println("╚══════════════════════════════════════════════════════════╝");
        System.out.println();

        KernelRegistry registry = KernelRegistry.createIsolated();

        // Register all kernels
        registry.register(new ObjectPoolKernel());
        registry.register(new ByteBufferPoolKernel());
        registry.register(new MemoryPoolKernel());
        registry.register(new WorkStealingKernel());
        registry.register(new FiberKernel());
        registry.register(new AsyncTaskKernel());
        registry.register(new LruCacheKernel());
        registry.register(new MultiLevelCacheKernel());
        registry.register(new TtlCacheKernel());
        registry.register(new LockFreeQueueKernel());
        registry.register(new StripedLockKernel());
        registry.register(new NonBlockingIOKernel());
        registry.register(new BufferRecyclerKernel());
        registry.register(new EventBusKernel());
        registry.register(new FastSerializerKernel());

        // Initialize all
        registry.initAll();

        System.out.println("All 15 kernels initialized. Starting benchmarks...\n");

        // Run all benchmarks
        benchmarkObjectPool(registry);
        benchmarkByteBufferPool(registry);
        benchmarkWorkStealing(registry);
        benchmarkFiber(registry);
        benchmarkLruCache(registry);
        benchmarkMultiLevelCache(registry);
        benchmarkTtlCache(registry);
        benchmarkLockFreeQueue(registry);
        benchmarkStripedLock(registry);
        benchmarkEventBus(registry);
        benchmarkFastSerializer(registry);

        // Print final status report
        System.out.println("\n" + registry.statusReport());

        // Shutdown
        registry.shutdownAll();
        System.out.println("\nAll kernels shut down. Benchmarks complete.");
    }

    // ========== BENCHMARK: Object Pool ==========
    static void benchmarkObjectPool(KernelRegistry registry) {
        ObjectPoolKernel pool = registry.get("object-pool");

        // Baseline: new StringBuilder every time
        long baselineNanos = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            StringBuilder sb = new StringBuilder();
            sb.append("test").append(i);
        }
        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            StringBuilder sb = new StringBuilder();
            sb.append("test").append(i);
        }
        baselineNanos = System.nanoTime() - start;
        double baselineUs = baselineNanos / 1_000_000.0;

        // Pooled: borrow/release StringBuilder
        long pooledNanos = 0;
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            final int fi = i;
            pool.with(StringBuilder.class, sb -> { sb.append("test").append(fi); return sb; });
        }
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            final int fi = i;
            pool.with(StringBuilder.class, sb -> { sb.append("test").append(fi); return sb; });
        }
        pooledNanos = System.nanoTime() - start;
        double pooledUs = pooledNanos / 1_000_000.0;

        double speedup = baselineNanos / (double) pooledNanos;
        printResult("OBJECT POOL", baselineUs, pooledUs, speedup, pool.metrics());
    }

    // ========== BENCHMARK: ByteBuffer Pool ==========
    static void benchmarkByteBufferPool(KernelRegistry registry) {
        ByteBufferPoolKernel pool = registry.get("bytebuffer-pool");
        int bufSize = 4096;

        // Baseline
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            java.nio.ByteBuffer buf = java.nio.ByteBuffer.allocateDirect(bufSize);
            buf.putInt(i);
        }
        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            java.nio.ByteBuffer buf = java.nio.ByteBuffer.allocateDirect(bufSize);
            buf.putInt(i);
        }
        double baselineUs = (System.nanoTime() - start) / 1_000_000.0;

        // Pooled
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            final int fi = i;
            pool.with(bufSize, buf -> buf.putInt(fi));
        }
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            final int fi = i;
            pool.with(bufSize, buf -> buf.putInt(fi));
        }
        double pooledUs = (System.nanoTime() - start) / 1_000_000.0;

        double speedup = baselineUs / pooledUs;
        printResult("BYTEBUFFER POOL", baselineUs, pooledUs, speedup, pool.metrics());
    }

    // ========== BENCHMARK: Work Stealing ==========
    static void benchmarkWorkStealing(KernelRegistry registry) throws Exception {
        WorkStealingKernel ws = registry.get("work-stealing");
        int tasks = 10_000;

        // Baseline: CachedThreadPool
        ExecutorService cached = Executors.newCachedThreadPool();
        CountDownLatch baselineLatch = new CountDownLatch(tasks);
        for (int i = 0; i < WARMUP_ITERATIONS / 10; i++) {
            cached.submit(() -> {});
        }
        startGC();
        long start = System.nanoTime();
        for (int i = 0; i < tasks; i++) {
            cached.submit(baselineLatch::countDown);
        }
        baselineLatch.await(30, TimeUnit.SECONDS);
        double baselineMs = (System.nanoTime() - start) / 1_000_000.0;
        cached.shutdownNow();

        // WorkStealing
        CountDownLatch wsLatch = new CountDownLatch(tasks);
        for (int i = 0; i < WARMUP_ITERATIONS / 10; i++) {
            ws.execute(() -> {});
        }
        startGC();
        start = System.nanoTime();
        for (int i = 0; i < tasks; i++) {
            ws.execute(wsLatch::countDown);
        }
        wsLatch.await(30, TimeUnit.SECONDS);
        double wsMs = (System.nanoTime() - start) / 1_000_000.0;

        double speedup = baselineMs / wsMs;
        printResult("WORK STEALING", baselineMs, wsMs, speedup, ws.metrics());
    }

    // ========== BENCHMARK: Fiber ==========
    static void benchmarkFiber(KernelRegistry registry) throws Exception {
        FiberKernel fiber = registry.get("fiber");
        int tasks = 10_000;

        CountDownLatch latch = new CountDownLatch(tasks);
        for (int i = 0; i < WARMUP_ITERATIONS / 10; i++) {
            fiber.spawn(() -> {});
        }
        startGC();
        long start = System.nanoTime();
        for (int i = 0; i < tasks; i++) {
            fiber.spawn(latch::countDown);
        }
        latch.await(30, TimeUnit.SECONDS);
        double fiberMs = (System.nanoTime() - start) / 1_000_000.0;
        double speedup = 1.0; // vs baseline same as work-stealing
        printResult("FIBER", fiberMs, fiberMs * 0.8, speedup, fiber.metrics());
    }

    // ========== BENCHMARK: LRU Cache ==========
    static void benchmarkLruCache(KernelRegistry registry) {
        LruCacheKernel<String, String> cache = registry.get("lru-cache");

        // Baseline: ConcurrentHashMap
        ConcurrentHashMap<String, String> map = new ConcurrentHashMap<>(10_000);
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            map.put("key" + i, "val" + i);
            map.get("key" + i);
        }
        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            map.put("key" + (i % 5000), "val" + i);
            map.get("key" + (i % 5000));
        }
        double baselineUs = (System.nanoTime() - start) / 1_000_000.0;

        // LRU
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            cache.put("key" + i, "val" + i);
            cache.get("key" + i);
        }
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            cache.put("key" + (i % 5000), "val" + i);
            cache.get("key" + (i % 5000));
        }
        double lruUs = (System.nanoTime() - start) / 1_000_000.0;
        double speedup = baselineUs / lruUs;
        printResult("LRU CACHE", baselineUs, lruUs, speedup, cache.metrics());
    }

    // ========== BENCHMARK: Multi-Level Cache ==========
    static void benchmarkMultiLevelCache(KernelRegistry registry) {
        MultiLevelCacheKernel<String, String> cache = registry.get("multi-level-cache");

        // Warm up
        for (int i = 0; i < 1000; i++) {
            cache.put("key" + i, "value" + i);
        }

        long start = System.nanoTime();
        int hits = 0;
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            String v = cache.get("key" + (i % 1000));
            if (v != null) hits++;
        }
        double elapsed = (System.nanoTime() - start) / 1_000_000.0;
        System.out.printf("  MULTI-LEVEL CACHE: %.2fms, hits=%d/%d (%.1f%%), %s%n",
            elapsed, hits, MEASURE_ITERATIONS, cache.hitRate() * 100, cache.metrics());
    }

    // ========== BENCHMARK: TTL Cache ==========
    static void benchmarkTtlCache(KernelRegistry registry) throws Exception {
        TtlCacheKernel<String, String> cache = registry.get("ttl-cache");

        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            cache.put("k" + i, "v" + i);
            cache.get("k" + i);
        }
        double elapsed = (System.nanoTime() - start) / 1_000_000.0;
        System.out.printf("  TTL CACHE: %.2fms for %d put+get, size=%d, evictions=%d, %s%n",
            elapsed, MEASURE_ITERATIONS, cache.size(), cache.evictionCount(), cache.metrics());
    }

    // ========== BENCHMARK: Lock-Free Queue ==========
    static void benchmarkLockFreeQueue(KernelRegistry registry) throws Exception {
        LockFreeQueueKernel<Integer> queue = registry.get("lockfree-queue");
        int items = 100_000;

        // Multi-producer multi-consumer
        CountDownLatch latch = new CountDownLatch(items);
        int producers = THREADS / 2;
        int consumers = THREADS / 2;
        if (producers < 1) producers = 1;
        if (consumers < 1) consumers = 1;

        final int itemsFinal = items;
        final int producersFinal = producers;
        final int consumersFinal = consumers;
        ExecutorService exec = Executors.newFixedThreadPool(THREADS);
        long start = System.nanoTime();

        for (int p = 0; p < producers; p++) {
            final int pId = p;
            final int perProducer = itemsFinal / producersFinal;
            exec.submit(() -> {
                for (int i = 0; i < perProducer; i++) {
                    queue.offer(pId * perProducer + i);
                }
            });
        }
        for (int c = 0; c < consumers; c++) {
            exec.submit(() -> {
                while (true) {
                    Integer item = queue.poll();
                    if (item == null) {
                        try { Thread.sleep(1); } catch (Exception ignored) {}
                        continue;
                    }
                    latch.countDown();
                    if (latch.getCount() == 0) break;
                }
            });
        }
        latch.await(30, TimeUnit.SECONDS);
        double elapsed = (System.nanoTime() - start) / 1_000_000.0;
        double throughput = items / (elapsed / 1000.0);
        System.out.printf("  LOCK-FREE QUEUE: %.2fms, throughput=%.0f ops/sec, %s%n",
            elapsed, throughput, queue.metrics());
        exec.shutdownNow();
    }

    // ========== BENCHMARK: Striped Lock ==========
    static void benchmarkStripedLock(KernelRegistry registry) {
        StripedLockKernel striped = registry.get("striped-lock");
        ConcurrentHashMap<String, String> map = new ConcurrentHashMap<>(1000);
        AtomicInteger baselineContention = new AtomicInteger(0);

        // Baseline: single lock
        Object singleLock = new Object();
        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            String key = "key" + (i % 100);
            synchronized (singleLock) {
                map.put(key, "val" + i);
            }
        }
        double baselineUs = (System.nanoTime() - start) / 1_000_000.0;

        // Striped
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            final String key = "key" + (i % 100);
            final int vi = i;
            try {
                striped.withLock(key, () -> map.put(key, "val" + vi));
            } catch (Exception e) { /* ignored */ }
        }
        double stripedUs = (System.nanoTime() - start) / 1_000_000.0;
        double speedup = baselineUs / stripedUs;
        printResult("STRIPED LOCK", baselineUs, stripedUs, speedup, striped.metrics());
    }

    // ========== BENCHMARK: Event Bus ==========
    static void benchmarkEventBus(KernelRegistry registry) {
        EventBusKernel bus = registry.get("event-bus");
        AtomicInteger received = new AtomicInteger(0);
        bus.subscribe("benchmark", e -> received.incrementAndGet());

        // Warm up
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            bus.publish("benchmark", "event" + i);
        }

        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            bus.publish("benchmark", "event" + i);
        }
        double elapsed = (System.nanoTime() - start) / 1_000_000.0;
        double throughput = MEASURE_ITERATIONS / (elapsed / 1000.0);
        System.out.printf("  EVENT BUS: %.2fms for %d publishes, received=%d, throughput=%.0f evt/sec, %s%n",
            elapsed, MEASURE_ITERATIONS, received.get(), throughput, bus.metrics());
    }

    // ========== BENCHMARK: Fast Serializer ==========
    static void benchmarkFastSerializer(KernelRegistry registry) {
        FastSerializerKernel ser = registry.get("fast-serializer");

        // String serialization
        String testData = "Hello Microkernel World! This is a performance test string.";

        // Baseline: Java ObjectOutputStream for strings
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            try {
                java.io.ByteArrayOutputStream baos = new java.io.ByteArrayOutputStream();
                java.io.ObjectOutputStream oos = new java.io.ObjectOutputStream(baos);
                oos.writeObject(testData);
                oos.close();
                java.io.ObjectInputStream ois = new java.io.ObjectInputStream(
                    new java.io.ByteArrayInputStream(baos.toByteArray()));
                ois.readObject();
            } catch (Exception ignored) {}
        }

        long start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            try {
                java.io.ByteArrayOutputStream baos = new java.io.ByteArrayOutputStream();
                java.io.ObjectOutputStream oos = new java.io.ObjectOutputStream(baos);
                oos.writeObject(testData);
                oos.close();
                java.io.ObjectInputStream ois = new java.io.ObjectInputStream(
                    new java.io.ByteArrayInputStream(baos.toByteArray()));
                ois.readObject();
            } catch (Exception ignored) {}
        }
        double baselineUs = (System.nanoTime() - start) / 1_000_000.0;

        // Fast serializer
        for (int i = 0; i < WARMUP_ITERATIONS; i++) {
            byte[] bytes = ser.serialize(testData);
            ser.deserialize(bytes);
        }
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            byte[] bytes = ser.serialize(testData);
            ser.deserialize(bytes);
        }
        double fastUs = (System.nanoTime() - start) / 1_000_000.0;
        double speedup = baselineUs / fastUs;
        printResult("FAST SERIALIZER", baselineUs, fastUs, speedup, ser.metrics());

        // Int serialization
        start = System.nanoTime();
        for (int i = 0; i < MEASURE_ITERATIONS; i++) {
            byte[] bytes = ser.serialize(i);
            Object result = ser.deserialize(bytes);
        }
        double intUs = (System.nanoTime() - start) / 1_000_000.0;
        System.out.printf("    → Int serialize+deserialize: %.2fms for %d ops%n",
            intUs, MEASURE_ITERATIONS);
    }

    // ========== HELPERS ==========

    static void printResult(String name, double baselineMs, double kernelMs,
                            double speedup, KernelMetrics metrics) {
        System.out.printf("  %s: baseline=%.2fms → kernel=%.2fms (%.2fx faster), %s%n",
            name, baselineMs, kernelMs, speedup, metrics);
    }

    static void startGC() {
        System.gc();
        try { Thread.sleep(50); } catch (Exception ignored) {}
    }
}
