package com.deriv.swarm.agents.execution;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Execution handler: Fill_Rate_Optimizer
 * Execution agent implementing Fill_Rate_Optimizer order execution strategy.
 */
public class Execution_Fill_Rate_Optimizer extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Execution_Fill_Rate_Optimizer.class);

    public Execution_Fill_Rate_Optimizer(String agentId, MessageBus messageBus) {
        super(agentId, "execution", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Execution_Fill_Rate_Optimizer with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Execution_Fill_Rate_Optimizer started");
        // Handle order execution
        scheduleAtFixedRate(() -> {
            broadcast("EXECUTION_STATUS",
                String.format("{"strategy":"Fill_Rate_Optimizer","agent":"%s","status":"READY"}",
                agentId));
        }, 2000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Execution_Fill_Rate_Optimizer received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Execution_Fill_Rate_Optimizer received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Execution_Fill_Rate_Optimizer stopped. Total processed: {}", getMessagesProcessed());
    }
}
