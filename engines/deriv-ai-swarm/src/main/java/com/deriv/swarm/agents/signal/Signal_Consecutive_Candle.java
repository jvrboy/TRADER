package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Consecutive_Candle on frxGBPJPY 1t
 * Signal agent implementing Consecutive_Candle strategy for frxGBPJPY at 1t.
 */
public class Signal_Consecutive_Candle extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Consecutive_Candle.class);

    public Signal_Consecutive_Candle(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Consecutive_Candle with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Consecutive_Candle started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Consecutive_Candle","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPJPY", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Consecutive_Candle received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Consecutive_Candle received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Consecutive_Candle stopped. Total processed: {}", getMessagesProcessed());
    }
}
