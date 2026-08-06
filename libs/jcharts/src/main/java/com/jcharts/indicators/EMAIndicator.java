package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartColor;

/** Exponential Moving Average */
public class EMAIndicator extends AbstractIndicator {
    private final int period;

    public EMAIndicator(int period) { this(period, ChartColor.INDICATOR_2); }
    public EMAIndicator(int period, java.awt.Color color) {
        super("EMA(" + period + ")", "EMA" + period, color);
        this.period = period;
    }

    @Override public void calculate(TimeSeries data) {
        values = ema(data.getCloses(), period);
    }

    public int getPeriod() { return period; }
}
