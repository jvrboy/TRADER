package com.deriv.swarm;

import org.junit.jupiter.api.*;
import org.junit.jupiter.api.io.*;
import static org.junit.jupiter.api.Assertions.*;

import java.io.File;
import java.nio.file.*;
import java.util.*;
import java.util.stream.*;

/**
 * Validates that all 500 agent and 900+ indicator source files were generated correctly.
 */
class AgentGenerationTest {

    private static final String BASE_SRC = "src/main/java/com/deriv/swarm";

    @Test
    void testDataAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/data");
        System.out.println("Data agents: " + count);
        assertTrue(count >= 100, "Expected >= 100 data agents, got " + count);
    }

    @Test
    void testAnalysisAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/analysis");
        System.out.println("Analysis agents: " + count);
        assertTrue(count >= 150, "Expected >= 150 analysis agents, got " + count);
    }

    @Test
    void testSignalAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/signal");
        System.out.println("Signal agents: " + count);
        assertTrue(count >= 100, "Expected >= 100 signal agents, got " + count);
    }

    @Test
    void testRiskAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/risk");
        System.out.println("Risk agents: " + count);
        assertTrue(count >= 60, "Expected >= 60 risk agents, got " + count);
    }

    @Test
    void testPortfolioAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/portfolio");
        System.out.println("Portfolio agents: " + count);
        assertTrue(count >= 40, "Expected >= 40 portfolio agents, got " + count);
    }

    @Test
    void testExecutionAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/execution");
        System.out.println("Execution agents: " + count);
        assertTrue(count >= 25, "Expected >= 25 execution agents, got " + count);
    }

    @Test
    void testMonitoringAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/monitoring");
        System.out.println("Monitoring agents: " + count);
        assertTrue(count >= 15, "Expected >= 15 monitoring agents, got " + count);
    }

    @Test
    void testCoordinationAgentCount() {
        long count = countJavaFiles(BASE_SRC + "/agents/coordination");
        System.out.println("Coordination agents: " + count);
        assertTrue(count >= 10, "Expected >= 10 coordination agents, got " + count);
    }

    @Test
    void testTotalAgentCount() {
        long total = countJavaFiles(BASE_SRC + "/agents");
        System.out.println("TOTAL AGENTS: " + total);
        assertTrue(total >= 500, "Expected >= 500 total agents, got " + total);
    }

    @Test
    void testTrendIndicatorCount() {
        long count = countJavaFiles(BASE_SRC + "/indicators/trend");
        System.out.println("Trend indicators: " + count);
        assertTrue(count >= 100);
    }

    @Test
    void testMomentumIndicatorCount() {
        long count = countJavaFiles(BASE_SRC + "/indicators/momentum");
        System.out.println("Momentum indicators: " + count);
        assertTrue(count >= 100);
    }

    @Test
    void testVolatilityIndicatorCount() {
        long count = countJavaFiles(BASE_SRC + "/indicators/volatility");
        System.out.println("Volatility indicators: " + count);
        assertTrue(count >= 50);
    }

    @Test
    void testVolumeIndicatorCount() {
        long count = countJavaFiles(BASE_SRC + "/indicators/volume");
        System.out.println("Volume indicators: " + count);
        assertTrue(count >= 50);
    }

    @Test
    void testPatternIndicatorCount() {
        long count = countJavaFiles(BASE_SRC + "/indicators/pattern");
        System.out.println("Pattern indicators: " + count);
        assertTrue(count >= 100);
    }

    @Test
    void testTotalIndicatorCount() {
        long total = 0;
        File indDir = new File(BASE_SRC + "/indicators");
        for (File catDir : indDir.listFiles()) {
            if (catDir.isDirectory()) {
                total += countJavaFiles(catDir.getPath());
            }
        }
        System.out.println("TOTAL INDICATORS: " + total);
        assertTrue(total >= 900, "Expected >= 900 total indicators, got " + total);
    }

    @Test
    void testAllAgentFilesContainValidPackage() throws Exception {
        File agentsDir = new File(BASE_SRC + "/agents");
        int errors = 0;
        for (File catDir : agentsDir.listFiles()) {
            if (!catDir.isDirectory()) continue;
            String expectedPkg = "com.deriv.swarm.agents." + catDir.getName();
            for (File f : catDir.listFiles((d, n) -> n.endsWith(".java"))) {
                String content = Files.readString(f.toPath());
                if (!content.contains("package " + expectedPkg)) {
                    System.err.println("Wrong package in " + f.getName());
                    errors++;
                }
            }
        }
        assertEquals(0, errors);
    }

    @Test
    void testAllIndicatorFilesImplementInterface() throws Exception {
        File indDir = new File(BASE_SRC + "/indicators");
        int errors = 0;
        for (File catDir : indDir.listFiles()) {
            if (!catDir.isDirectory()) continue;
            for (File f : catDir.listFiles((d, n) -> n.endsWith(".java"))) {
                String content = Files.readString(f.toPath());
                if (!content.contains("implements TechnicalIndicator")) {
                    System.err.println("Missing interface in " + f.getName());
                    errors++;
                }
            }
        }
        assertEquals(0, errors);
    }

    @Test
    void testCoreFilesExist() {
        String[] required = {
            "core/Agent.java", "core/AgentSwarm.java", "core/AgentMessage.java",
            "core/AgentRegistry.java", "core/AgentState.java", "core/MessageBus.java",
            "api/DerivClient.java", "api/DerivWebSocket.java",
            "model/Candle.java", "model/IndicatorResult.java", "model/SignalType.java",
            "model/TickData.java", "config/SwarmConfig.java",
            "Main.java", "SwarmBuilder.java", "IndicatorRegistry.java",
            "indicators/TechnicalIndicator.java", "indicators/IndicatorMath.java"
        };
        for (String path : required) {
            File f = new File(BASE_SRC + "/" + path);
            assertTrue(f.exists(), "Missing: " + path);
        }
    }

    @Test
    void testTestFilesExist() {
        String[] tests = {
            "src/test/java/com/deriv/swarm/CoreFrameworkTest.java",
            "src/test/java/com/deriv/swarm/IndicatorTest.java",
            "src/test/java/com/deriv/swarm/DerivAPITest.java",
            "src/test/java/com/deriv/swarm/AgentGenerationTest.java"
        };
        for (String path : tests) {
            assertTrue(new File(path).exists(), "Missing test: " + path);
        }
    }

    private long countJavaFiles(String dir) {
        File d = new File(dir);
        if (!d.exists() || !d.isDirectory()) return 0;
        return Arrays.stream(d.listFiles())
            .filter(f -> f.getName().endsWith(".java"))
            .count();
    }
}
