package com.deriv.swarm.indicators.order_flow;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class OrderFlow_CVD_Trend implements TechnicalIndicator {

    @Override
    public String getName() { return "Order Flow CVD_Trend"; }

    @Override
    public String getCategory() { return "order_flow"; }

    @Override
    public String getDescription() { return "Order Flow CVD_Trend - order_flow indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        List<Long> volumes = candles.stream().map(Candle::getTickVolume).toList();
        int len = Math.min(20, closes.size());
        double delta = 0;
        for (int i = 0; i < len; i++) {
            int idx = closes.size() - len + i;
            double change = closes.get(idx) - (idx > 0 ? closes.get(idx-1) : closes.get(idx));
            delta += change * volumes.get(idx);
        }
        double value = delta;
        SignalType signal = delta > 0 ? SignalType.BUY : (delta < 0 ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.min(Math.abs(delta) / 1000.0, 1.0);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Order Flow CVD_Trend: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
