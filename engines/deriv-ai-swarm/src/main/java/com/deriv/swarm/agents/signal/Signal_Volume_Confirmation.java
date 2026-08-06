package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Volume_Confirmation on frxUSDCAD 1t
 * Signal agent implementing Volume_Confirmation strategy for frxUSDCAD at 1t.
 */
public class Signal_Volume_Confirmation extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Volume_Confirmation.class);

    public Signal_Volume_Confirmation(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Volume_Confirmation with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Volume_Confirmation started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Volume_Confirmation","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxUSDCAD", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Volume_Confirmation received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Volume_Confirmation received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Volume_Confirmation stopped. Total processed: {}", getMessagesProcessed());
    }
}
