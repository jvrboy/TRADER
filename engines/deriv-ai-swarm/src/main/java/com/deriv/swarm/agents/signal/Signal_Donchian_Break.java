package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Donchian_Break on frxNZDUSD 15m
 * Signal agent implementing Donchian_Break strategy for frxNZDUSD at 15m.
 */
public class Signal_Donchian_Break extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Donchian_Break.class);

    public Signal_Donchian_Break(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Donchian_Break with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Donchian_Break started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Donchian_Break","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxNZDUSD", "15m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Donchian_Break received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Donchian_Break received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Donchian_Break stopped. Total processed: {}", getMessagesProcessed());
    }
}
