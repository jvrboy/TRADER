package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Risk_Reward_Calculator
 * Risk management agent handling Risk_Reward_Calculator for the swarm portfolio.
 */
public class Risk_Risk_Reward_Calculator extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Risk_Reward_Calculator.class);

    public Risk_Risk_Reward_Calculator(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Risk_Reward_Calculator with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Risk_Reward_Calculator started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Risk_Reward_Calculator","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Risk_Reward_Calculator received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Risk_Reward_Calculator received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Risk_Reward_Calculator stopped. Total processed: {}", getMessagesProcessed());
    }
}
