package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Slippage_Estimator
 * Risk management agent handling Slippage_Estimator for the swarm portfolio.
 */
public class Risk_Slippage_Estimator extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Slippage_Estimator.class);

    public Risk_Slippage_Estimator(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Slippage_Estimator with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Slippage_Estimator started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Slippage_Estimator","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Slippage_Estimator received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Slippage_Estimator received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Slippage_Estimator stopped. Total processed: {}", getMessagesProcessed());
    }
}
