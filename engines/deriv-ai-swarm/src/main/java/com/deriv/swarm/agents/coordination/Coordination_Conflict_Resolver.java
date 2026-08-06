package com.deriv.swarm.agents.coordination;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Coordinator: Conflict_Resolver
 * Coordination agent handling Conflict_Resolver for the entire swarm.
 */
public class Coordination_Conflict_Resolver extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Coordination_Conflict_Resolver.class);

    public Coordination_Conflict_Resolver(String agentId, MessageBus messageBus) {
        super(agentId, "coordination", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Coordination_Conflict_Resolver with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Coordination_Conflict_Resolver started");
        // Coordinate swarm activities
        scheduleAtFixedRate(() -> {
            broadcast("COORDINATION_PING",
                String.format("{"coordinator":"Conflict_Resolver","agent":"%s","timestamp":%d}",
                agentId, System.currentTimeMillis()));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Coordination_Conflict_Resolver received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Coordination_Conflict_Resolver received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Coordination_Conflict_Resolver stopped. Total processed: {}", getMessagesProcessed());
    }
}
