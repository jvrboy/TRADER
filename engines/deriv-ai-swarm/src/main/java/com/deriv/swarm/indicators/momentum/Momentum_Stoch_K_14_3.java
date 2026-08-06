package com.deriv.swarm.indicators.momentum;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Momentum_Stoch_K_14_3 implements TechnicalIndicator {

    @Override
    public String getName() { return "Momentum Stoch_K_14_3"; }

    @Override
    public String getCategory() { return "momentum"; }

    @Override
    public String getDescription() { return "Momentum Stoch_K_14_3 - momentum indicator with period 14"; }

    @Override
    public int getMinCandles() { return 14; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> highs = candles.stream().map(Candle::getHigh).toList();
        List<Double> lows = candles.stream().map(Candle::getLow).toList();
        List<Double> closes = candles.stream().map(Candle::getClose).toList();
        int p = Math.min(14, closes.size());
        double hh = IndicatorMath.max(highs, p);
        double ll = IndicatorMath.min(lows, p);
        double value = (hh - ll) == 0 ? 50 : (closes.get(closes.size()-1) - ll) / (hh - ll) * 100;
        SignalType signal = value > 80 ? SignalType.SELL : (value < 20 ? SignalType.BUY : SignalType.NEUTRAL);
        double strength = Math.abs(value - 50) / 50.0;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Momentum Stoch_K_14_3: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
