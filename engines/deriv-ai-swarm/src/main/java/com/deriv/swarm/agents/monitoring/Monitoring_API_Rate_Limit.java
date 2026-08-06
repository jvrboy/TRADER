package com.deriv.swarm.agents.monitoring;

import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.AgentMessage;
import com.deriv.swarm.core.AgentState;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * System monitor: API_Rate_Limit
 * Monitoring agent responsible for API_Rate_Limit across the swarm.
 */
public class Monitoring_API_Rate_Limit extends Agent {

    private static final Logger log = LoggerFactory.getLogger(Monitoring_API_Rate_Limit.class);

    public Monitoring_API_Rate_Limit(String agentId, MessageBus messageBus) {
        super(agentId, "monitoring", messageBus);
    }

    @Override
    protected void onInitialize(Map<String, String> config) {
        log.info("Initializing Monitoring_API_Rate_Limit with config: {}", config);
    }

    @Override
    protected void onStart() {
        log.info("Monitoring_API_Rate_Limit started");
        // Collect system metrics
        scheduleAtFixedRate(() -> {
            Runtime rt = Runtime.getRuntime();
            long usedMem = (rt.totalMemory() - rt.freeMemory()) / (1024 * 1024);
            broadcast("SYSTEM_METRICS",
                String.format("{"monitor":"API_Rate_Limit","usedMemoryMB":%d,"availableProcessors":%d,"agent":"%s"}",
                usedMem, rt.availableProcessors(), agentId));
        }, 5000);
    }

    @Override
    protected void handleMessage(AgentMessage msg) {
        switch (msg.getType()) {
            case "SHUTDOWN" -> {
                log.info("Monitoring_API_Rate_Limit received shutdown");
                stop();
            }
            case "STATUS_REQUEST" -> {
                sendTo(msg.getSenderId(), "STATUS_RESPONSE", getStatus());
            }
            case "CONFIG_UPDATE" -> {
                log.info("Config update: {}", msg.getPayload());
            }
            default -> {
                if (log.isTraceEnabled()) log.trace("Monitoring_API_Rate_Limit received: {}", msg.getType());
            }
        }
    }

    @Override
    protected void onStop() {
        log.info("Monitoring_API_Rate_Limit stopped. Total processed: {}", getMessagesProcessed());
    }
}
