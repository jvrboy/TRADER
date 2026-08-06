package com.deriv.swarm.model;

import java.time.Instant;
import java.util.Map;

public class IndicatorResult {
    private final String indicatorName;
    private final String symbol;
    private final String timeframe;
    private final Instant timestamp;
    private final Map<String, Double> values;
    private final SignalType signal;
    private final double strength;
    private final String description;

    public IndicatorResult(String indicatorName, String symbol, String timeframe,
                           Instant timestamp, Map<String, Double> values,
                           SignalType signal, double strength, String description) {
        this.indicatorName = indicatorName;
        this.symbol = symbol;
        this.timeframe = timeframe;
        this.timestamp = timestamp;
        this.values = values;
        this.signal = signal;
        this.strength = strength;
        this.description = description;
    }

    public String getIndicatorName() { return indicatorName; }
    public String getSymbol() { return symbol; }
    public String getTimeframe() { return timeframe; }
    public Instant getTimestamp() { return timestamp; }
    public Map<String, Double> getValues() { return values; }
    public SignalType getSignal() { return signal; }
    public double getStrength() { return strength; }
    public String getDescription() { return description; }

    public double getValue(String key) { return values.getOrDefault(key, 0.0); }

    @Override
    public String toString() {
        return String.format("IndicatorResult{%s on %s %s: signal=%s, strength=%.2f, values=%s}",
                indicatorName, symbol, timeframe, signal, strength, values);
    }
}
