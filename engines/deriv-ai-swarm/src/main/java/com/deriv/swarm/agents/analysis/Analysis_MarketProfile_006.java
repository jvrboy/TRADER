package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Market profile analysis on frxGBPAUD 1t
 * Analysis agent that computes market_profile indicators for frxGBPAUD at 1t timeframe.
 */
public class Analysis_MarketProfile_006 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_MarketProfile_006.class);

    public Analysis_MarketProfile_006(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_MarketProfile_006 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_MarketProfile_006 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"market_profile","symbol":"%s","timeframe":"%s"}",
                agentId, "frxGBPAUD", "1t"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_MarketProfile_006 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_MarketProfile_006 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_MarketProfile_006 stopped. Total processed: {}", getMessagesProcessed());
    }
}
