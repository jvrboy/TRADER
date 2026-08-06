package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Keltner_Explosion on frxGBPCAD 1h
 * Signal agent implementing Keltner_Explosion strategy for frxGBPCAD at 1h.
 */
public class Signal_Keltner_Explosion_02_18 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Keltner_Explosion_02_18.class);

    public Signal_Keltner_Explosion_02_18(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Keltner_Explosion_02_18 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Keltner_Explosion_02_18 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Keltner_Explosion","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPCAD", "1h"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Keltner_Explosion_02_18 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Keltner_Explosion_02_18 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Keltner_Explosion_02_18 stopped. Total processed: {}", getMessagesProcessed());
    }
}
