package com.deriv.swarm.indicators.custom;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Custom_Trend_Quality_Index implements TechnicalIndicator {

    @Override
    public String getName() { return "Custom Trend_Quality_Index"; }

    @Override
    public String getCategory() { return "custom"; }

    @Override
    public String getDescription() { return "Custom Trend_Quality_Index - custom indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double sma = IndicatorMath.sma(closes, Math.min(20, closes.size()));
        double rsi = IndicatorMath.rsi(closes, Math.min(20, closes.size()));
        double std = IndicatorMath.stddev(closes, Math.min(20, closes.size()));
        double value = (rsi / 100.0) * (1 + (closes.get(closes.size()-1) - sma) / (std == 0 ? 1 : std));
        SignalType signal = value > 0.5 ? SignalType.BUY : (value < -0.5 ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(value), 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Custom Trend_Quality_Index: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
