package com.deriv.swarm.core;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.*;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

public class AgentSwarm {
    private static final Logger log = LoggerFactory.getLogger(AgentSwarm.class);
    private final MessageBus messageBus = new MessageBus();
    private final AgentRegistry registry = new AgentRegistry();
    private final Gson gson = new GsonBuilder().setPrettyPrinting().create();
    private volatile boolean running = false;

    public void initialize(List<Agent> agents, Map<String, String> defaultConfig) {
        log.info("Initializing swarm with {} agents...", agents.size());
        agents.forEach(a -> {
            registry.register(a);
            a.initialize(defaultConfig);
        });
        messageBus.start();
        log.info("Swarm initialized. {} agents registered.", registry.count());
    }

    public void start() {
        running = true;
        registry.getAll().forEach(Agent::start);
        log.info("Swarm started with {} agents", registry.count());
        printSwarmStats();
    }

    public void stop() {
        running = false;
        registry.getAll().forEach(Agent::stop);
        messageBus.stop();
        log.info("Swarm stopped");
    }

    public void sendMessage(AgentMessage msg) { messageBus.publish(msg); }
    public AgentRegistry getRegistry() { return registry; }
    public MessageBus getMessageBus() { return messageBus; }
    public boolean isRunning() { return running; }

    public String getStats() {
        Map<String, Object> stats = new LinkedHashMap<>();
        stats.put("totalAgents", registry.count());
        stats.put("totalMessagesProcessed", registry.getAll().stream()
                .mapToLong(Agent::getMessagesProcessed).sum());
        stats.put("totalMessagesSent", registry.getAll().stream()
                .mapToLong(Agent::getMessagesSent).sum());
        stats.put("messageBusQueueSize", messageBus.getQueueSize());
        stats.put("messageBusTotalDispatched", messageBus.getMessageCount());
        Map<String, Integer> typeCounts = new LinkedHashMap<>();
        registry.getAllTypes().values().forEach(t ->
                typeCounts.merge(t, 1, Integer::sum));
        stats.put("agentsByType", typeCounts);
        return gson.toJson(stats);
    }

    private void printSwarmStats() {
        log.info("=== SWARM STATS ===");
        registry.getAllTypes().entrySet().stream()
                .collect(java.util.stream.Collectors.groupingBy(
                        Map.Entry::getValue, java.util.stream.Collectors.counting()))
                .forEach((type, count) -> log.info("  {} : {} agents", type, count));
        log.info("===================");
    }

    public void awaitTermination(long timeout, TimeUnit unit) throws InterruptedException {
        while (running && !Thread.currentThread().isInterrupted()) {
            Thread.sleep(1000);
        }
    }
}
