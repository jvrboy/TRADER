package com.deriv.swarm.agents.coordination;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Coordinator: Topology_Manager
 * Coordination agent handling Topology_Manager for the entire swarm.
 */
public class Coordination_Topology_Manager extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Coordination_Topology_Manager.class);

    public Coordination_Topology_Manager(String agentId, MessageBus messageBus) {
        super(agentId, "coordination", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Coordination_Topology_Manager with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Coordination_Topology_Manager started");
        // Coordinate swarm activities
        scheduleAtFixedRate(() -> {
            broadcast("COORDINATION_PING",
                String.format("{"coordinator":"Topology_Manager","agent":"%s","timestamp":%d}",
                agentId, System.currentTimeMillis()));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Coordination_Topology_Manager received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Coordination_Topology_Manager received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Coordination_Topology_Manager stopped. Total processed: {}", getMessagesProcessed());
    }
}
