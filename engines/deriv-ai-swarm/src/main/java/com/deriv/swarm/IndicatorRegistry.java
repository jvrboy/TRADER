package com.deriv.swarm;

import com.deriv.swarm.indicators.TechnicalIndicator;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.*;
import java.util.stream.Collectors;

/**
 * Discovers and registers all 900+ technical indicator implementations.
 */
public class IndicatorRegistry {
    private static final Logger log = LoggerFactory.getLogger(IndicatorRegistry.class);
    private final Map<String, TechnicalIndicator> indicators = new LinkedHashMap<>();
    private final Map<String, List<String>> byCategory = new HashMap<>();

    @SuppressWarnings("unchecked")
    public void discover() {
        String[] packages = {
            "com.deriv.swarm.indicators.trend",
            "com.deriv.swarm.indicators.momentum",
            "com.deriv.swarm.indicators.volatility",
            "com.deriv.swarm.indicators.volume",
            "com.deriv.swarm.indicators.pattern",
            "com.deriv.swarm.indicators.support_resistance",
            "com.deriv.swarm.indicators.fibonacci",
            "com.deriv.swarm.indicators.pivot",
            "com.deriv.swarm.indicators.statistical",
            "com.deriv.swarm.indicators.cycle",
            "com.deriv.swarm.indicators.market_profile",
            "com.deriv.swarm.indicators.order_flow",
            "com.deriv.swarm.indicators.custom"
        };

        for (String pkg : packages) {
            String path = pkg.replace('.', '/');
            try {
                Enumeration<java.net.URL> resources = getClass().getClassLoader().getResources(path);
                while (resources.hasMoreElements()) {
                    java.net.URL resource = resources.nextElement();
                    if (resource.getProtocol().equals("file")) {
                        File directory = new File(resource.toURI());
                        if (directory.exists()) {
                            for (File file : directory.listFiles()) {
                                if (file.getName().endsWith(".class")) {
                                    String className = pkg + '.' + file.getName().replace(".class", "");
                                    try {
                                        Class<?> clazz = Class.forName(className);
                                        if (TechnicalIndicator.class.isAssignableFrom(clazz)) {
                                            TechnicalIndicator ind = (TechnicalIndicator) clazz.getDeclaredConstructor().newInstance();
                                            indicators.put(ind.getName(), ind);
                                            byCategory.computeIfAbsent(ind.getCategory(), k -> new ArrayList<>()).add(ind.getName());
                                        }
                                    } catch (Exception e) {
                                        log.trace("Skipping indicator: {}", className);
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
                log.warn("Error scanning package {}: {}", pkg, e.getMessage());
            }
        }

        log.info("========================================");
        log.info("DISCOVERED {} TECHNICAL INDICATORS", indicators.size());
        log.info("========================================");
        byCategory.entrySet().stream()
            .sorted(Map.Entry.comparingByKey())
            .forEach(e -> log.info("  {}: {} indicators", e.getKey(), e.getValue().size()));
    }

    public TechnicalIndicator get(String name) { return indicators.get(name); }
    public Collection<TechnicalIndicator> getAll() { return indicators.values(); }
    public List<TechnicalIndicator> getByCategory(String category) {
        return byCategory.getOrDefault(category, List.of()).stream()
            .map(indicators::get)
            .filter(Objects::nonNull)
            .collect(Collectors.toList());
    }
    public int size() { return indicators.size(); }
    public Set<String> getCategories() { return byCategory.keySet(); }
    public Map<String, List<String>> getByCategoryMap() { return byCategory; }
}
