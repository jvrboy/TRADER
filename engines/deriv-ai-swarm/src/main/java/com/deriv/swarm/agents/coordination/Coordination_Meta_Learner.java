package com.deriv.swarm.agents.coordination;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Coordinator: Meta_Learner
 * Coordination agent handling Meta_Learner for the entire swarm.
 */
public class Coordination_Meta_Learner extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Coordination_Meta_Learner.class);

    public Coordination_Meta_Learner(String agentId, MessageBus messageBus) {
        super(agentId, "coordination", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Coordination_Meta_Learner with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Coordination_Meta_Learner started");
        // Coordinate swarm activities
        scheduleAtFixedRate(() -> {
            broadcast("COORDINATION_PING",
                String.format("{"coordinator":"Meta_Learner","agent":"%s","timestamp":%d}",
                agentId, System.currentTimeMillis()));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Coordination_Meta_Learner received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Coordination_Meta_Learner received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Coordination_Meta_Learner stopped. Total processed: {}", getMessagesProcessed());
    }
}
