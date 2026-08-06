package com.microkernel.memory;

import com.microkernel.core.*;

import java.lang.management.ManagementFactory;
import java.lang.management.MemoryMXBean;
import java.lang.management.MemoryUsage;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Micro-kernel: Monitors JVM memory usage and provides manual
 * compaction hints. Useful for apps that allocate many direct buffers.
 *
 * This kernel tracks the JVM's heap + non-heap memory and can trigger
 * System.gc() in a controlled way when thresholds are exceeded.
 */
public class MemoryPoolKernel implements MicroKernel {

    private long warningThresholdBytes = 256 * 1024 * 1024; // 256 MB
    private long criticalThresholdBytes = 512 * 1024 * 1024; // 512 MB
    private volatile long peakMemoryUsed = 0;
    private final AtomicLong gcTriggerCount = new AtomicLong(0);
    private KernelMetrics metrics;
    private MemoryMXBean memoryBean;

    @Override
    public String name() { return "memory-pool"; }

    @Override
    public String description() {
        return "Monitors JVM memory and triggers controlled GC when thresholds are hit";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("memory-pool");
        memoryBean = ManagementFactory.getMemoryMXBean();
    }

    /** Get total heap memory used */
    public long getHeapMemoryUsed() {
        MemoryUsage heap = memoryBean.getHeapMemoryUsage();
        return heap.getUsed();
    }

    /** Get total non-heap memory used */
    public long getNonHeapMemoryUsed() {
        MemoryUsage nonHeap = memoryBean.getNonHeapMemoryUsage();
        return nonHeap.getUsed();
    }

    /** Get total memory used (heap + non-heap) */
    public long getTotalMemoryUsed() {
        return getHeapMemoryUsed() + getNonHeapMemoryUsed();
    }

    /** Get max heap memory available */
    public long getMaxHeapMemory() {
        return memoryBean.getHeapMemoryUsage().getMax();
    }

    /** Heap memory as percentage of max */
    public double heapUsagePercent() {
        long used = getHeapMemoryUsed();
        long max = getMaxHeapMemory();
        return max == 0 ? 0.0 : (used * 100.0 / max);
    }

    /** Check memory and trigger GC if above critical threshold */
    public MemoryCheckResult checkMemory() {
        long start = System.nanoTime();
        long totalUsed = getTotalMemoryUsed();
        if (totalUsed > peakMemoryUsed) {
            peakMemoryUsed = totalUsed;
        }
        MemoryCheckResult result = new MemoryCheckResult(totalUsed, peakMemoryUsed);

        if (totalUsed > criticalThresholdBytes) {
            System.gc();
            gcTriggerCount.incrementAndGet();
            result.level = MemoryLevel.CRITICAL;
            result.gcTriggered = true;
        } else if (totalUsed > warningThresholdBytes) {
            result.level = MemoryLevel.WARNING;
        } else {
            result.level = MemoryLevel.OK;
        }
        metrics.recordOperation(System.nanoTime() - start);
        return result;
    }

    /** Set thresholds in MB */
    public void setThresholds(int warningMB, int criticalMB) {
        this.warningThresholdBytes = warningMB * 1024L * 1024L;
        this.criticalThresholdBytes = criticalMB * 1024L * 1024L;
    }

    public long getPeakMemoryUsed() { return peakMemoryUsed; }
    public long getGcTriggerCount()  { return gcTriggerCount.get(); }

    @Override
    public void shutdown() {}

    @Override
    public KernelStatus status() {
        long used = getTotalMemoryUsed();
        if (used > criticalThresholdBytes) return KernelStatus.DEGRADED;
        return KernelStatus.RUNNING;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }

    /** Result of a memory check */
    public static class MemoryCheckResult {
        public MemoryLevel level;
        public long totalUsed;
        public long peakUsed;
        public boolean gcTriggered;

        MemoryCheckResult(long totalUsed, long peakUsed) {
            this.totalUsed = totalUsed;
            this.peakUsed = peakUsed;
        }
    }

    public enum MemoryLevel { OK, WARNING, CRITICAL }
}
