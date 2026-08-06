package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Pivot_Reversal on frxGBPAUD 1t
 * Signal agent implementing Pivot_Reversal strategy for frxGBPAUD at 1t.
 */
public class Signal_Pivot_Reversal_03_22 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Pivot_Reversal_03_22.class);

    public Signal_Pivot_Reversal_03_22(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Pivot_Reversal_03_22 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Pivot_Reversal_03_22 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Pivot_Reversal","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPAUD", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Pivot_Reversal_03_22 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Pivot_Reversal_03_22 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Pivot_Reversal_03_22 stopped. Total processed: {}", getMessagesProcessed());
    }
}
