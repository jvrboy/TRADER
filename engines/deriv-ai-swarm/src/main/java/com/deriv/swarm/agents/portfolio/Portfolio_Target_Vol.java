package com.deriv.swarm.agents.portfolio;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Portfolio allocator: Target_Vol
 * Portfolio management agent implementing Target_Vol allocation strategy.
 */
public class Portfolio_Target_Vol extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Portfolio_Target_Vol.class);

    public Portfolio_Target_Vol(String agentId, MessageBus messageBus) {
        super(agentId, "portfolio", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Portfolio_Target_Vol with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Portfolio_Target_Vol started");
        // Rebalance portfolio allocation
        scheduleAtFixedRate(() -> {
            broadcast("PORTFOLIO_UPDATE",
                String.format("{"strategy":"Target_Vol","agent":"%s","action":"REBALANCE"}",
                agentId));
        }, 30000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Portfolio_Target_Vol received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Portfolio_Target_Vol received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Portfolio_Target_Vol stopped. Total processed: {}", getMessagesProcessed());
    }
}
