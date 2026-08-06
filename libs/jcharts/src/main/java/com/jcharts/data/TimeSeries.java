package com.jcharts.data;

import java.util.*;
import java.util.stream.Collectors;

/**
 * An ordered collection of OHLCBar objects representing a time series of price data.
 * Provides statistical utilities, slicing, and data transformation methods.
 */
public class TimeSeries {

    private final List<OHLCBar> bars;
    private final String symbol;
    private final String timeframe;

    public TimeSeries() {
        this("DEFAULT", "1D");
    }

    public TimeSeries(String symbol, String timeframe) {
        this.bars = new ArrayList<>();
        this.symbol = symbol;
        this.timeframe = timeframe;
    }

    public TimeSeries(List<OHLCBar> bars) {
        this(bars, "DEFAULT", "1D");
    }

    public TimeSeries(List<OHLCBar> bars, String symbol, String timeframe) {
        this.bars = new ArrayList<>(bars);
        this.bars.sort(Comparator.naturalOrder());
        this.symbol = symbol;
        this.timeframe = timeframe;
    }

    public void addBar(OHLCBar bar) {
        bars.add(bar);
        bars.sort(Comparator.naturalOrder());
    }

    public OHLCBar getBar(int index) {
        if (index < 0 || index >= bars.size()) throw new IndexOutOfBoundsException("Index: " + index + ", Size: " + bars.size());
        return bars.get(index);
    }

    public List<OHLCBar> getBars() { return Collections.unmodifiableList(bars); }
    public int size() { return bars.size(); }
    public boolean isEmpty() { return bars.isEmpty(); }
    public String getSymbol() { return symbol; }
    public String getTimeframe() { return timeframe; }

    public double[] getCloses() {
        return bars.stream().mapToDouble(OHLCBar::getClose).toArray();
    }

    public double[] getOpens() {
        return bars.stream().mapToDouble(OHLCBar::getOpen).toArray();
    }

    public double[] getHighs() {
        return bars.stream().mapToDouble(OHLCBar::getHigh).toArray();
    }

    public double[] getLows() {
        return bars.stream().mapToDouble(OHLCBar::getLow).toArray();
    }

    public double[] getVolumes() {
        return bars.stream().mapToDouble(OHLCBar::getVolume).toArray();
    }

    public double[] getTypicalPrices() {
        return bars.stream().mapToDouble(b -> (b.getHigh() + b.getLow() + b.getClose()) / 3.0).toArray();
    }

    public double[] getHL2() {
        return bars.stream().mapToDouble(b -> (b.getHigh() + b.getLow()) / 2.0).toArray();
    }

    public double[] getHLC3() {
        return bars.stream().mapToDouble(b -> (b.getHigh() + b.getLow() + b.getClose()) / 3.0).toArray();
    }

    public double[] getOHLC4() {
        return bars.stream().mapToDouble(b -> (b.getOpen() + b.getHigh() + b.getLow() + b.getClose()) / 4.0).toArray();
    }

    public double getGlobalMax() {
        return bars.stream().mapToDouble(OHLCBar::getHigh).max().orElse(Double.NaN);
    }

    public double getGlobalMin() {
        return bars.stream().mapToDouble(OHLCBar::getLow).min().orElse(Double.NaN);
    }

    public double getAvgVolume() {
        return bars.stream().mapToDouble(OHLCBar::getVolume).average().orElse(0);
    }

    public TimeSeries subSeries(int start, int end) {
        return new TimeSeries(bars.subList(start, Math.min(end, bars.size())), symbol, timeframe);
    }

    public TimeSeries last(int n) {
        if (n >= bars.size()) return new TimeSeries(bars, symbol, timeframe);
        return subSeries(bars.size() - n, bars.size());
    }

    public static TimeSeries generateRandom(int count, double startPrice, double volatility, String symbol) {
        List<OHLCBar> bars = new ArrayList<>();
        Random random = new Random(42);
        double price = startPrice;
        long baseTime = System.currentTimeMillis() - (long) count * 86400000L;

        for (int i = 0; i < count; i++) {
            double change = (random.nextDouble() - 0.48) * volatility;
            double open = price;
            double close = price + change;
            double high = Math.max(open, close) + random.nextDouble() * volatility * 0.5;
            double low = Math.min(open, close) - random.nextDouble() * volatility * 0.5;
            double volume = 1000000 + random.nextDouble() * 5000000;
            if (low < 0.01) low = 0.01;
            bars.add(new OHLCBar(baseTime + i * 86400000L, open, Math.max(open, high), low, Math.max(close, 0.01), volume));
            price = Math.max(close, 0.01);
        }
        return new TimeSeries(bars, symbol, "1D");
    }

    public static TimeSeries merge(TimeSeries a, TimeSeries b) {
        List<OHLCBar> merged = new ArrayList<>(a.getBars());
        merged.addAll(b.getBars());
        return new TimeSeries(merged, a.getSymbol(), a.getTimeframe());
    }

    @Override
    public String toString() {
        return String.format("TimeSeries{symbol=%s, timeframe=%s, bars=%d}", symbol, timeframe, bars.size());
    }
}
