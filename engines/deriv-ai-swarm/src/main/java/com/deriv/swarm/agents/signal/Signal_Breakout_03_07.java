package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Breakout on frxCADCHF 1t
 * Signal agent implementing Breakout strategy for frxCADCHF at 1t.
 */
public class Signal_Breakout_03_07 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Breakout_03_07.class);

    public Signal_Breakout_03_07(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Breakout_03_07 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Breakout_03_07 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Breakout","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxCADCHF", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Breakout_03_07 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Breakout_03_07 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Breakout_03_07 stopped. Total processed: {}", getMessagesProcessed());
    }
}
