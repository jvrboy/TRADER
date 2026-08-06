package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Bollinger Bands (SMA middle, upper/lower at +/- k * stddev). */
public class BollingerBandsIndicator extends AbstractIndicator {
    private final int period;
    private final double k;
    private double[] upper, lower;

    public BollingerBandsIndicator() { this(20, 2.0); }
    public BollingerBandsIndicator(int period, double k) {
        super("BB(" + period + "," + k + ")", "BB" + period, ChartColor.INDICATOR_1);
        this.period = period; this.k = k;
    }

    @Override public void calculate(TimeSeries data) {
        double[] closes = data.getCloses();
        values = sma(closes, period);
        upper = new double[closes.length];
        lower = new double[closes.length];
        for (int i = period - 1; i < closes.length; i++) {
            double sumSq = 0;
            for (int j = i - period + 1; j <= i; j++) sumSq += Math.pow(closes[j] - values[i], 2);
            double std = Math.sqrt(sumSq / period);
            upper[i] = values[i] + k * std;
            lower[i] = values[i] - k * std;
        }
        for (int i = 0; i < period - 1; i++) { upper[i] = Double.NaN; lower[i] = Double.NaN; }
    }

    @Override public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        // Fill between bands
        int start = model.getVisibleStart(), end = model.getVisibleEnd();
        for (int i = start; i < end - 1; i++) {
            if (i >= upper.length || Double.isNaN(upper[i]) || i + 1 >= upper.length || Double.isNaN(upper[i + 1])) continue;
            int x1 = (int) model.barX(i, chartW), x2 = (int) model.barX(i + 1, chartW);
            int yu1 = (int) model.priceToY(upper[i], chartH), yu2 = (int) model.priceToY(upper[i + 1], chartH);
            int yl1 = (int) model.priceToY(lower[i], chartH), yl2 = (int) model.priceToY(lower[i + 1], chartH);
            g.setColor(ChartColor.withAlpha(color, 20));
            g.fillPolygon(new int[]{x1, x2, x2, x1}, new int[]{yu1, yu2, yl2, yl1}, 4);
        }
        drawLine(g, model, chartW, chartH, upper, 0);
        drawLine(g, model, chartW, chartH, lower, 0);
        drawLine(g, model, chartW, chartH, values, 0);
        drawLabel(g, model.getLeftMargin() + 5, model.getTopMargin() + 15);
    }

    public double[] getUpper() { return upper; }
    public double[] getLower() { return lower; }
}