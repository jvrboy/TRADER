package com.deriv.swarm.indicators.volatility;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Volatility_ATR_7 implements TechnicalIndicator {

    @Override
    public String getName() { return "Volatility ATR_7"; }

    @Override
    public String getCategory() { return "volatility"; }

    @Override
    public String getDescription() { return "Volatility ATR_7 - volatility indicator with period 7"; }

    @Override
    public int getMinCandles() { return 7; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.stddev(closes, Math.min(7, closes.size()));
        double price = candles.get(candles.size()-1).getClose();
        double mean = IndicatorMath.sma(closes, Math.min(7, closes.size()));
        SignalType signal = value > mean * 0.02 ? SignalType.STRONG_BUY : SignalType.NEUTRAL;
        double strength = value / mean * 100;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Volatility ATR_7: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
