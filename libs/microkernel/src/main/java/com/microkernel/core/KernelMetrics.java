package com.microkernel.core;

import java.util.concurrent.atomic.AtomicLong;

/**
 * Lightweight metrics snapshot for a kernel.
 * All fields are thread-safe atomics so they can be read
 * without synchronization during monitoring.
 */
public class KernelMetrics {

    private final String kernelName;
    private final AtomicLong totalOperations = new AtomicLong(0);
    private final AtomicLong totalErrors = new AtomicLong(0);
    private final AtomicLong totalNanosSpent = new AtomicLong(0);
    private final AtomicLong currentActive = new AtomicLong(0);
    private final AtomicLong peakActive = new AtomicLong(0);
    private final AtomicLong totalAllocations = new AtomicLong(0);
    private final AtomicLong totalRecycles = new AtomicLong(0);

    public KernelMetrics(String kernelName) {
        this.kernelName = kernelName;
    }

    public void recordOperation(long nanosSpent) {
        totalOperations.incrementAndGet();
        totalNanosSpent.addAndGet(nanosSpent);
        long active = currentActive.incrementAndGet();
        long peak = peakActive.get();
        if (active > peak) {
            peakActive.compareAndSet(peak, active);
        }
        currentActive.decrementAndGet();
    }

    public void recordAllocation() { totalAllocations.incrementAndGet(); }
    public void recordRecycle()     { totalRecycles.incrementAndGet(); }
    public void recordError()       { totalErrors.incrementAndGet(); }

    public long totalOperations()   { return totalOperations.get(); }
    public long totalErrors()       { return totalErrors.get(); }
    public long totalNanosSpent()   { return totalNanosSpent.get(); }
    public long currentActive()     { return currentActive.get(); }
    public long peakActive()        { return peakActive.get(); }
    public long totalAllocations()  { return totalAllocations.get(); }
    public long totalRecycles()     { return totalRecycles.get(); }

    public double avgNanosPerOp() {
        long ops = totalOperations.get();
        return ops == 0 ? 0.0 : (double) totalNanosSpent.get() / ops;
    }

    @Override
    public String toString() {
        return String.format(
            "KernelMetrics[%s] ops=%d errs=%d avg=%.0fns allocs=%d recycles=%d",
            kernelName, totalOperations(), totalErrors(),
            avgNanosPerOp(), totalAllocations(), totalRecycles()
        );
    }
}
