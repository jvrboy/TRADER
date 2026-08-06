package com.deriv.swarm.indicators;

import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.List;
import java.util.Map;

public interface TechnicalIndicator {
    String getName();
    String getCategory();
    String getDescription();
    IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe);
    default boolean requiresMinCandles(int count) { return true; }
    default int getMinCandles() { return 14; }
    default SignalType generateSignal(double value) {
        return SignalType.NEUTRAL;
    }
    default Map<String, Double> defaultParams() { return Map.of(); }
}