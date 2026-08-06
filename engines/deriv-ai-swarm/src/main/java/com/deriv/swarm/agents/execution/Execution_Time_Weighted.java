package com.deriv.swarm.agents.execution;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Execution handler: Time_Weighted
 * Execution agent implementing Time_Weighted order execution strategy.
 */
public class Execution_Time_Weighted extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Execution_Time_Weighted.class);

    public Execution_Time_Weighted(String agentId, MessageBus messageBus) {
        super(agentId, "execution", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Execution_Time_Weighted with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Execution_Time_Weighted started");
        // Handle order execution
        scheduleAtFixedRate(() -> {
            broadcast("EXECUTION_STATUS",
                String.format("{"strategy":"Time_Weighted","agent":"%s","status":"READY"}",
                agentId));
        }, 2000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Execution_Time_Weighted received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Execution_Time_Weighted received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Execution_Time_Weighted stopped. Total processed: {}", getMessagesProcessed());
    }
}
