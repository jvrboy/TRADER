package com.deriv.swarm.indicators.cycle;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Cycle_HT_TrendMode_20 implements TechnicalIndicator {

    @Override
    public String getName() { return "Cycle HT_TrendMode_20"; }

    @Override
    public String getCategory() { return "cycle"; }

    @Override
    public String getDescription() { return "Cycle HT_TrendMode_20 - cycle indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        int len = Math.min(20, closes.size());
        double sumSin = 0, sumCos = 0;
        for (int i = 0; i < len; i++) {
            double angle = 2.0 * Math.PI * i / len;
            sumSin += closes.get(closes.size() - len + i) * Math.sin(angle);
            sumCos += closes.get(closes.size() - len + i) * Math.cos(angle);
        }
        double value = Math.atan2(sumSin, sumCos);
        SignalType signal = value > 0 ? SignalType.BUY : SignalType.SELL;
        double strength = Math.abs(value) / Math.PI;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Cycle HT_TrendMode_20: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
