package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Supertrend_Follow on frxAUDCHF 1m
 * Signal agent implementing Supertrend_Follow strategy for frxAUDCHF at 1m.
 */
public class Signal_Supertrend_Follow_03_15 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Supertrend_Follow_03_15.class);

    public Signal_Supertrend_Follow_03_15(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Supertrend_Follow_03_15 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Supertrend_Follow_03_15 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Supertrend_Follow","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDCHF", "1m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Supertrend_Follow_03_15 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Supertrend_Follow_03_15 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Supertrend_Follow_03_15 stopped. Total processed: {}", getMessagesProcessed());
    }
}
