package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: MACD_Cross on frxEURJPY 1m
 * Signal agent implementing MACD_Cross strategy for frxEURJPY at 1m.
 */
public class Signal_MACD_Cross extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_MACD_Cross.class);

    public Signal_MACD_Cross(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_MACD_Cross with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_MACD_Cross started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"MACD_Cross","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURJPY", "1m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_MACD_Cross received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_MACD_Cross received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_MACD_Cross stopped. Total processed: {}", getMessagesProcessed());
    }
}
