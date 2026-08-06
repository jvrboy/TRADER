package com.jcharts.features;

import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;
import java.util.ArrayList;
import java.util.List;

/** Compares two or more symbols by normalizing their prices to percentage change from a base point. */
public class SymbolCompare {
    private final List<TimeSeries> series = new ArrayList<>();
    private final List<java.awt.Color> colors = new ArrayList<>();
    private final List<String> names = new ArrayList<>();

    public void addSymbol(TimeSeries ts, String name, java.awt.Color color) {
        series.add(ts); names.add(name); colors.add(color);
    }

    public List<NormalizedSeries> normalize() {
        List<NormalizedSeries> result = new ArrayList<>();
        int maxLen = series.stream().mapToInt(TimeSeries::size).max().orElse(0);
        for (int s = 0; s < series.size(); s++) {
            double[] closes = series.get(s).getCloses();
            double base = closes.length > 0 ? closes[0] : 1;
            double[] pct = new double[closes.length];
            for (int i = 0; i < closes.length; i++) pct[i] = ((closes[i] - base) / base) * 100.0;
            result.add(new NormalizedSeries(names.get(s), pct, colors.get(s)));
        }
        return result;
    }

    public static class NormalizedSeries {
        public final String name;
        public final double[] percentChanges;
        public final java.awt.Color color;
        NormalizedSeries(String name, double[] pct, java.awt.Color color) { this.name = name; this.percentChanges = pct; this.color = color; }
    }
}
