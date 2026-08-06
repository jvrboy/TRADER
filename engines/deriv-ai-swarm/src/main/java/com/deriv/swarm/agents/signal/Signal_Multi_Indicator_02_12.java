package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Multi_Indicator on frxGBPNZD 5m
 * Signal agent implementing Multi_Indicator strategy for frxGBPNZD at 5m.
 */
public class Signal_Multi_Indicator_02_12 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Multi_Indicator_02_12.class);

    public Signal_Multi_Indicator_02_12(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Multi_Indicator_02_12 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Multi_Indicator_02_12 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Multi_Indicator","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPNZD", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Multi_Indicator_02_12 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Multi_Indicator_02_12 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Multi_Indicator_02_12 stopped. Total processed: {}", getMessagesProcessed());
    }
}
