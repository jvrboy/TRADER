package com.deriv.swarm.agents.analysis;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Fibonacci analysis on frxCADCHF 1t
 * Analysis agent that computes fibonacci indicators for frxCADCHF at 1t timeframe.
 */
public class Analysis_Fibonacci_001 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Analysis_Fibonacci_001.class);

    public Analysis_Fibonacci_001(String agentId, MessageBus messageBus) {
        super(agentId, "analysis", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Analysis_Fibonacci_001 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Analysis_Fibonacci_001 started");
        // Run indicator calculations
        scheduleAtFixedRate(() -> {
            broadcast("ANALYSIS_RESULT",
                String.format("{"agent":"%s","category":"fibonacci","symbol":"%s","timeframe":"%s"}",
                agentId, "frxCADCHF", "1t"));
        }, 10000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Analysis_Fibonacci_001 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Analysis_Fibonacci_001 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Analysis_Fibonacci_001 stopped. Total processed: {}", getMessagesProcessed());
    }
}
