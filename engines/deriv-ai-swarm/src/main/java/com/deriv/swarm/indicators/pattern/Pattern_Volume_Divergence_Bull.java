package com.deriv.swarm.indicators.pattern;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Pattern_Volume_Divergence_Bull implements TechnicalIndicator {

    @Override
    public String getName() { return "Pattern Volume_Divergence_Bull"; }

    @Override
    public String getCategory() { return "pattern"; }

    @Override
    public String getDescription() { return "Pattern Volume_Divergence_Bull - pattern indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                if (candles.size() < 20) return null;
        int n = candles.size();
        double lastClose = candles.get(n-1).getClose();
        double prevClose = candles.get(n-2).getClose();
        double change = (lastClose - prevClose) / prevClose * 100;
        double value = change;
        SignalType signal = change > 0.5 ? SignalType.BUY : (change < -0.5 ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(change) / 2.0, 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Pattern Volume_Divergence_Bull: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
