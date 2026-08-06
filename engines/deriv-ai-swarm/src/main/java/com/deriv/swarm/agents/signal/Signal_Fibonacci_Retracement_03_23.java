package com.deriv.swarm.agents.signal;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Signal generator: Fibonacci_Retracement on frxAUDCAD 5m
 * Signal agent implementing Fibonacci_Retracement strategy for frxAUDCAD at 5m.
 */
public class Signal_Fibonacci_Retracement_03_23 extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Signal_Fibonacci_Retracement_03_23.class);

    public Signal_Fibonacci_Retracement_03_23(String agentId, MessageBus messageBus) {
        super(agentId, "signal", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Signal_Fibonacci_Retracement_03_23 with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Signal_Fibonacci_Retracement_03_23 started");
        // Generate trading signals
        scheduleAtFixedRate(() -> {
            broadcast("SIGNAL_GENERATED",
                String.format("{"strategy":"Fibonacci_Retracement","symbol":"%s","timeframe":"%s","signal":"ANALYZE"}",
                "frxAUDCAD", "5m"));
        }, 3000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Signal_Fibonacci_Retracement_03_23 received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Signal_Fibonacci_Retracement_03_23 received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Signal_Fibonacci_Retracement_03_23 stopped. Total processed: {}", getMessagesProcessed());
    }
}
