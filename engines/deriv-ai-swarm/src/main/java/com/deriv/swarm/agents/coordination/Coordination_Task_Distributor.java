package com.deriv.swarm.agents.coordination;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Coordinator: Task_Distributor
 * Coordination agent handling Task_Distributor for the entire swarm.
 */
public class Coordination_Task_Distributor extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Coordination_Task_Distributor.class);

    public Coordination_Task_Distributor(String agentId, MessageBus messageBus) {
        super(agentId, "coordination", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Coordination_Task_Distributor with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Coordination_Task_Distributor started");
        // Coordinate swarm activities
        scheduleAtFixedRate(() -> {
            broadcast("COORDINATION_PING",
                String.format("{"coordinator":"Task_Distributor","agent":"%s","timestamp":%d}",
                agentId, System.currentTimeMillis()));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Coordination_Task_Distributor received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Coordination_Task_Distributor received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Coordination_Task_Distributor stopped. Total processed: {}", getMessagesProcessed());
    }
}
