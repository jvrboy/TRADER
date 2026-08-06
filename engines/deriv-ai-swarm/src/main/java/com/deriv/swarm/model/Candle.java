package com.deriv.swarm.model;

import java.math.BigDecimal;
import java.time.Instant;

public class Candle {
    private final Instant epoch;
    private final double open;
    private final double high;
    private final double low;
    private final double close;
    private final long tickVolume;

    public Candle(Instant epoch, double open, double high, double low, double close, long tickVolume) {
        this.epoch = epoch;
        this.open = open;
        this.high = high;
        this.low = low;
        this.close = close;
        this.tickVolume = tickVolume;
    }

    public Instant getEpoch() { return epoch; }
    public double getOpen() { return open; }
    public double getHigh() { return high; }
    public double getLow() { return low; }
    public double getClose() { return close; }
    public long getTickVolume() { return tickVolume; }

    public double getBody() { return close - open; }
    public double getUpperWick() { return high - Math.max(open, close); }
    public double getLowerWick() { return Math.min(open, close) - low; }
    public double getRange() { return high - low; }
    public double getMidpoint() { return (high + low) / 2.0; }
    public boolean isBullish() { return close > open; }
    public boolean isBearish() { return close < open; }
    public boolean isDoji() { return Math.abs(close - open) < getRange() * 0.1; }

    @Override
    public String toString() {
        return String.format("Candle{epoch=%s, O=%.5f, H=%.5f, L=%.5f, C=%.5f, V=%d}",
                epoch, open, high, low, close, tickVolume);
    }
}
