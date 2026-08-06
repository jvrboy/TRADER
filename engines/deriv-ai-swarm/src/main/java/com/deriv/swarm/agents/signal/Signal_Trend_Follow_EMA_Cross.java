package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Trend_Follow_EMA_Cross on frxUSDJPY 5m
 * Signal agent implementing Trend_Follow_EMA_Cross strategy for frxUSDJPY at 5m.
 */
public class Signal_Trend_Follow_EMA_Cross extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Trend_Follow_EMA_Cross.class);

    public Signal_Trend_Follow_EMA_Cross(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Trend_Follow_EMA_Cross with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Trend_Follow_EMA_Cross started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Trend_Follow_EMA_Cross","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxUSDJPY", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Trend_Follow_EMA_Cross received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Trend_Follow_EMA_Cross received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Trend_Follow_EMA_Cross stopped. Total processed: {}", getMessagesProcessed());
    }
}
