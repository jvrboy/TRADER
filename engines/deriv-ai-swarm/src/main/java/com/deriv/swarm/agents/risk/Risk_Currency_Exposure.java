package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Currency_Exposure
 * Risk management agent handling Currency_Exposure for the swarm portfolio.
 */
public class Risk_Currency_Exposure extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Currency_Exposure.class);

    public Risk_Currency_Exposure(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Currency_Exposure with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Currency_Exposure started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Currency_Exposure","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Currency_Exposure received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Currency_Exposure received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Currency_Exposure stopped. Total processed: {}", getMessagesProcessed());
    }
}
