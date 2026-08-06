package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Gap_Trading on frxEURCAD 5m
 * Signal agent implementing Gap_Trading strategy for frxEURCAD at 5m.
 */
public class Signal_Gap_Trading extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Gap_Trading.class);

    public Signal_Gap_Trading(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Gap_Trading with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Gap_Trading started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Gap_Trading","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURCAD", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Gap_Trading received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Gap_Trading received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Gap_Trading stopped. Total processed: {}", getMessagesProcessed());
    }
}
