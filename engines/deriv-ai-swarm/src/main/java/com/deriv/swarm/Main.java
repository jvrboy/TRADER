package com.deriv.swarm;

import com.deriv.swarm.api.DerivClient;
import com.deriv.swarm.config.SwarmConfig;
import com.deriv.swarm.core.AgentSwarm;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.TickData;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Instant;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Deriv AI Agent Swarm - Main Entry Point
 * 500 Agents | 1190 Technical Analysis Indicators | Deriv API
 */
public class Main {
    private static final Logger log = LoggerFactory.getLogger(Main.class);

    public static void main(String[] args) {
        log.info("");
        log.info("  ██████╗ ███████╗ ██████╗     ██████╗  ██████╗  ██████╗  ██████╗");
        log.info("  ██╔══██╗██╔════╝██╔═══██╗    ╚════██╗██╔═████╗██╔═████╗╚════██╗");
        log.info("  ██████╔╝█████╗  ██║   ██║     █████╔╝██║██╔██║██║██╔██║ █████╔╝");
        log.info("  ██╔══██╗██╔══╝  ██║   ██║    ██╔═══╝ ████╔╝██║████╔╝██║██╔═══╝ ");
        log.info("  ██║  ██║███████╗╚██████╔╝    ███████╗╚██████╔╝╚██████╔╝███████╗");
        log.info("  ╚═╝  ╚═╝╚══════╝ ╚═════╝     ╚══════╝ ╚═════╝  ╚═════╝ ╚══════╝");
        log.info("");
        log.info("  Deriv AI Agent Swarm v1.0.0");
        log.info("  500 Agents | 1190 Technical Indicators | Deriv Public API");
        log.info("");

        // 1. Discover indicators
        log.info("[Phase 1] Discovering technical indicators...");
        IndicatorRegistry indicatorRegistry = new IndicatorRegistry();
        indicatorRegistry.discover();
        log.info("  -> {} indicators discovered\n", indicatorRegistry.size());

        // 2. Build the swarm
        log.info("[Phase 2] Building agent swarm...");
        SwarmConfig config = new SwarmConfig();
        SwarmBuilder builder = new SwarmBuilder(config);
        List<com.deriv.swarm.core.Agent> agents = builder.buildAll();
        log.info("  -> {} agents built\n", agents.size());

        // 3. Initialize swarm
        log.info("[Phase 3] Initializing swarm...");
        AgentSwarm swarm = new AgentSwarm();
        swarm.initialize(agents, config.toMap());
        log.info("  -> Swarm initialized\n");

        // 4. Start swarm
        log.info("[Phase 4] Starting swarm...");
        swarm.start();
        log.info("  -> Swarm running\n");

        // 5. Test Deriv API
        log.info("[Phase 5] Testing Deriv API connection...");
        testDerivAPI();

        // 6. Test indicators
        log.info("[Phase 6] Testing indicator calculations...");
        testIndicators(indicatorRegistry);

        // 7. Print swarm stats
        log.info("[Phase 7] Swarm Statistics:");
        log.info("{}", swarm.getStats());

        // 8. Keep running
        log.info("");
        log.info("Swarm is running. Press Ctrl+C to stop.");
        try {
            swarm.awaitTermination(Long.MAX_VALUE, TimeUnit.DAYS);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }

        // Cleanup
        swarm.stop();
        log.info("Swarm shut down gracefully.");
    }

    private static void testDerivAPI() {
        try {
            DerivClient client = new DerivClient();
            log.info("  Testing: getActiveSymbols...");
            var symbols = client.getActiveSymbols();
            if (symbols.has("active_symbols")) {
                int count = symbols.getAsJsonArray("active_symbols").size();
                log.info("  -> SUCCESS: Retrieved {} active symbols", count);
            } else {
                log.warn("  -> No active_symbols in response");
            }

            log.info("  Testing: getCandles (R_100, 5m, 50)...");
            List<Candle> candles = client.getCandles("R_100", "5m", 50);
            log.info("  -> SUCCESS: Retrieved {} candles", candles.size());
            if (!candles.isEmpty()) {
                Candle last = candles.get(candles.size() - 1);
                log.info("     Last candle: O={}, H={}, L={}, C={}, V={}",
                        last.getOpen(), last.getHigh(), last.getLow(), last.getClose(), last.getTickVolume());
            }
            client.disconnect();
        } catch (Exception e) {
            log.error("  -> Deriv API test error: {}", e.getMessage());
        }
    }

    private static void testIndicators(IndicatorRegistry registry) {
        // Generate synthetic test data
        Random random = new Random(42);
        double price = 1.10000;
        List<Candle> candles = new ArrayList<>();
        for (int i = 0; i < 300; i++) {
            double open = price;
            double change = (random.nextDouble() - 0.48) * 0.003;
            double close = open + change;
            double high = Math.max(open, close) + random.nextDouble() * 0.001;
            double low = Math.min(open, close) - random.nextDouble() * 0.001;
            candles.add(new Candle(Instant.now().minusSeconds((300 - i) * 300),
                    open, high, low, close, (long) (random.nextInt(10000) + 1000)));
            price = close;
        }

        int tested = 0, passed = 0, failed = 0;
        Map<String, Integer> categoryResults = new LinkedHashMap<>();

        for (TechnicalIndicator indicator : registry.getAll()) {
            try {
                IndicatorResult result = indicator.calculate(candles, "R_100", "5m");
                if (result != null) {
                    passed++;
                    categoryResults.merge(indicator.getCategory(), 1, Integer::sum);
                }
            } catch (Exception e) {
                failed++;
            }
            tested++;
        }

        log.info("  -> Indicator Test Results: {}/{} passed, {} failed", passed, tested, failed);
        log.info("  -> By Category:");
        categoryResults.forEach((cat, count) ->
            log.info("     {}: {} indicators tested OK", cat, count));
    }
}