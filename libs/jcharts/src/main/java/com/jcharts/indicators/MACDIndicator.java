package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** MACD indicator with signal line and histogram. */
public class MACDIndicator extends AbstractIndicator {
    private final int fastPeriod, slowPeriod, signalPeriod;
    private double[] signal;
    private double[] histogram;

    public MACDIndicator() { this(12, 26, 9); }
    public MACDIndicator(int fast, int slow, int sig) {
        super("MACD(" + fast + "," + slow + "," + sig + ")", "MACD", ChartColor.INDICATOR_1);
        this.fastPeriod = fast; this.slowPeriod = slow; this.signalPeriod = sig;
    }

    @Override public void calculate(TimeSeries data) {
        double[] fastEma = ema(data.getCloses(), fastPeriod);
        double[] slowEma = ema(data.getCloses(), slowPeriod);
        values = new double[data.size()];
        for (int i = 0; i < data.size(); i++) {
            values[i] = Double.isNaN(fastEma[i]) || Double.isNaN(slowEma[i]) ? Double.NaN : fastEma[i] - slowEma[i];
        }
        signal = ema(clean(values), signalPeriod);
        // Re-insert NaN prefix
        double[] tmp = new double[values.length];
        System.arraycopy(signal, 0, tmp, values.length - signal.length, signal.length);
        for (int i = 0; i < values.length - signal.length; i++) tmp[i] = Double.NaN;
        signal = tmp;
        histogram = new double[data.size()];
        for (int i = 0; i < data.size(); i++) {
            if (!Double.isNaN(values[i]) && !Double.isNaN(signal[i])) histogram[i] = values[i] - signal[i];
            else histogram[i] = Double.NaN;
        }
    }

    private double[] clean(double[] arr) {
        int start = 0; while (start < arr.length && Double.isNaN(arr[start])) start++;
        double[] out = new double[arr.length - start];
        System.arraycopy(arr, start, out, 0, out.length);
        return out;
    }

    @Override public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        // Histogram
        int startIdx = model.getVisibleStart();
        int endIdx = model.getVisibleEnd();
        double barW = model.getBarWidth(chartW) * 0.5;
        for (int i = startIdx; i < endIdx; i++) {
            if (i >= histogram.length || Double.isNaN(histogram[i])) continue;
            double x = model.barX(i, chartW);
            double zeroY = model.priceToY(0, chartH);
            double valY = model.priceToY(histogram[i], chartH);
            g.setColor(histogram[i] >= 0 ? ChartColor.VOLUME_BULL : ChartColor.VOLUME_BEAR);
            int top = (int) Math.min(zeroY, valY);
            int h = (int) Math.abs(valY - zeroY);
            g.fillRect((int)(x - barW/2), top, (int)barW, Math.max(1, h));
        }
        // MACD line
        g.setStroke(new BasicStroke(1.5f));
        g.setColor(color);
        boolean drawing = false; int px = 0, py = 0;
        for (int i = startIdx; i < endIdx; i++) {
            if (i >= values.length || Double.isNaN(values[i])) { drawing = false; continue; }
            int x = (int) model.barX(i, chartW), y = (int) model.priceToY(values[i], chartH);
            if (drawing) g.drawLine(px, py, x, y);
            px = x; py = y; drawing = true;
        }
        // Signal line
        g.setColor(ChartColor.INDICATOR_2);
        drawing = false;
        for (int i = startIdx; i < endIdx; i++) {
            if (i >= signal.length || Double.isNaN(signal[i])) { drawing = false; continue; }
            int x = (int) model.barX(i, chartW), y = (int) model.priceToY(signal[i], chartH);
            if (drawing) g.drawLine(px, py, x, y);
            px = x; py = y; drawing = true;
        }
        drawLabel(g, model.getLeftMargin() + 5, model.getTopMargin() + 15);
    }

    public double[] getSignal() { return signal; }
    public double[] getHistogram() { return histogram; }
}