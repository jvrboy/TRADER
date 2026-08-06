package com.deriv.swarm.indicators.fibonacci;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Fibonacci_Extension_1414 implements TechnicalIndicator {

    @Override
    public String getName() { return "Fibonacci Extension_1414"; }

    @Override
    public String getCategory() { return "fibonacci"; }

    @Override
    public String getDescription() { return "Fibonacci Extension_1414 - fibonacci indicator with period 1"; }

    @Override
    public int getMinCandles() { return 1; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                double high = IndicatorMath.max(candles.stream().map(Candle::getHigh).toList(), Math.min(1, candles.size()));
        double low = IndicatorMath.min(candles.stream().map(Candle::getLow).toList(), Math.min(1, candles.size()));
        double diff = high - low;
        double fibLevel = 0.618;
        double value = low + diff * fibLevel;
        double price = candles.get(candles.size()-1).getClose();
        SignalType signal = price > value ? SignalType.BUY : (price < value ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.abs(price - value) / diff * 100;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Fibonacci Extension_1414: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
