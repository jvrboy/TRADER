package com.deriv.swarm.indicators.statistical;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Stat_Downside_Deviation_20 implements TechnicalIndicator {

    @Override
    public String getName() { return "Statistical Downside_Deviation_20"; }

    @Override
    public String getCategory() { return "statistical"; }

    @Override
    public String getDescription() { return "Statistical Downside_Deviation_20 - statistical indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double mean = IndicatorMath.sma(closes, Math.min(20, closes.size()));
        double std = IndicatorMath.stddev(closes, Math.min(20, closes.size()));
        double price = candles.get(candles.size()-1).getClose();
        double value = (price - mean) / (std == 0 ? 1 : std);
        SignalType signal = value > 1 ? SignalType.BUY : (value < -1 ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(value) / 3.0, 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Statistical Downside_Deviation_20: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
