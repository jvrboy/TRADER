package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Liquidity_Sweep on frxUSDJPY 5m
 * Signal agent implementing Liquidity_Sweep strategy for frxUSDJPY at 5m.
 */
public class Signal_Liquidity_Sweep extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Liquidity_Sweep.class);

    public Signal_Liquidity_Sweep(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Liquidity_Sweep with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Liquidity_Sweep started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Liquidity_Sweep","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxUSDJPY", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Liquidity_Sweep received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Liquidity_Sweep received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Liquidity_Sweep stopped. Total processed: {}", getMessagesProcessed());
    }
}
