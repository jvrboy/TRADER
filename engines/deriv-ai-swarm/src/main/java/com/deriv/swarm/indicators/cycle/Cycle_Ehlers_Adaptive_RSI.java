package com.deriv.swarm.indicators.cycle;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Cycle_Ehlers_Adaptive_RSI implements TechnicalIndicator {

    @Override
    public String getName() { return "Cycle Ehlers_Adaptive_RSI"; }

    @Override
    public String getCategory() { return "cycle"; }

    @Override
    public String getDescription() { return "Cycle Ehlers_Adaptive_RSI - cycle indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.rsi(closes, 20);
        SignalType signal = value > 70 ? SignalType.SELL : (value < 30 ? SignalType.BUY : SignalType.NEUTRAL);
        double strength = Math.abs(value - 50) / 50.0;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Cycle Ehlers_Adaptive_RSI: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
