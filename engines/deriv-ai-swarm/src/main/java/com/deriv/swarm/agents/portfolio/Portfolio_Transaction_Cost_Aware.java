package com.deriv.swarm.agents.portfolio;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Portfolio allocator: Transaction_Cost_Aware
 * Portfolio management agent implementing Transaction_Cost_Aware allocation strategy.
 */
public class Portfolio_Transaction_Cost_Aware extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Portfolio_Transaction_Cost_Aware.class);

    public Portfolio_Transaction_Cost_Aware(String agentId, MessageBus messageBus) {
        super(agentId, "portfolio", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Portfolio_Transaction_Cost_Aware with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Portfolio_Transaction_Cost_Aware started");
        // Rebalance portfolio allocation
        scheduleAtFixedRate(() -> {
            broadcast("PORTFOLIO_UPDATE",
                String.format("{"strategy":"Transaction_Cost_Aware","agent":"%s","action":"REBALANCE"}",
                agentId));
        }, 30000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Portfolio_Transaction_Cost_Aware received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Portfolio_Transaction_Cost_Aware received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Portfolio_Transaction_Cost_Aware stopped. Total processed: {}", getMessagesProcessed());
    }
}
