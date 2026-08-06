package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: VWAP_Strategy on frxEURUSD 1h
 * Signal agent implementing VWAP_Strategy strategy for frxEURUSD at 1h.
 */
public class Signal_VWAP_Strategy_02_13 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_VWAP_Strategy_02_13.class);

    public Signal_VWAP_Strategy_02_13(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_VWAP_Strategy_02_13 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_VWAP_Strategy_02_13 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"VWAP_Strategy","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURUSD", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_VWAP_Strategy_02_13 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_VWAP_Strategy_02_13 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_VWAP_Strategy_02_13 stopped. Total processed: {}", getMessagesProcessed());
    }
}
