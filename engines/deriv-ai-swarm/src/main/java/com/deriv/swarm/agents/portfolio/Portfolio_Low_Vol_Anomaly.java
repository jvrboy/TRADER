package com.deriv.swarm.agents.portfolio;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Portfolio allocator: Low_Vol_Anomaly
 * Portfolio management agent implementing Low_Vol_Anomaly allocation strategy.
 */
public class Portfolio_Low_Vol_Anomaly extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Portfolio_Low_Vol_Anomaly.class);

    public Portfolio_Low_Vol_Anomaly(String agentId, MessageBus messageBus) {
        super(agentId, "portfolio", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Portfolio_Low_Vol_Anomaly with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Portfolio_Low_Vol_Anomaly started");
        // Rebalance portfolio allocation
        scheduleAtFixedRate(() -> {
            broadcast("PORTFOLIO_UPDATE",
                String.format("{"strategy":"Low_Vol_Anomaly","agent":"%s","action":"REBALANCE"}",
                agentId));
        }, 30000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Portfolio_Low_Vol_Anomaly received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Portfolio_Low_Vol_Anomaly received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Portfolio_Low_Vol_Anomaly stopped. Total processed: {}", getMessagesProcessed());
    }
}
