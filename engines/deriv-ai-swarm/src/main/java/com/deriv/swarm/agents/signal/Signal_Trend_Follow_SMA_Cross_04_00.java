package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Trend_Follow_SMA_Cross on frxAUDJPY 1h
 * Signal agent implementing Trend_Follow_SMA_Cross strategy for frxAUDJPY at 1h.
 */
public class Signal_Trend_Follow_SMA_Cross_04_00 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Trend_Follow_SMA_Cross_04_00.class);

    public Signal_Trend_Follow_SMA_Cross_04_00(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Trend_Follow_SMA_Cross_04_00 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Trend_Follow_SMA_Cross_04_00 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Trend_Follow_SMA_Cross","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDJPY", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Trend_Follow_SMA_Cross_04_00 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Trend_Follow_SMA_Cross_04_00 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Trend_Follow_SMA_Cross_04_00 stopped. Total processed: {}", getMessagesProcessed());
    }
}
