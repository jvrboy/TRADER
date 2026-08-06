package com.deriv.swarm.config;

import java.util.Map;

public class SwarmConfig {
    private String derivAppId = "1089";
    private int dataAgentCount = 100;
    private int analysisAgentCount = 150;
    private int signalAgentCount = 100;
    private int riskAgentCount = 60;
    private int portfolioAgentCount = 40;
    private int executionAgentCount = 25;
    private int monitoringAgentCount = 15;
    private int coordinationAgentCount = 10;
    private String defaultSymbol = "R_100";
    private String defaultTimeframe = "5m";
    private int candleHistoryCount = 200;
    private long dataCollectionIntervalMs = 5000;
    private long analysisIntervalMs = 10000;
    private long signalIntervalMs = 3000;
    private long riskCheckIntervalMs = 15000;
    private boolean enableWebSocket = true;
    private boolean enableTests = true;

    public Map<String, String> toMap() {
        return Map.of(
            "deriv.app_id", derivAppId,
            "default.symbol", defaultSymbol,
            "default.timeframe", defaultTimeframe,
            "candle.history", String.valueOf(candleHistoryCount),
            "data.interval", String.valueOf(dataCollectionIntervalMs),
            "analysis.interval", String.valueOf(analysisIntervalMs),
            "websocket.enabled", String.valueOf(enableWebSocket)
        );
    }

    // Getters and Setters
    public String getDerivAppId() { return derivAppId; }
    public SwarmConfig setDerivAppId(String v) { this.derivAppId = v; return this; }
    public int getDataAgentCount() { return dataAgentCount; }
    public SwarmConfig setDataAgentCount(int v) { this.dataAgentCount = v; return this; }
    public int getAnalysisAgentCount() { return analysisAgentCount; }
    public int getSignalAgentCount() { return signalAgentCount; }
    public int getRiskAgentCount() { return riskAgentCount; }
    public int getPortfolioAgentCount() { return portfolioAgentCount; }
    public int getExecutionAgentCount() { return executionAgentCount; }
    public int getMonitoringAgentCount() { return monitoringAgentCount; }
    public int getCoordinationAgentCount() { return coordinationAgentCount; }
    public String getDefaultSymbol() { return defaultSymbol; }
    public String getDefaultTimeframe() { return defaultTimeframe; }
    public int getCandleHistoryCount() { return candleHistoryCount; }
    public boolean isEnableWebSocket() { return enableWebSocket; }
    public boolean isEnableTests() { return enableTests; }
    public int getTotalAgents() {
        return dataAgentCount + analysisAgentCount + signalAgentCount + riskAgentCount
             + portfolioAgentCount + executionAgentCount + monitoringAgentCount + coordinationAgentCount;
    }
}