package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Volume_Confirmation on frxAUDUSD 1m
 * Signal agent implementing Volume_Confirmation strategy for frxAUDUSD at 1m.
 */
public class Signal_Volume_Confirmation_03_10 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Volume_Confirmation_03_10.class);

    public Signal_Volume_Confirmation_03_10(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Volume_Confirmation_03_10 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Volume_Confirmation_03_10 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Volume_Confirmation","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDUSD", "1m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Volume_Confirmation_03_10 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Volume_Confirmation_03_10 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Volume_Confirmation_03_10 stopped. Total processed: {}", getMessagesProcessed());
    }
}
