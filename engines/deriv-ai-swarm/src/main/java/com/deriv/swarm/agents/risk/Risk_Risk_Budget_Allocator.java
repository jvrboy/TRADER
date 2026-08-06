package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Risk_Budget_Allocator
 * Risk management agent handling Risk_Budget_Allocator for the swarm portfolio.
 */
public class Risk_Risk_Budget_Allocator extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Risk_Budget_Allocator.class);

    public Risk_Risk_Budget_Allocator(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Risk_Budget_Allocator with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Risk_Budget_Allocator started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Risk_Budget_Allocator","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Risk_Budget_Allocator received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Risk_Budget_Allocator received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Risk_Budget_Allocator stopped. Total processed: {}", getMessagesProcessed());
    }
}
