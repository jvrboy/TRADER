package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartColor;

/** Average True Range - volatility measure. */
public class ATRIndicator extends AbstractIndicator {
    private final int period;

    public ATRIndicator() { this(14); }
    public ATRIndicator(int period) {
        super("ATR(" + period + ")", "ATR" + period, ChartColor.ORANGE);
        this.period = period;
    }

    @Override public void calculate(TimeSeries data) {
        int n = data.size();
        double[] tr = new double[n];
        for (int i = 0; i < n; i++) {
            if (i == 0) { tr[i] = data.getBar(i).getHigh() - data.getBar(i).getLow(); continue; }
            double hl = data.getBar(i).getHigh() - data.getBar(i).getLow();
            double hc = Math.abs(data.getBar(i).getHigh() - data.getBar(i - 1).getClose());
            double lc = Math.abs(data.getBar(i).getLow() - data.getBar(i - 1).getClose());
            tr[i] = Math.max(hl, Math.max(hc, lc));
        }
        values = ema(tr, period);
    }

    public int getPeriod() { return period; }
}