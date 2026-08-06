package com.deriv.swarm.indicators.support_resistance;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class SR_Mid_S1_S2 implements TechnicalIndicator {

    @Override
    public String getName() { return "S/R Mid_S1_S2"; }

    @Override
    public String getCategory() { return "support_resistance"; }

    @Override
    public String getDescription() { return "S/R Mid_S1_S2 - support_resistance indicator with period 1"; }

    @Override
    public int getMinCandles() { return 1; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.sma(closes, Math.min(1, closes.size()));
        double price = candles.get(candles.size()-1).getClose();
        SignalType signal = price > value ? SignalType.BUY : (price < value ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.abs(price - value) / value * 100;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "S/R Mid_S1_S2: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
