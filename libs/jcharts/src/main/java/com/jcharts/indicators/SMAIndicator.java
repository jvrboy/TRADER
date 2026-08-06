package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartColor;

/** Simple Moving Average */
public class SMAIndicator extends AbstractIndicator {
    private final int period;

    public SMAIndicator(int period) { this(period, ChartColor.INDICATOR_1); }
    public SMAIndicator(int period, java.awt.Color color) {
        super("SMA(" + period + ")", "SMA" + period, color);
        this.period = period;
    }

    @Override public void calculate(TimeSeries data) {
        values = sma(data.getCloses(), period);
    }

    public int getPeriod() { return period; }
}
