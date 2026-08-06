package com.deriv.swarm.core;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.atomic.AtomicReference;

public abstract class Agent {
    private static final Logger log = LoggerFactory.getLogger(Agent.class);
    protected final String agentId;
    protected final String agentType;
    protected final MessageBus messageBus;
    protected final AtomicReference<AgentState> state = new AtomicReference<>(AgentState.CREATED);
    protected final AtomicLong messagesProcessed = new AtomicLong(0);
    protected final AtomicLong messagesSent = new AtomicLong(0);
    protected ScheduledExecutorService scheduler;
    protected Map<String, String> config;

    protected Agent(String agentId, String agentType, MessageBus messageBus) {
        this.agentId = agentId;
        this.agentType = agentType;
        this.messageBus = messageBus;
    }

    public void initialize(Map<String, String> config) {
        this.config = config;
        state.set(AgentState.INITIALIZING);
        this.scheduler = Executors.newSingleThreadScheduledExecutor(r ->
            new Thread(r, "agent-" + agentId));
        messageBus.subscribe(agentId, this::onMessage);
        onInitialize(config);
        state.set(AgentState.IDLE);
        log.info("Agent {} ({}) initialized", agentId, agentType);
    }

    public void start() {
        state.set(AgentState.RUNNING);
        onStart();
        log.info("Agent {} ({}) started", agentId, agentType);
    }

    public void stop() {
        state.set(AgentState.STOPPED);
        if (scheduler != null) scheduler.shutdownNow();
        messageBus.unsubscribe(agentId);
        onStop();
        log.info("Agent {} ({}) stopped. Processed {} msgs, sent {} msgs",
                agentId, agentType, messagesProcessed.get(), messagesSent.get());
    }

    protected void onMessage(AgentMessage msg) {
        messagesProcessed.incrementAndGet();
        handleMessage(msg);
    }

    protected void send(AgentMessage msg) {
        messagesSent.incrementAndGet();
        messageBus.publish(msg);
    }

    protected void broadcast(String type, String payload) {
        send(AgentMessage.broadcast(agentId, type, payload));
    }

    protected void sendTo(String recipientId, String type, String payload) {
        send(new AgentMessage(agentId, recipientId, type, payload));
    }

    protected void scheduleAtFixedRate(Runnable task, long periodMs) {
        scheduler.scheduleAtFixedRate(task, 0, periodMs, TimeUnit.MILLISECONDS);
    }

    protected void scheduleOnce(Runnable task, long delayMs) {
        scheduler.schedule(task, delayMs, TimeUnit.MILLISECONDS);
    }

    // Lifecycle hooks for subclasses
    protected abstract void onInitialize(Map<String, String> config);
    protected abstract void onStart();
    protected abstract void handleMessage(AgentMessage msg);
    protected void onStop() {}

    // Getters
    public String getAgentId() { return agentId; }
    public String getAgentType() { return agentType; }
    public AgentState getState() { return state.get(); }
    public long getMessagesProcessed() { return messagesProcessed.get(); }
    public long getMessagesSent() { return messagesSent.get(); }
    public Map<String, String> getConfig() { return config; }

    public String getStatus() {
        return String.format("Agent[%s] type=%s state=%s processed=%d sent=%d",
                agentId, agentType, state.get(), messagesProcessed.get(), messagesSent.get());
    }
}