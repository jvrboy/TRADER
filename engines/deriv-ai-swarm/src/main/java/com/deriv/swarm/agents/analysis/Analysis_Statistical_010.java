package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Statistical analysis on frxAUDNZD 1h
 * Analysis agent that computes statistical indicators for frxAUDNZD at 1h timeframe.
 */
public class Analysis_Statistical_010 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_Statistical_010.class);

    public Analysis_Statistical_010(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_Statistical_010 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_Statistical_010 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"statistical","symbol":"%s","timeframe":"%s"}",
                agentId, "frxAUDNZD", "1h"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_Statistical_010 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_Statistical_010 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_Statistical_010 stopped. Total processed: {}", getMessagesProcessed());
    }
}
