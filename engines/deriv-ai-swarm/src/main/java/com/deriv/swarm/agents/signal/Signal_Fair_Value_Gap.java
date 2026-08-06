package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Fair_Value_Gap on R_100 1t
 * Signal agent implementing Fair_Value_Gap strategy for R_100 at 1t.
 */
public class Signal_Fair_Value_Gap extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Fair_Value_Gap.class);

    public Signal_Fair_Value_Gap(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Fair_Value_Gap with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Fair_Value_Gap started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Fair_Value_Gap","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "R_100", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Fair_Value_Gap received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Fair_Value_Gap received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Fair_Value_Gap stopped. Total processed: {}", getMessagesProcessed());
    }
}
