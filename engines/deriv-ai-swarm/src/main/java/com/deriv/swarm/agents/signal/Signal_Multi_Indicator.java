package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Multi_Indicator on frxAUDJPY 1h
 * Signal agent implementing Multi_Indicator strategy for frxAUDJPY at 1h.
 */
public class Signal_Multi_Indicator extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Multi_Indicator.class);

    public Signal_Multi_Indicator(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Multi_Indicator with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Multi_Indicator started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Multi_Indicator","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDJPY", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Multi_Indicator received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Multi_Indicator received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Multi_Indicator stopped. Total processed: {}", getMessagesProcessed());
    }
}
