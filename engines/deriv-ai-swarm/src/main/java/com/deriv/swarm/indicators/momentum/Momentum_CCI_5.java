package com.deriv.swarm.indicators.momentum;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Momentum_CCI_5 implements TechnicalIndicator {

    @Override
    public String getName() { return "Momentum CCI_5"; }

    @Override
    public String getCategory() { return "momentum"; }

    @Override
    public String getDescription() { return "Momentum CCI_5 - momentum indicator with period 5"; }

    @Override
    public int getMinCandles() { return 5; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> highs = candles.stream().map(Candle::getHigh).toList();
        List<Double> lows = candles.stream().map(Candle::getLow).toList();
        List<Double> closes = candles.stream().map(Candle::getClose).toList();
        int p = Math.min(5, closes.size());
        double tp = (highs.get(highs.size()-1) + lows.get(lows.size()-1) + closes.get(closes.size()-1)) / 3.0;
        double value = (tp - IndicatorMath.sma(closes, p)) / (0.015 * IndicatorMath.stddev(closes, p));
        SignalType signal = value > 100 ? SignalType.SELL : (value < -100 ? SignalType.BUY : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(value) / 200.0, 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Momentum CCI_5: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
