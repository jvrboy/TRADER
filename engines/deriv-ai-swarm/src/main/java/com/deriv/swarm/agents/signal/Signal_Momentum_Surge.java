package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Momentum_Surge on frxGBPUSD 15m
 * Signal agent implementing Momentum_Surge strategy for frxGBPUSD at 15m.
 */
public class Signal_Momentum_Surge extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Momentum_Surge.class);

    public Signal_Momentum_Surge(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Momentum_Surge with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Momentum_Surge started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Momentum_Surge","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPUSD", "15m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Momentum_Surge received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Momentum_Surge received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Momentum_Surge stopped. Total processed: {}", getMessagesProcessed());
    }
}
