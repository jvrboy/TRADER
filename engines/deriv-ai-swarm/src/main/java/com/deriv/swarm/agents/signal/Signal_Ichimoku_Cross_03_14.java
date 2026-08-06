package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Ichimoku_Cross on frxGBPCAD 1h
 * Signal agent implementing Ichimoku_Cross strategy for frxGBPCAD at 1h.
 */
public class Signal_Ichimoku_Cross_03_14 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Ichimoku_Cross_03_14.class);

    public Signal_Ichimoku_Cross_03_14(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Ichimoku_Cross_03_14 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Ichimoku_Cross_03_14 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Ichimoku_Cross","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPCAD", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Ichimoku_Cross_03_14 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Ichimoku_Cross_03_14 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Ichimoku_Cross_03_14 stopped. Total processed: {}", getMessagesProcessed());
    }
}
