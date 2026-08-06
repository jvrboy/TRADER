package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Mean_Reversion on frxAUDCAD 5m
 * Signal agent implementing Mean_Reversion strategy for frxAUDCAD at 5m.
 */
public class Signal_Mean_Reversion extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Mean_Reversion.class);

    public Signal_Mean_Reversion(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Mean_Reversion with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Mean_Reversion started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Mean_Reversion","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDCAD", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Mean_Reversion received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Mean_Reversion received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Mean_Reversion stopped. Total processed: {}", getMessagesProcessed());
    }
}
