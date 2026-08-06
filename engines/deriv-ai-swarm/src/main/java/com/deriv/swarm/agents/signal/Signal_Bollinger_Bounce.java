package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Bollinger_Bounce on frxEURAUD 15m
 * Signal agent implementing Bollinger_Bounce strategy for frxEURAUD at 15m.
 */
public class Signal_Bollinger_Bounce extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Bollinger_Bounce.class);

    public Signal_Bollinger_Bounce(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Bollinger_Bounce with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Bollinger_Bounce started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Bollinger_Bounce","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURAUD", "15m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Bollinger_Bounce received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Bollinger_Bounce received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Bollinger_Bounce stopped. Total processed: {}", getMessagesProcessed());
    }
}
