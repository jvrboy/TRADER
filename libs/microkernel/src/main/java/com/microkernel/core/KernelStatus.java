package com.microkernel.core;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Enum representing the operational status of a kernel.
 */
public enum KernelStatus {
    UNINITIALIZED,
    INITIALIZING,
    RUNNING,
    DEGRADED,
    STOPPED,
    FAILED
}
