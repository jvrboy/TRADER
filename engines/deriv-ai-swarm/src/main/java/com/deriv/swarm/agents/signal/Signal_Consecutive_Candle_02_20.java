package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Consecutive_Candle on frxEURNZD 15m
 * Signal agent implementing Consecutive_Candle strategy for frxEURNZD at 15m.
 */
public class Signal_Consecutive_Candle_02_20 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Consecutive_Candle_02_20.class);

    public Signal_Consecutive_Candle_02_20(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Consecutive_Candle_02_20 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Consecutive_Candle_02_20 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Consecutive_Candle","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURNZD", "15m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Consecutive_Candle_02_20 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Consecutive_Candle_02_20 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Consecutive_Candle_02_20 stopped. Total processed: {}", getMessagesProcessed());
    }
}
