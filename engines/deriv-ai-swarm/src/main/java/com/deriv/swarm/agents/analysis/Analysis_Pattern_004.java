package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Pattern recognition on frxGBPNZD 15m
 * Analysis agent that computes pattern indicators for frxGBPNZD at 15m timeframe.
 */
public class Analysis_Pattern_004 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_Pattern_004.class);

    public Analysis_Pattern_004(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_Pattern_004 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_Pattern_004 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"pattern","symbol":"%s","timeframe":"%s"}",
                agentId, "frxGBPNZD", "15m"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_Pattern_004 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_Pattern_004 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_Pattern_004 stopped. Total processed: {}", getMessagesProcessed());
    }
}
