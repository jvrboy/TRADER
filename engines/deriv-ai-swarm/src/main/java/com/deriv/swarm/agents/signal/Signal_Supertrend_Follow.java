package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Supertrend_Follow on frxCADCHF 1t
 * Signal agent implementing Supertrend_Follow strategy for frxCADCHF at 1t.
 */
public class Signal_Supertrend_Follow extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Supertrend_Follow.class);

    public Signal_Supertrend_Follow(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Supertrend_Follow with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Supertrend_Follow started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Supertrend_Follow","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxCADCHF", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Supertrend_Follow received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Supertrend_Follow received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Supertrend_Follow stopped. Total processed: {}", getMessagesProcessed());
    }
}
