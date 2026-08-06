package com.deriv.swarm.core;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.*;
import java.util.function.Consumer;

public class MessageBus {
    private static final Logger log = LoggerFactory.getLogger(MessageBus.class);
    private final ConcurrentHashMap<String, CopyOnWriteArrayList<Consumer<AgentMessage>>> subscribers =
            new ConcurrentHashMap<>();
    private final PriorityBlockingQueue<AgentMessage> queue =
            new PriorityBlockingQueue<>(1000, (a, b) -> Integer.compare(b.getPriority(), a.getPriority()));
    private final ExecutorService dispatcher = Executors.newFixedThreadPool(8);
    private volatile boolean running = false;
    private long messageCount = 0;

    public void start() {
        running = true;
        for (int i = 0; i < 4; i++) {
            dispatcher.submit(this::dispatchLoop);
        }
        log.info("MessageBus started with 4 dispatchers");
    }

    private void dispatchLoop() {
        while (running) {
            try {
                AgentMessage msg = queue.poll(100, TimeUnit.MILLISECONDS);
                if (msg != null) {
                    dispatch(msg);
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                break;
            }
        }
    }

    private void dispatch(AgentMessage msg) {
        messageCount++;
        if (msg.isBroadcast()) {
            subscribers.values().forEach(list ->
                    list.forEach(consumer -> {
                        try { consumer.accept(msg); } catch (Exception e) {
                            log.warn("Subscriber error: {}", e.getMessage());
                        }
                    })
            );
        } else {
            CopyOnWriteArrayList<Consumer<AgentMessage>> subs = subscribers.get(msg.getRecipientId());
            if (subs != null) {
                subs.forEach(consumer -> {
                    try { consumer.accept(msg); } catch (Exception e) {
                        log.warn("Subscriber error for {}: {}", msg.getRecipientId(), e.getMessage());
                    }
                });
            }
        }
    }

    public void publish(AgentMessage msg) {
        queue.offer(msg);
    }

    public void subscribe(String agentId, Consumer<AgentMessage> handler) {
        subscribers.computeIfAbsent(agentId, k -> new CopyOnWriteArrayList<>()).add(handler);
    }

    public void subscribeType(String type, String agentId, Consumer<AgentMessage> handler) {
        subscribe(agentId, msg -> {
            if (msg.getType().equals(type)) handler.accept(msg);
        });
    }

    public void unsubscribe(String agentId) {
        subscribers.remove(agentId);
    }

    public void stop() {
        running = false;
        dispatcher.shutdownNow();
        log.info("MessageBus stopped. Total messages dispatched: {}", messageCount);
    }

    public long getMessageCount() { return messageCount; }
    public int getQueueSize() { return queue.size(); }
    public int getSubscriberCount() { return subscribers.size(); }
}
