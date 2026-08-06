package com.microkernel.core;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArrayList;
import java.util.logging.Logger;

/**
 * Central registry that manages lifecycle of all microkernels.
 * Usage:
 *   KernelRegistry registry = KernelRegistry.getInstance();
 *   registry.register(new MemoryPoolKernel());
 *   registry.register(new WorkStealingKernel());
 *   registry.initAll();
 *   // ... use kernels ...
 *   registry.shutdownAll();
 */
public final class KernelRegistry {

    private static final Logger LOG = Logger.getLogger(KernelRegistry.class.getName());
    private static volatile KernelRegistry instance;

    private final CopyOnWriteArrayList<MicroKernel> kernels = new CopyOnWriteArrayList<>();
    private final ConcurrentHashMap<String, MicroKernel> kernelMap = new ConcurrentHashMap<>();
    private final KernelContext globalContext = new KernelContext();
    private volatile boolean initialized = false;

    private KernelRegistry() {}

    public static KernelRegistry getInstance() {
        if (instance == null) {
            synchronized (KernelRegistry.class) {
                if (instance == null) {
                    instance = new KernelRegistry();
                }
            }
        }
        return instance;
    }

    /** Alternative: create an isolated registry for testing */
    public static KernelRegistry createIsolated() {
        return new KernelRegistry();
    }

    /** Register a kernel (must call before initAll) */
    public void register(MicroKernel kernel) {
        if (initialized) {
            throw new IllegalStateException(
                "Cannot register kernel after initAll() has been called");
        }
        kernelMap.put(kernel.name(), kernel);
        kernels.add(kernel);
        globalContext.registerKernel(kernel);
        LOG.info("Registered kernel: " + kernel.name() + " — " + kernel.description());
    }

    /** Initialize all registered kernels in parallel */
    public void initAll() {
        if (initialized) return;
        initialized = true;
        LOG.info("Initializing " + kernels.size() + " kernels ...");

        kernels.parallelStream().forEach(kernel -> {
            try {
                kernel.init(globalContext);
                LOG.info("Initialized: " + kernel.name());
            } catch (Exception e) {
                LOG.severe("Failed to init " + kernel.name() + ": " + e.getMessage());
                throw new RuntimeException("Kernel init failed: " + kernel.name(), e);
            }
        });
    }

    /** Gracefully shut down all kernels in reverse order */
    public void shutdownAll() {
        LOG.info("Shutting down " + kernels.size() + " kernels ...");
        for (int i = kernels.size() - 1; i >= 0; i--) {
            MicroKernel kernel = kernels.get(i);
            try {
                kernel.shutdown();
                LOG.info("Shutdown: " + kernel.name());
            } catch (Exception e) {
                LOG.warning("Error shutting down " + kernel.name() + ": " + e.getMessage());
            }
        }
        initialized = false;
    }

    /** Get a specific kernel by name */
    @SuppressWarnings("unchecked")
    public <T extends MicroKernel> T get(String name) {
        return (T) kernelMap.get(name);
    }

    /** Get the shared context */
    public KernelContext getContext() {
        return globalContext;
    }

    /** List all registered kernel names */
    public List<String> listKernels() {
        List<String> names = new ArrayList<>();
        for (MicroKernel k : kernels) names.add(k.name());
        return names;
    }

    /** Print status summary of all kernels */
    public String statusReport() {
        StringBuilder sb = new StringBuilder();
        sb.append("=== MicroKernel System Status ===\n");
        for (MicroKernel k : kernels) {
            sb.append(String.format("  %-25s [%-14s] %s\n",
                k.name(), k.status(), k.metrics().toString()));
        }
        return sb.toString();
    }
}
