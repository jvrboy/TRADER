package com.deriv.swarm.agents.risk;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Risk manager: Spread_Risk_Monitor
 * Risk management agent handling Spread_Risk_Monitor for the swarm portfolio.
 */
public class Risk_Spread_Risk_Monitor extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Risk_Spread_Risk_Monitor.class);

    public Risk_Spread_Risk_Monitor(String agentId, MessageBus messageBus) {
        super(agentId, "risk", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Risk_Spread_Risk_Monitor with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Risk_Spread_Risk_Monitor started");
        // Monitor risk parameters
        scheduleAtFixedRate(() -> {
            broadcast("RISK_UPDATE",
                String.format("{"risk_type":"Spread_Risk_Monitor","status":"MONITORING","agent":"%s"}",
                agentId));
        }, 15000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Risk_Spread_Risk_Monitor received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Risk_Spread_Risk_Monitor received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Risk_Spread_Risk_Monitor stopped. Total processed: {}", getMessagesProcessed());
    }
}
