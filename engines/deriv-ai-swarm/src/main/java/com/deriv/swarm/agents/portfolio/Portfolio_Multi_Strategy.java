package com.deriv.swarm.agents.portfolio;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Portfolio allocator: Multi_Strategy
 * Portfolio management agent implementing Multi_Strategy allocation strategy.
 */
public class Portfolio_Multi_Strategy extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Portfolio_Multi_Strategy.class);

    public Portfolio_Multi_Strategy(String agentId, MessageBus messageBus) {
        super(agentId, "portfolio", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Portfolio_Multi_Strategy with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Portfolio_Multi_Strategy started");
        // Rebalance portfolio allocation
        scheduleAtFixedRate(() -> {
            broadcast("PORTFOLIO_UPDATE",
                String.format("{"strategy":"Multi_Strategy","agent":"%s","action":"REBALANCE"}",
                agentId));
        }, 30000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Portfolio_Multi_Strategy received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Portfolio_Multi_Strategy received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Portfolio_Multi_Strategy stopped. Total processed: {}", getMessagesProcessed());
    }
}
