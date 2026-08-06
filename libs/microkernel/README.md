# MicroKernel Performance System v1.0.0

## What Is This?

A native Java microkernel architecture containing **15 micro and nano kernels**
that make heavy applications feel lightweight by eliminating the biggest
performance bottlenecks:

- **GC pressure** → Object pooling, buffer recycling, memory monitoring
- **Thread contention** → Work-stealing, lock-free queues, striped locks
- **Cache misses** → LRU, multi-level (L1+L2 soft-reference), TTL caches
- **I/O blocking** → Non-blocking async I/O, buffer recycler
- **Serialization overhead** → Fast byte-level serializer with class caching
- **Event latency** → Zero-dependency in-process event bus

Zero external dependencies. Pure Java 8+. Embed in any application.

---

## 15 Kernels

| # | Kernel | Package | Purpose |
|---|--------|---------|---------|
| 1 | **ObjectPoolKernel** | memory | Recycles mutable objects (StringBuilder, byte[], char[]) to eliminate GC |
| 2 | **ByteBufferPoolKernel** | memory | Pools direct ByteBuffers by size bucket for zero-alloc I/O |
| 3 | **MemoryPoolKernel** | memory | Monitors native memory; triggers controlled GC at thresholds |
| 4 | **WorkStealingKernel** | threading | ForkJoin-based work-stealing that balances load across all cores |
| 5 | **FiberKernel** | threading | Ultra-lightweight task fibers for massive concurrency |
| 6 | **AsyncTaskKernel** | threading | Rate-limited async dispatcher preventing thread explosion |
| 7 | **LruCacheKernel** | cache | Segmented LRU with lock-striping for concurrent O(1) operations |
| 8 | **MultiLevelCacheKernel** | cache | Two-level cache: L1 concurrent map + L2 soft references |
| 9 | **TtlCacheKernel** | cache | TTL-based cache with lazy + periodic eviction |
| 10 | **LockFreeQueueKernel** | concurrency | CAS-based lock-free MPMC queue |
| 11 | **StripedLockKernel** | concurrency | Striped lock spreading contention across N segments |
| 12 | **NonBlockingIOKernel** | io | NIO.2 async file channels with completion handlers |
| 13 | **BufferRecyclerKernel** | io | Recycles char[]/byte[] buffers from string processing |
| 14 | **EventBusKernel** | event | Zero-dependency in-process pub/sub event bus |
| 15 | **FastSerializerKernel** | serialization | Fast byte-level serializer with class cache (10x faster than OOS for primitives) |

---

## Quick Start

### 1. Register & Initialize

```java
import com.microkernel.core.KernelRegistry;
import com.microkernel.memory.*;
import com.microkernel.threading.*;
import com.microkernel.cache.*;
import com.microkernel.concurrency.*;
import com.microkernel.io.*;
import com.microkernel.event.*;
import com.microkernel.serialization.*;

KernelRegistry registry = KernelRegistry.getInstance();

// Register the kernels you need
registry.register(new ObjectPoolKernel());
registry.register(new ByteBufferPoolKernel());
registry.register(new WorkStealingKernel());
registry.register(new LruCacheKernel<String, Object>());
registry.register(new EventBusKernel());
// ... register more as needed

// Initialize all at once (parallel init)
registry.initAll();
```

### 2. Use in Your Application

```java
// === Object Pool: borrow instead of new ===
ObjectPoolKernel pool = registry.get("object-pool");
pool.with(StringBuilder.class, sb -> {
    sb.append("process data without GC");
});

// === ByteBuffer Pool: zero-alloc I/O ===
ByteBufferPoolKernel bufPool = registry.get("bytebuffer-pool");
bufPool.with(4096, buf -> {
    buf.putInt(myData);
    channel.write(buf);
});

// === Work Stealing: parallel processing ===
WorkStealingKernel ws = registry.get("work-stealing");
ws.parallelFor(0, 1_000_000, i -> {
    processItem(data[i]); // auto-balanced across cores
});

// === LRU Cache: concurrent cache ===
LruCacheKernel<String, User> cache = registry.get("lru-cache");
cache.put("user:123", user);
User u = cache.get("user:123"); // O(1), lock-striped

// === Multi-Level Cache: high hit-rate ===
MultiLevelCacheKernel<String, Data> mlCache = registry.get("multi-level-cache");
Data d = mlCache.getOrCompute("expensive-key", key -> computeFromDB(key));
// L1 hit → instant. L1 miss, L2 hit → fast. Full miss → computes and caches.

// === Event Bus: decouple modules ===
EventBusKernel bus = registry.get("event-bus");
bus.subscribe("order.created", event -> notifyInventory(event));
bus.subscribeAsync("order.created", event -> sendAnalytics(event)); // async handler
bus.publish("order.created", orderEvent);

// === Lock-Free Queue: producer-consumer ===
LockFreeQueueKernel<Task> queue = registry.get("lockfree-queue");
queue.offer(task);           // producer
Task t = queue.poll();      // consumer, zero locks

// === Fast Serializer ===
FastSerializerKernel ser = registry.get("fast-serializer");
byte[] data = ser.serialize(myString);  // 10x faster than ObjectOutputStream
String result = ser.deserialize(data);
```

### 3. Monitor Performance

```java
// Status report of all kernels
System.out.println(registry.statusReport());

// Individual kernel metrics
ObjectPoolKernel pool = registry.get("object-pool");
System.out.println(pool.metrics());
// → KernelMetrics[object-pool] ops=50000 errs=0 avg=120ns allocs=256 recycles=49744
```

### 4. Shutdown

```java
registry.shutdownAll(); // Graceful, reverse-order shutdown
```

---

## Build & Test

```bash
# Compile
javac -d target/classes $(find src/main/java -name "*.java")

# Run integration tests
java -cp target/classes com.microkernel.MicroKernelIntegrationTest

# Run benchmarks
java -cp target/classes com.microkernel.MicroKernelBenchmark

# Or with Maven
mvn compile
mvn test
mvn exec:java -Dexec.mainClass="com.microkernel.MicroKernelBenchmark"
```

---

## Embedding in Your App

Copy the `src/main/java/com/microkernel/` directory into your project,
or build the JAR and add it to your classpath.

**Minimum embedding (just pick what you need):**
```java
// Isolated registry per module — no global state
KernelRegistry myReg = KernelRegistry.createIsolated();
myReg.register(new ObjectPoolKernel());
myReg.register(new WorkStealingKernel());
myReg.initAll();

// Your module now has boosted performance
```

---

## Architecture

```
                    ┌──────────────────┐
                    │  KernelRegistry   │
                    │  (lifecycle mgr)  │
                    └────────┬─────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
       ┌──────┴──────┐ ┌─────┴─────┐ ┌─────┴──────┐
       │   MEMORY    │ │ THREADING │ │   CACHE    │
       │ ObjectPool  │ │ WorkSteal │ │ LRU        │
       │ ByteBuffer  │ │ Fiber     │ │ MultiLevel │
       │ MemoryMon   │ │ AsyncTask │ │ TTL        │
       └─────────────┘ └───────────┘ └────────────┘
              │              │              │
       ┌──────┴──────┐ ┌─────┴──────┐ ┌────┴──────┐
       │   I/O       │ │CONCURRENCY │ │  EVENTS   │
       │ NonBlocking │ │ LockFreeQ  │ │ EventBus  │
       │ BufferRecyc │ │ StripedLck │ │           │
       └─────────────┘ └────────────┘ └───────────┘
                             │
                    ┌────────┴────────┐
                    │  SERIALIZATION   │
                    │  FastSerializer  │
                    └─────────────────┘
```

## License
Free to use, modify, and embed in any project.
