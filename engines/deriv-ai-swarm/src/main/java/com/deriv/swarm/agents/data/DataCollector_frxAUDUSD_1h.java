package com.deriv.swarm.agents.data;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Data collector for frxAUDUSD on 1h
 * Data collector agent that fetches candle data for frxAUDUSD at 1h timeframe from Deriv API.
 */
public class DataCollector_frxAUDUSD_1h extends Agent {

    private static final Logger log = LoggerFactory.getLogger(DataCollector_frxAUDUSD_1h.class);

    public DataCollector_frxAUDUSD_1h(String agentId, MessageBus messageBus) {
        super(agentId, "data", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing DataCollector_frxAUDUSD_1h with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("DataCollector_frxAUDUSD_1h started");
        // Subscribe to tick data
        scheduleAtFixedRate(() -> {
            sendTo("coordination_swarm_orchestrator", "DATA_AVAILABLE",
                String.format("{"symbol":"%s","timeframe":"%s","candles":100}", "frxAUDUSD", "1h"));
        }, 5000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("DataCollector_frxAUDUSD_1h received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("DataCollector_frxAUDUSD_1h received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("DataCollector_frxAUDUSD_1h stopped. Total processed: {}", getMessagesProcessed());
    }
}
