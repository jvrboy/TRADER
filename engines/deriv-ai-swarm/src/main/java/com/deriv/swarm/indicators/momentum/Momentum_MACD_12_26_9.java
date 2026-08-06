package com.deriv.swarm.indicators.momentum;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Momentum_MACD_12_26_9 implements TechnicalIndicator {

    @Override
    public String getName() { return "Momentum MACD_12_26_9"; }

    @Override
    public String getCategory() { return "momentum"; }

    @Override
    public String getDescription() { return "Momentum MACD_12_26_9 - momentum indicator with period 26"; }

    @Override
    public int getMinCandles() { return 26; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double emaFast = IndicatorMath.ema(closes, Math.min(12, closes.size()));
        double emaSlow = IndicatorMath.ema(closes, Math.min(26, closes.size()));
        double value = emaFast - emaSlow;
        SignalType signal = value > 0 ? SignalType.BUY : (value < 0 ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(value) / closes.get(closes.size()-1) * 100, 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Momentum MACD_12_26_9: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
