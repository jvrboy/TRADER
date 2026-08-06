package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Pattern_Recognition on frxEURGBP 5m
 * Signal agent implementing Pattern_Recognition strategy for frxEURGBP at 5m.
 */
public class Signal_Pattern_Recognition extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Pattern_Recognition.class);

    public Signal_Pattern_Recognition(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Pattern_Recognition with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Pattern_Recognition started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Pattern_Recognition","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURGBP", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Pattern_Recognition received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Pattern_Recognition received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Pattern_Recognition stopped. Total processed: {}", getMessagesProcessed());
    }
}
