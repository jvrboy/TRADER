package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Position_Sizer_Volatility
 * Risk management agent handling Position_Sizer_Volatility for the swarm portfolio.
 */
public class Risk_Position_Sizer_Volatility extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Position_Sizer_Volatility.class);

    public Risk_Position_Sizer_Volatility(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Position_Sizer_Volatility with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Position_Sizer_Volatility started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Position_Sizer_Volatility","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Position_Sizer_Volatility received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Position_Sizer_Volatility received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Position_Sizer_Volatility stopped. Total processed: {}", getMessagesProcessed());
    }
}
