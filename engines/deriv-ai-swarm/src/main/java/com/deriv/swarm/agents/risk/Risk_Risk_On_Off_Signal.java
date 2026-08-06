package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Risk_On_Off_Signal
 * Risk management agent handling Risk_On_Off_Signal for the swarm portfolio.
 */
public class Risk_Risk_On_Off_Signal extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Risk_On_Off_Signal.class);

    public Risk_Risk_On_Off_Signal(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Risk_On_Off_Signal with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Risk_On_Off_Signal started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Risk_On_Off_Signal","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Risk_On_Off_Signal received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Risk_On_Off_Signal received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Risk_On_Off_Signal stopped. Total processed: {}", getMessagesProcessed());
    }
}
