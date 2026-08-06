package com.microkernel.event;

import com.microkernel.core.*;

import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.Consumer;

/**
 * Micro-kernel: Ultra-lightweight in-process event bus with zero
 * dependencies. Supports synchronous and asynchronous dispatch.
 *
 * Usage:
 *   EventBusKernel bus = registry.get("event-bus");
 *   bus.subscribe("user.created", event -> handleUser(event));
 *   bus.publish("user.created", userEvent);
 */
public class EventBusKernel implements MicroKernel {

    private final ConcurrentHashMapSafe<Class<?>, CopyOnWriteArrayList<Subscriber<?>>> subscribersByType = new ConcurrentHashMapSafe<>();
    private final ConcurrentHashMapSafe<String, CopyOnWriteArrayList<Subscriber<?>>> subscribersByString = new ConcurrentHashMapSafe<>();
    private ExecutorService asyncPool;
    private KernelMetrics metrics;

    @Override
    public String name() { return "event-bus"; }

    @Override
    public String description() {
        return "Lightweight in-process event bus with sync/async dispatch and zero dependencies";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("event-bus");
        asyncPool = Executors.newFixedThreadPool(
            Runtime.getRuntime().availableProcessors(),
            r -> {
                Thread t = new Thread(r, "event-bus-");
                t.setDaemon(true);
                return t;
            }
        );
    }

    /** Subscribe to events by string topic */
    public <T> void subscribe(String topic, Consumer<T> handler) {
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByString
            .computeIfAbsent(topic, k -> new CopyOnWriteArrayList<>());
        subs.add(new Subscriber<>(handler, false));
    }

    /** Subscribe to events by class type */
    public <T> void subscribe(Class<T> eventType, Consumer<T> handler) {
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByType
            .computeIfAbsent(eventType, k -> new CopyOnWriteArrayList<>());
        subs.add(new Subscriber<>(handler, false));
    }

    /** Subscribe with async delivery */
    public <T> void subscribeAsync(String topic, Consumer<T> handler) {
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByString
            .computeIfAbsent(topic, k -> new CopyOnWriteArrayList<>());
        subs.add(new Subscriber<>(handler, true));
    }

    /** Subscribe by class type with async delivery */
    public <T> void subscribeAsync(Class<T> eventType, Consumer<T> handler) {
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByType
            .computeIfAbsent(eventType, k -> new CopyOnWriteArrayList<>());
        subs.add(new Subscriber<>(handler, true));
    }

    /** Publish event to string topic (sync) */
    @SuppressWarnings("unchecked")
    public void publish(String topic, Object event) {
        long start = System.nanoTime();
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByString.get(topic);
        if (subs != null) {
            for (Subscriber<?> sub : subs) {
                dispatch((Subscriber<Object>) sub, event);
            }
        }
        metrics.recordOperation(System.nanoTime() - start);
    }

    /** Publish event by type (sync) */
    @SuppressWarnings("unchecked")
    public void publish(Object event) {
        long start = System.nanoTime();
        CopyOnWriteArrayList<Subscriber<?>> subs = subscribersByType.get(event.getClass());
        if (subs != null) {
            for (Subscriber<?> sub : subs) {
                dispatch((Subscriber<Object>) sub, event);
            }
        }
        metrics.recordOperation(System.nanoTime() - start);
    }

    @SuppressWarnings("unchecked")
    private <T> void dispatch(Subscriber<T> sub, Object event) {
        if (sub.async) {
            asyncPool.submit(() -> {
                try {
                    sub.handler.accept((T) event);
                } catch (Exception e) {
                    metrics.recordError();
                }
            });
        } else {
            try {
                sub.handler.accept((T) event);
            } catch (Exception e) {
                metrics.recordError();
            }
        }
    }

    /** Unsubscribe all handlers for a topic */
    public void unsubscribe(String topic) {
        subscribersByString.remove(topic);
    }

    /** Unsubscribe all handlers for a type */
    public void unsubscribe(Class<?> type) {
        subscribersByType.remove(type);
    }

    /** Simple ConcurrentHashMap-safe computeIfAbsent for Java 8+ */
    private static class ConcurrentHashMapSafe<K, V> extends java.util.concurrent.ConcurrentHashMap<K, V> {}

    private static class Subscriber<T> {
        final Consumer<T> handler;
        final boolean async;
        Subscriber(Consumer<T> handler, boolean async) {
            this.handler = handler;
            this.async = async;
        }
    }

    @Override
    public void shutdown() {
        if (asyncPool != null) asyncPool.shutdownNow();
        subscribersByType.clear();
        subscribersByString.clear();
    }

    @Override
    public KernelStatus status() {
        return asyncPool.isShutdown() ? KernelStatus.STOPPED : KernelStatus.RUNNING;
    }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
