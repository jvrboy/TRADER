package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Stochastic_Oscillator on frxEURCHF 1m
 * Signal agent implementing Stochastic_Oscillator strategy for frxEURCHF at 1m.
 */
public class Signal_Stochastic_Oscillator_03_05 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Stochastic_Oscillator_03_05.class);

    public Signal_Stochastic_Oscillator_03_05(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Stochastic_Oscillator_03_05 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Stochastic_Oscillator_03_05 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Stochastic_Oscillator","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxEURCHF", "1m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Stochastic_Oscillator_03_05 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Stochastic_Oscillator_03_05 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Stochastic_Oscillator_03_05 stopped. Total processed: {}", getMessagesProcessed());
    }
}
