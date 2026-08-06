package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Equal_Risk_Weight
 * Risk management agent handling Equal_Risk_Weight for the swarm portfolio.
 */
public class Risk_Equal_Risk_Weight extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Equal_Risk_Weight.class);

    public Risk_Equal_Risk_Weight(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Equal_Risk_Weight with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Equal_Risk_Weight started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Equal_Risk_Weight","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Equal_Risk_Weight received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Equal_Risk_Weight received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Equal_Risk_Weight stopped. Total processed: {}", getMessagesProcessed());
    }
}
