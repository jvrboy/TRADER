package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Institutional_Level on frxUSDCHF 1h
 * Signal agent implementing Institutional_Level strategy for frxUSDCHF at 1h.
 */
public class Signal_Institutional_Level extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Institutional_Level.class);

    public Signal_Institutional_Level(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Institutional_Level with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Institutional_Level started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Institutional_Level","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxUSDCHF", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Institutional_Level received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Institutional_Level received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Institutional_Level stopped. Total processed: {}", getMessagesProcessed());
    }
}
