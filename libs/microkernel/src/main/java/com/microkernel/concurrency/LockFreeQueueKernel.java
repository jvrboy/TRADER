package com.microkernel.concurrency;

import com.microkernel.core.*;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;
import java.util.concurrent.atomic.AtomicReferenceArray;
import java.util.Objects;

/**
 * Nano-kernel: Lock-free unbounded multi-producer multi-consumer queue
 * based on a ring buffer with CAS-based indices.
 *
 * This is the foundational concurrent structure that other kernels
 * use for high-throughput message passing.
 */
public class LockFreeQueueKernel<E> implements MicroKernel {

    @SuppressWarnings("unchecked")
    private final AtomicReferenceArray<E> buffer;
    private final AtomicReference<Node<E>> head;
    private final AtomicReference<Node<E>> tail;
    private final AtomicInteger size = new AtomicInteger(0);
    private KernelMetrics metrics;

    /** Use linked-node approach for unbounded MPMC */
    private static class Node<E> {
        final E value;
        final AtomicReference<Node<E>> next = new AtomicReference<>();
        Node(E value) { this.value = value; }
    }

    public LockFreeQueueKernel() {
        this.buffer = null; // not used in linked version
        Node<E> sentinel = new Node<>(null);
        head = new AtomicReference<>(sentinel);
        tail = new AtomicReference<>(sentinel);
    }

    @Override
    public String name() { return "lockfree-queue"; }

    @Override
    public String description() {
        return "Lock-free MPMC queue using CAS-based linked nodes for zero-contention messaging";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("lockfree-queue");
    }

    /** Enqueue: CAS-loop to append node to tail */
    public boolean offer(E item) {
        Objects.requireNonNull(item);
        long start = System.nanoTime();
        try {
            Node<E> newNode = new Node<>(item);
            while (true) {
                Node<E> currentTail = tail.get();
                Node<E> next = currentTail.next.get();
                if (currentTail == tail.get()) {
                    if (next == null) {
                        if (currentTail.next.compareAndSet(null, newNode)) {
                            tail.compareAndSet(currentTail, newNode);
                            size.incrementAndGet();
                            return true;
                        }
                    } else {
                        tail.compareAndSet(currentTail, next);
                    }
                }
            }
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Dequeue: CAS-loop to pop node from head */
    public E poll() {
        long start = System.nanoTime();
        try {
            while (true) {
                Node<E> currentHead = head.get();
                Node<E> currentTail = tail.get();
                Node<E> next = currentHead.next.get();
                if (currentHead == head.get()) {
                    if (currentHead == currentTail) {
                        if (next == null) return null;
                        tail.compareAndSet(currentTail, next);
                    } else {
                        E value = next.value;
                        if (head.compareAndSet(currentHead, next)) {
                            size.decrementAndGet();
                            return value;
                        }
                    }
                }
            }
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Peek at head without removing */
    public E peek() {
        Node<E> h = head.get();
        Node<E> n = h.next.get();
        return n == null ? null : n.value;
    }

    public int size() { return Math.max(0, size.get()); }

    /** Drain all elements into an array (snapshot) */
    @SuppressWarnings("unchecked")
    public E[] drainToArray(int maxSize) {
        Object[] arr = new Object[Math.min(size.get(), maxSize)];
        int count = 0;
        E item;
        while (count < maxSize && (item = poll()) != null) {
            arr[count++] = item;
        }
        return (E[]) arr;
    }

    @Override
    public void shutdown() {
        // Drain remaining
        while (poll() != null) {}
    }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
