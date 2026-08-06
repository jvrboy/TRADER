package com.jcharts.data;

import java.io.Serializable;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;

/**
 * Represents a single OHLC (Open-High-Low-Close) data bar with volume and timestamp.
 * Immutable data class used as the foundational unit for all chart types.
 */
public final class OHLCBar implements Serializable, Comparable<OHLCBar> {

    private static final long serialVersionUID = 1L;
    private static final DateTimeFormatter FORMATTER = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm");

    private final long timestamp;
    private final double open;
    private final double high;
    private final double low;
    private final double close;
    private final double volume;

    public OHLCBar(long timestamp, double open, double high, double low, double close, double volume) {
        if (high < low) {
            throw new IllegalArgumentException("High (" + high + ") cannot be less than Low (" + low + ")");
        }
        if (open < 0 || high < 0 || low < 0 || close < 0 || volume < 0) {
            throw new IllegalArgumentException("Values cannot be negative: O=" + open + " H=" + high + " L=" + low + " C=" + close + " V=" + volume);
        }
        this.timestamp = timestamp;
        this.open = open;
        this.high = high;
        this.low = low;
        this.close = close;
        this.volume = volume;
    }

    public long getTimestamp() { return timestamp; }
    public double getOpen() { return open; }
    public double getHigh() { return high; }
    public double getLow() { return low; }
    public double getClose() { return close; }
    public double getVolume() { return volume; }

    public double getBodySize() { return Math.abs(close - open); }
    public double getUpperWick() { return high - Math.max(open, close); }
    public double getLowerWick() { return Math.min(open, close) - low; }
    public double getRange() { return high - low; }
    public double getMidpoint() { return (high + low) / 2.0; }
    public boolean isBullish() { return close >= open; }
    public boolean isBearish() { return close < open; }
    public boolean isDoji() { return Math.abs(close - open) < (high - low) * 0.05 && (high - low) > 0; }

    public LocalDateTime getDateTime() {
        return LocalDateTime.ofInstant(Instant.ofEpochMilli(timestamp), ZoneId.of("UTC"));
    }

    public String getTimeString() {
        return getDateTime().format(FORMATTER);
    }

    public static OHLCBar of(LocalDateTime dt, double o, double h, double l, double c, double v) {
        return new OHLCBar(dt.toEpochSecond(ZoneOffset.UTC) * 1000, o, h, l, c, v);
    }

    @Override
    public int compareTo(OHLCBar other) {
        return Long.compare(this.timestamp, other.timestamp);
    }

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (!(o instanceof OHLCBar)) return false;
        OHLCBar ohlcBar = (OHLCBar) o;
        return timestamp == ohlcBar.timestamp;
    }

    @Override
    public int hashCode() {
        return Long.hashCode(timestamp);
    }

    @Override
    public String toString() {
        return String.format("OHLCBar{time=%s, O=%.2f, H=%.2f, L=%.2f, C=%.2f, V=%.0f}",
                getTimeString(), open, high, low, close, volume);
    }
}
