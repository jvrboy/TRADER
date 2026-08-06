package com.deriv.swarm.indicators.market_profile;

import com.deriv.swarm.indicators.IndicatorMath;
import com.deriv.swarm.indicators.TechnicalIndicator;
import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.IndicatorResult;
import com.deriv.swarm.model.SignalType;

import java.time.Instant;
import java.util.*;

public class MarketProfile_Initial_Balance_Low implements TechnicalIndicator {

    @Override
    public String getName() { return "Market Profile Initial_Balance_Low"; }

    @Override
    public String getCategory() { return "market_profile"; }

    @Override
    public String getDescription() { return "Market Profile Initial_Balance_Low - market_profile indicator with period 1"; }

    @Override
    public int getMinCandles() { return 1; }

    @Override
    public IndicatorResult calculate(List<Candle> candles, String symbol, String timeframe) {
        if (candles == null || candles.size() < getMinCandles()) return null;
                List<Double> closes = candles.stream().map(Candle::getClose).toList();
        double value = IndicatorMath.sma(closes, Math.min(1, closes.size()));
        double high = IndicatorMath.max(closes, Math.min(1, closes.size()));
        double low = IndicatorMath.min(closes, Math.min(1, closes.size()));
        double price = closes.get(closes.size()-1);
        SignalType signal = price > value ? SignalType.BUY : (price < value ? SignalType.SELL : SignalType.NEUTRAL);
        double strength = (high - low) == 0 ? 0 : Math.abs(price - value) / (high - low);
        Map<String, Double> values = new HashMap<>();
        values.put("value", value);
        values.put("strength", strength);
        values.put("signal_score", signal.getScore());
        return new IndicatorResult(getName(), symbol, timeframe,
            candles.get(candles.size()-1).getEpoch(), values, signal, strength,
            "Market Profile Initial_Balance_Low: value=" + String.format("%.5f", value) + " signal=" + signal);
    }
}
