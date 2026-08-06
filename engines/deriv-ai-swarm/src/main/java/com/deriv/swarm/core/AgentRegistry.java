package com.deriv.swarm.core;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Collection;
import java.util.Collections;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

public class AgentRegistry {
    private static final Logger log = LoggerFactory.getLogger(AgentRegistry.class);
    private final ConcurrentHashMap<String, Agent> agents = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<String, String> agentTypes = new ConcurrentHashMap<>();

    public void register(Agent agent) {
        agents.put(agent.getAgentId(), agent);
        agentTypes.put(agent.getAgentId(), agent.getAgentType());
    }

    public Agent get(String agentId) { return agents.get(agentId); }

    public Collection<Agent> getAll() { return Collections.unmodifiableCollection(agents.values()); }

    public Collection<Agent> getByType(String type) {
        return agents.values().stream()
                .filter(a -> a.getAgentType().equals(type))
                .toList();
    }

    public int count() { return agents.size(); }
    public int countByType(String type) {
        return (int) agentTypes.values().stream().filter(t -> t.equals(type)).count();
    }

    public Map<String, String> getAllTypes() { return Collections.unmodifiableMap(agentTypes); }

    public void remove(String agentId) {
        Agent a = agents.remove(agentId);
        agentTypes.remove(agentId);
        if (a != null) log.info("Removed agent: {}", agentId);
    }
}
