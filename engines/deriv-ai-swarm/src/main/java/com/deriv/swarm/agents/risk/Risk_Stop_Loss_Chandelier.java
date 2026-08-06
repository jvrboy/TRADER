package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Stop_Loss_Chandelier
 * Risk management agent handling Stop_Loss_Chandelier for the swarm portfolio.
 */
public class Risk_Stop_Loss_Chandelier extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Stop_Loss_Chandelier.class);

    public Risk_Stop_Loss_Chandelier(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Stop_Loss_Chandelier with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Stop_Loss_Chandelier started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Stop_Loss_Chandelier","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Stop_Loss_Chandelier received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Stop_Loss_Chandelier received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Stop_Loss_Chandelier stopped. Total processed: {}", getMessagesProcessed());
    }
}
