package com.deriv.swarm.indicators.pivot;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Pivot_Standard_R2 implements TechnicalIndicator {

    @Override
    public String getName() { return "Pivot Standard_R2"; }

    @Override
    public String getCategory() { return "pivot"; }

    @Override
    public String getDescription() { return "Pivot Standard_R2 - pivot indicator with period 1"; }

    @Override
    public int getMinCandles() { return 1; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                Candle last = candles.get(candles.size()-1);
        double pp = (last.getHigh() + last.getLow() + last.getClose()) / 3.0;
        double value = pp;
        double price = last.getClose();
        SignalType signal = price > pp ? SignalType.BUY : (price < pp ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.abs(price - pp) / pp * 100;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Pivot Standard_R2: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
