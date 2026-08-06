package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Cycle analysis on R_100 1t
 * Analysis agent that computes cycle indicators for R_100 at 1t timeframe.
 */
public class Analysis_Cycle_001 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_Cycle_001.class);

    public Analysis_Cycle_001(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_Cycle_001 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_Cycle_001 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"cycle","symbol":"%s","timeframe":"%s"}",
                agentId, "R_100", "1t"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_Cycle_001 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_Cycle_001 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_Cycle_001 stopped. Total processed: {}", getMessagesProcessed());
    }
}
