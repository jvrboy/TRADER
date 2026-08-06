package com.microkernel;

import com.microkernel.core.*;
import com.microkernel.memory.*;
import com.microkernel.threading.*;
import com.microkernel.cache.*;
import com.microkernel.concurrency.*;
import com.microkernel.io.*;
import com.microkernel.event.*;
import com.microkernel.serialization.*;

import java.io.IOException;
import java.nio.file.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Integration test that verifies every kernel works end-to-end.
 */
public class MicroKernelIntegrationTest {

    static int passed = 0;
    static int failed = 0;

    public static void main(String[] args) throws Exception {
        System.out.println("Running MicroKernel Integration Tests...\n");

        testObjectPool();
        testByteBufferPool();
        testMemoryPool();
        testWorkStealing();
        testFiber();
        testAsyncTask();
        testLruCache();
        testMultiLevelCache();
        testTtlCache();
        testLockFreeQueue();
        testStripedLock();
        testBufferRecycler();
        testEventBus();
        testFastSerializer();
        testKernelRegistryStatus();

        System.out.printf("\n═══════════════════════════════════%n");
        System.out.printf("  RESULTS: %d passed, %d failed%n", passed, failed);
        System.out.printf("═══════════════════════════════════%n");

        if (failed > 0) System.exit(1);
    }

    static void testObjectPool() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new ObjectPoolKernel());
        reg.initAll();
        ObjectPoolKernel pool = reg.get("object-pool");

        StringBuilder sb = pool.borrow(StringBuilder.class);
        sb.append("hello world");
        assert sb.toString().equals("hello world") : "Pool returned corrupt object";
        pool.release(StringBuilder.class, sb);

        // Verify it was reset
        StringBuilder sb2 = pool.borrow(StringBuilder.class);
        assert sb2.length() == 0 : "Object not reset after release";
        pool.release(StringBuilder.class, sb2);

        pass("ObjectPoolKernel");
        reg.shutdownAll();
    }

    static void testByteBufferPool() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new ByteBufferPoolKernel());
        reg.initAll();
        ByteBufferPoolKernel pool = reg.get("bytebuffer-pool");

        java.nio.ByteBuffer buf = pool.borrow(4096);
        assert buf.capacity() >= 4096 : "Buffer too small";
        buf.putInt(42);
        pool.release(buf);

        pass("ByteBufferPoolKernel");
        reg.shutdownAll();
    }

    static void testMemoryPool() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new MemoryPoolKernel());
        reg.initAll();
        MemoryPoolKernel mp = reg.get("memory-pool");

        MemoryPoolKernel.MemoryCheckResult result = mp.checkMemory();
        assert result != null : "Memory check returned null";
        assert result.level == MemoryPoolKernel.MemoryLevel.OK ||
               result.level == MemoryPoolKernel.MemoryLevel.WARNING :
            "Unexpected memory level";

        pass("MemoryPoolKernel");
        reg.shutdownAll();
    }

    static void testWorkStealing() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new WorkStealingKernel());
        reg.initAll();
        WorkStealingKernel ws = reg.get("work-stealing");

        AtomicInteger counter = new AtomicInteger(0);
        CountDownLatch latch = new CountDownLatch(100);
        for (int i = 0; i < 100; i++) {
            ws.execute(() -> { counter.incrementAndGet(); latch.countDown(); });
        }
        latch.await(10, TimeUnit.SECONDS);
        assert counter.get() == 100 : "Not all tasks completed: " + counter.get();

        pass("WorkStealingKernel");
        reg.shutdownAll();
    }

    static void testFiber() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new FiberKernel());
        reg.initAll();
        FiberKernel fiber = reg.get("fiber");

        AtomicInteger counter = new AtomicInteger(0);
        CountDownLatch latch = new CountDownLatch(50);
        for (int i = 0; i < 50; i++) {
            fiber.spawn(() -> { counter.incrementAndGet(); latch.countDown(); });
        }
        latch.await(10, TimeUnit.SECONDS);
        assert counter.get() == 50 : "Fiber tasks failed: " + counter.get();

        pass("FiberKernel");
        reg.shutdownAll();
    }

    static void testAsyncTask() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new WorkStealingKernel());
        reg.register(new AsyncTaskKernel());
        reg.initAll();
        AsyncTaskKernel at = reg.get("async-task");

        AtomicInteger counter = new AtomicInteger(0);
        for (int i = 0; i < 100; i++) {
            boolean accepted = at.submit(() -> counter.incrementAndGet());
            assert accepted : "Task rejected (queue full)";
        }
        // Wait longer for dispatcher to process all tasks
        Thread.sleep(5000);
        // If still not complete, check pending count
        if (counter.get() < 100) {
            Thread.sleep(5000);
        }
        assert counter.get() == 100 : "Not all async tasks ran: " + counter.get();

        pass("AsyncTaskKernel");
        reg.shutdownAll();
    }

    static void testLruCache() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new LruCacheKernel());
        reg.initAll();
        LruCacheKernel<String, String> cache = reg.get("lru-cache");

        for (int i = 0; i < 100; i++) {
            cache.put("key" + i, "val" + i);
        }
        assert cache.size() == 100 : "Wrong cache size: " + cache.size();
        assert cache.get("key50").equals("val50") : "Wrong value retrieved";

        pass("LruCacheKernel");
        reg.shutdownAll();
    }

    static void testMultiLevelCache() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new MultiLevelCacheKernel());
        reg.initAll();
        MultiLevelCacheKernel<String, String> cache = reg.get("multi-level-cache");

        cache.put("foo", "bar");
        assert "bar".equals(cache.get("foo")) : "L1 get failed";
        assert cache.l1Hits() >= 1 : "No L1 hits recorded";

        cache.invalidate("foo");
        assert cache.get("foo") == null : "Invalidation failed";

        pass("MultiLevelCacheKernel");
        reg.shutdownAll();
    }

    static void testTtlCache() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new TtlCacheKernel());
        reg.initAll();
        TtlCacheKernel<String, String> cache = reg.get("ttl-cache");

        cache.put("expiring", "value", 100, TimeUnit.MILLISECONDS);
        assert "value".equals(cache.get("expiring")) : "Immediate get failed";

        Thread.sleep(200);
        assert cache.get("expiring") == null : "TTL expiration failed";

        pass("TtlCacheKernel");
        reg.shutdownAll();
    }

    static void testLockFreeQueue() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new LockFreeQueueKernel());
        reg.initAll();
        LockFreeQueueKernel<Integer> queue = reg.get("lockfree-queue");

        for (int i = 0; i < 100; i++) {
            queue.offer(i);
        }
        assert queue.size() == 100 : "Queue size wrong: " + queue.size();

        int sum = 0;
        Integer item;
        while ((item = queue.poll()) != null) {
            sum += item;
        }
        // Sum 0..99 = 4950
        assert sum == 4950 : "Queue items corrupted: sum=" + sum;

        pass("LockFreeQueueKernel");
        reg.shutdownAll();
    }

    static void testStripedLock() throws Exception {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new StripedLockKernel());
        reg.initAll();
        StripedLockKernel lock = reg.get("striped-lock");

        ConcurrentHashMap<String, Integer> map = new ConcurrentHashMap<>();
        for (int i = 0; i < 100; i++) {
            final String key = "key" + i;
            final int val = i;
            lock.withLock(key, () -> map.put(key, val));
        }
        assert map.size() == 100 : "Striped lock map incomplete";

        pass("StripedLockKernel");
        reg.shutdownAll();
    }

    static void testBufferRecycler() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new BufferRecyclerKernel());
        reg.initAll();
        BufferRecyclerKernel recycler = reg.get("buffer-recycler");

        char[] chars = recycler.borrowChars(1024);
        assert chars.length >= 1024 : "Buffer too small";
        recycler.releaseChars(chars);

        byte[] bytes = recycler.borrowBytes(512);
        assert bytes.length >= 512 : "Byte buffer too small";
        recycler.releaseBytes(bytes);

        pass("BufferRecyclerKernel");
        reg.shutdownAll();
    }

    static void testEventBus() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new EventBusKernel());
        reg.initAll();
        EventBusKernel bus = reg.get("event-bus");

        AtomicInteger counter = new AtomicInteger(0);
        bus.subscribe("test.topic", e -> counter.incrementAndGet());
        bus.publish("test.topic", "hello");
        bus.publish("test.topic", "world");
        assert counter.get() == 2 : "Event not delivered: " + counter.get();

        pass("EventBusKernel");
        reg.shutdownAll();
    }

    static void testFastSerializer() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new FastSerializerKernel());
        reg.initAll();
        FastSerializerKernel ser = reg.get("fast-serializer");

        // String roundtrip
        byte[] data = ser.serialize("hello");
        assert "hello".equals(ser.deserialize(data)) : "String roundtrip failed";

        // Int roundtrip
        data = ser.serialize(42);
        assert ser.deserialize(data).equals(42) : "Int roundtrip failed";

        // Long roundtrip
        data = ser.serialize(123456789L);
        assert ser.deserialize(data).equals(123456789L) : "Long roundtrip failed";

        // Double roundtrip
        data = ser.serialize(3.14);
        assert ((Double) ser.deserialize(data)) == 3.14 : "Double roundtrip failed";

        // Null
        data = ser.serialize(null);
        assert ser.deserialize(data) == null : "Null roundtrip failed";

        pass("FastSerializerKernel");
        reg.shutdownAll();
    }

    static void testKernelRegistryStatus() {
        KernelRegistry reg = KernelRegistry.createIsolated();
        reg.register(new ObjectPoolKernel());
        reg.register(new ByteBufferPoolKernel());
        reg.register(new WorkStealingKernel());
        reg.initAll();

        String report = reg.statusReport();
        assert report.contains("object-pool") : "Status report missing object-pool";
        assert report.contains("bytebuffer-pool") : "Status report missing bytebuffer-pool";
        assert report.contains("work-stealing") : "Status report missing work-stealing";

        pass("KernelRegistry status report");
        reg.shutdownAll();
    }

    static void pass(String name) {
        passed++;
        System.out.println("  ✓ " + name);
    }

    static void fail(String name, String reason) {
        failed++;
        System.out.println("  ✗ " + name + ": " + reason);
    }
}
