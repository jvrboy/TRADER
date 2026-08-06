package com.deriv.swarm.indicators.volume;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Volume_Volume_Osc_10_20 implements TechnicalIndicator {

    @Override
    public String getName() { return "Volume Volume_Osc_10_20"; }

    @Override
    public String getCategory() { return "volume"; }

    @Override
    public String getDescription() { return "Volume Volume_Osc_10_20 - volume indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        List<Long> volumes = candles.stream().map(Candle::getTickVolume).toList();
        double obv = 0;
        for (int i = 1; i < closes.size(); i++) {
            if (closes.get(i) > closes.get(i-1)) obv += volumes.get(i);
            else if (closes.get(i) < closes.get(i-1)) obv -= volumes.get(i);
        }
        double value = obv;
        double prevObv = value - (closes.get(closes.size()-1) > closes.get(closes.size()-2) ? volumes.get(volumes.size()-1) : -volumes.get(volumes.size()-1));
        SignalType signal = value > prevObv ? SignalType.BUY : (value < prevObv ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(value - prevObv) / (Math.abs(value) + 1), 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Volume Volume_Osc_10_20: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
