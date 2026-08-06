package com.deriv.swarm.indicators.volume;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Volume_Volume_Weighted_RSI_14 implements TechnicalIndicator {

    @Override
    public String getName() { return "Volume Volume_Weighted_RSI_14"; }

    @Override
    public String getCategory() { return "volume"; }

    @Override
    public String getDescription() { return "Volume Volume_Weighted_RSI_14 - volume indicator with period 14"; }

    @Override
    public int getMinCandles() { return 14; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.rsi(closes, 14);
        SignalType signal = value > 70 ? SignalType.SELL : (value < 30 ? SignalType.BUY : SignalType.NEUTRAL);
        double strength = Math.abs(value - 50) / 50.0;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Volume Volume_Weighted_RSI_14: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
