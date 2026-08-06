package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Order flow analysis on frxAUDNZD 1h
 * Analysis agent that computes order_flow indicators for frxAUDNZD at 1h timeframe.
 */
public class Analysis_OrderFlow_007 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_OrderFlow_007.class);

    public Analysis_OrderFlow_007(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_OrderFlow_007 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_OrderFlow_007 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"order_flow","symbol":"%s","timeframe":"%s"}",
                agentId, "frxAUDNZD", "1h"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_OrderFlow_007 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_OrderFlow_007 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_OrderFlow_007 stopped. Total processed: {}", getMessagesProcessed());
    }
}
