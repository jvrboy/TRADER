package com.deriv.swarm;

import com.deriv.swarm.config.SwarmConfig;
import com.deriv.swarm.core.Agent;
import com.deriv.swarm.core.MessageBus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.net.URL;
import java.util.*;
import java.util.stream.Collectors;

/**
 * Dynamically discovers and instantiates all 500 agent classes
 * from the classpath using reflection.
 */
public class SwarmBuilder {
    private static final Logger log = LoggerFactory.getLogger(SwarmBuilder.class);

    private final SwarmConfig config;
    private final MessageBus messageBus;

    // Package prefixes for each agent type
    private static final Map<String, String> TYPE_PACKAGES = Map.of(
        "data", "com.deriv.swarm.agents.data",
        "analysis", "com.deriv.swarm.agents.analysis",
        "signal", "com.deriv.swarm.agents.signal",
        "risk", "com.deriv.swarm.agents.risk",
        "portfolio", "com.deriv.swarm.agents.portfolio",
        "execution", "com.deriv.swarm.agents.execution",
        "monitoring", "com.deriv.swarm.agents.monitoring",
        "coordination", "com.deriv.swarm.agents.coordination"
    );

    private static final Map<String, Integer> TYPE_COUNTS = Map.ofEntries(
        Map.entry("data", 100),
        Map.entry("analysis", 150),
        Map.entry("signal", 100),
        Map.entry("risk", 60),
        Map.entry("portfolio", 40),
        Map.entry("execution", 25),
        Map.entry("monitoring", 15),
        Map.entry("coordination", 10)
    );

    public SwarmBuilder(SwarmConfig config) {
        this.config = config;
        this.messageBus = new MessageBus();
    }

    public List<Agent> buildAll() {
        List<Agent> agents = new ArrayList<>();
        int totalExpected = config.getTotalAgents();
        log.info("Building swarm targeting {} total agents...", totalExpected);

        // Build in priority order: coordination, monitoring, data, analysis, signal, risk, portfolio, execution
        List<String> buildOrder = List.of("coordination", "monitoring", "data", "analysis",
                "signal", "risk", "portfolio", "execution");

        for (String agentType : buildOrder) {
            int desired = TYPE_COUNTS.getOrDefault(agentType, 0);
            String pkg = TYPE_PACKAGES.get(agentType);
            List<Class<? extends Agent>> classes = findClasses(pkg, Agent.class);

            log.info("Found {} {} agent classes, need {}", classes.size(), agentType, desired);

            int count = 0;
            for (Class<? extends Agent> cls : classes) {
                if (count >= desired) break;
                try {
                    String id = agentType + "_" + cls.getSimpleName().toLowerCase() + "_" + count;
                    Agent agent = cls.getConstructor(String.class, MessageBus.class)
                            .newInstance(id, messageBus);
                    agents.add(agent);
                    count++;
                } catch (Exception e) {
                    log.warn("Failed to instantiate {}: {}", cls.getSimpleName(), e.getMessage());
                }
            }
            log.info("Instantiated {}/{} {} agents", count, desired, agentType);
        }

        log.info("========================================");
        log.info("TOTAL AGENTS BUILT: {}", agents.size());
        log.info("========================================");
        return agents;
    }

    @SuppressWarnings("unchecked")
    private List<Class<? extends Agent>> findClasses(String packageName, Class<Agent> superClass) {
        List<Class<? extends Agent>> classes = new ArrayList<>();
        String path = packageName.replace('.', '/');
        try {
            Enumeration<URL> resources = getClass().getClassLoader().getResources(path);
            while (resources.hasMoreElements()) {
                URL resource = resources.nextElement();
                if (resource.getProtocol().equals("file")) {
                    File directory = new File(resource.toURI());
                    if (directory.exists()) {
                        for (File file : directory.listFiles()) {
                            if (file.getName().endsWith(".class")) {
                                String className = packageName + '.' + file.getName().replace(".class", "");
                                try {
                                    Class<?> clazz = Class.forName(className);
                                    if (superClass.isAssignableFrom(clazz) && !superClass.equals(clazz)) {
                                        classes.add((Class<? extends Agent>) clazz);
                                    }
                                } catch (ClassNotFoundException | NoClassDefFoundError e) {
                                    log.trace("Skipping class: {}", className);
                                }
                            }
                        }
                    }
                }
            }
        } catch (Exception e) {
            log.warn("Error scanning package {}: {}", packageName, e.getMessage());
        }
        classes.sort(Comparator.comparing(Class::getSimpleName));
        return classes;
    }

    public MessageBus getMessageBus() { return messageBus; }
}