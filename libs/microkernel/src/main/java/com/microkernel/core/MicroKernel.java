package com.microkernel.core;

/**
 * Base interface for all microkernels.
 * Every microkernel fragment must implement this to be registered
 * in the KernelRegistry and participate in the lifecycle.
 */
public interface MicroKernel {

    /** Unique name identifying this kernel */
    String name();

    /** One-line description of what this kernel optimizes */
    String description();

    /** Called once when the kernel is registered */
    void init(KernelContext context);

    /** Graceful shutdown — release all native resources */
    void shutdown();

    /** Current health status of this kernel */
    KernelStatus status();

    /** Expose internal metrics for monitoring */
    KernelMetrics metrics();
}
