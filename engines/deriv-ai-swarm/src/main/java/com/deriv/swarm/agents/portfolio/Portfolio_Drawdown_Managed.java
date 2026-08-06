package com.deriv.swarm.agents.portfolio;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Portfolio allocator: Drawdown_Managed
 * Portfolio management agent implementing Drawdown_Managed allocation strategy.
 */
public class Portfolio_Drawdown_Managed extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Portfolio_Drawdown_Managed.class);

    public Portfolio_Drawdown_Managed(String agentId, MessageBus messageBus) {
        super(agentId, "portfolio", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Portfolio_Drawdown_Managed with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Portfolio_Drawdown_Managed started");
        // Rebalance portfolio allocation
        scheduleAtFixedRate(() -> {
            broadcast("PORTFOLIO_UPDATE",
                String.format("{"strategy":"Drawdown_Managed","agent":"%s","action":"REBALANCE"}",
                agentId));
        }, 30000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Portfolio_Drawdown_Managed received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Portfolio_Drawdown_Managed received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Portfolio_Drawdown_Managed stopped. Total processed: {}", getMessagesProcessed());
    }
}
