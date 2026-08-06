package com.microkernel.core;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;

/**
 * Shared context passed to every kernel on init().
 * Kernels can use this to discover each other and share lightweight state.
 */
public class KernelContext {

    private final ConcurrentMap<String, Object> attributes = new ConcurrentHashMap<>();
    private final ConcurrentMap<String, MicroKernel> kernels = new ConcurrentHashMap<>();

    public void setAttribute(String key, Object value) {
        attributes.put(key, value);
    }

    @SuppressWarnings("unchecked")
    public <T> T getAttribute(String key) {
        return (T) attributes.get(key);
    }

    public void registerKernel(MicroKernel kernel) {
        kernels.put(kernel.name(), kernel);
    }

    @SuppressWarnings("unchecked")
    public <T extends MicroKernel> T getKernel(String name) {
        return (T) kernels.get(name);
    }

    public ConcurrentMap<String, MicroKernel> allKernels() {
        return kernels;
    }
}
