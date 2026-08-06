package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Stochastic_Oscillator on frxGBPAUD 1t
 * Signal agent implementing Stochastic_Oscillator strategy for frxGBPAUD at 1t.
 */
public class Signal_Stochastic_Oscillator extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Stochastic_Oscillator.class);

    public Signal_Stochastic_Oscillator(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Stochastic_Oscillator with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Stochastic_Oscillator started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Stochastic_Oscillator","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxGBPAUD", "1t"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Stochastic_Oscillator received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Stochastic_Oscillator received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Stochastic_Oscillator stopped. Total processed: {}", getMessagesProcessed());
    }
}
