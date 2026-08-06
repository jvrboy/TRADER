package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: ATR_Channel on R_100 1t
 * Signal agent implementing ATR_Channel strategy for R_100 at 1t.
 */
public class Signal_ATR_Channel_03_17 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_ATR_Channel_03_17.class);

    public Signal_ATR_Channel_03_17(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_ATR_Channel_03_17 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_ATR_Channel_03_17 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"ATR_Channel","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "R_100", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_ATR_Channel_03_17 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_ATR_Channel_03_17 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_ATR_Channel_03_17 stopped. Total processed: {}", getMessagesProcessed());
    }
}
