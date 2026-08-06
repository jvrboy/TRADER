package com.deriv.swarm.agents.execution;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Execution handler: Market_Order
 * Execution agent implementing Market_Order order execution strategy.
 */
public class Execution_Market_Order extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Execution_Market_Order.class);

    public Execution_Market_Order(String agentId, MessageBus messageBus) {
        super(agentId, "execution", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Execution_Market_Order with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Execution_Market_Order started");
        // Handle order execution
        scheduleAtFixedRate(() -> {
            broadcast("EXECUTION_STATUS",
                String.format("{"strategy":"Market_Order","agent":"%s","status":"READY"}",
                agentId));
        }, 2000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Execution_Market_Order received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Execution_Market_Order received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Execution_Market_Order stopped. Total processed: {}", getMessagesProcessed());
    }
}
