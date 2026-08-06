package com.deriv.swarm.indicators.volatility;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class Volatility_Acceleration_Band_Upper implements TechnicalIndicator {

    @Override
    public String getName() { return "Volatility Acceleration_Band_Upper"; }

    @Override
    public String getCategory() { return "volatility"; }

    @Override
    public String getDescription() { return "Volatility Acceleration_Band_Upper - volatility indicator with period 20"; }

    @Override
    public int getMinCandles() { return 20; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.sma(closes, Math.min(20, closes.size()));
        double price = candles.get(candles.size()-1).getClose();
        SignalType signal = price > value ? SignalType.BUY : (price < value ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = Math.abs(price - value) / value * 100;
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Volatility Acceleration_Band_Upper: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
