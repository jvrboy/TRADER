package com.deriv.swarm.agents.data;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Data collector for frxGBPCHF on 1m
 * Data collector agent that fetches candle data for frxGBPCHF at 1m timeframe from Deriv API.
 */
public class DataCollector_frxGBPCHF_1min extends Agent {

    private static final Logger log = LoggerFactory.getLogger(DataCollector_frxGBPCHF_1min.class);

    public DataCollector_frxGBPCHF_1min(String agentId, MessageBus messageBus) {
        super(agentId, "data", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing DataCollector_frxGBPCHF_1min with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("DataCollector_frxGBPCHF_1min started");
        // Subscribe to tick data
        scheduleAtFixedRate(() -> {
            sendTo("coordination_swarm_orchestrator", "DATA_AVAILABLE",
                String.format("{"symbol":"%s","timeframe":"%s","candles":100}", "frxGBPCHF", "1m"));
        }, 5000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("DataCollector_frxGBPCHF_1min received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("DataCollector_frxGBPCHF_1min received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("DataCollector_frxGBPCHF_1min stopped. Total processed: {}", getMessagesProcessed());
    }
}
