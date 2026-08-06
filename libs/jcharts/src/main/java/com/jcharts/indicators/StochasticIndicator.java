package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Stochastic Oscillator (%K and %D lines, 0-100 scale). */
public class StochasticIndicator extends AbstractIndicator {
    private final int kPeriod, dPeriod;
    private double[] dValues;

    public StochasticIndicator() { this(14, 3); }
    public StochasticIndicator(int kPeriod, int dPeriod) {
        super("Stoch(" + kPeriod + "," + dPeriod + ")", "Stoch", ChartColor.INDICATOR_4);
        this.kPeriod = kPeriod; this.dPeriod = dPeriod;
    }

    @Override public void calculate(TimeSeries data) {
        int n = data.size();
        values = new double[n]; dValues = new double[n];
        if (n < kPeriod) return;
        for (int i = kPeriod - 1; i < n; i++) {
            double highest = -Double.MAX_VALUE, lowest = Double.MAX_VALUE;
            for (int j = i - kPeriod + 1; j <= i; j++) {
                highest = Math.max(highest, data.getBar(j).getHigh());
                lowest = Math.min(lowest, data.getBar(j).getLow());
            }
            double range = highest - lowest;
            values[i] = range == 0 ? 50 : ((data.getBar(i).getClose() - lowest) / range) * 100;
        }
        for (int i = 0; i < kPeriod - 1; i++) values[i] = Double.NaN;
        dValues = sma(values, dPeriod);
    }

    @Override public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        if (model.isAutoScale()) { model.setManualMinPrice(0); model.setManualMaxPrice(100); }
        int y80 = (int) model.priceToY(80, chartH), y20 = (int) model.priceToY(20, chartH);
        g.setStroke(new BasicStroke(0.5f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL, 0, new float[]{4, 4}, 0));
        g.setColor(ChartColor.TEXT_DIM);
        g.drawLine(model.getLeftMargin(), y80, model.getLeftMargin() + chartW, y80);
        g.drawLine(model.getLeftMargin(), y20, model.getLeftMargin() + chartW, y20);
        g.setFont(new Font("SansSerif", Font.PLAIN, 9));
        g.drawString("80", model.getLeftMargin() + chartW + 3, y80 + 4);
        g.drawString("20", model.getLeftMargin() + chartW + 3, y20 + 4);
        drawLine(g, model, chartW, chartH, values, 0);
        // %D
        g.setColor(ChartColor.INDICATOR_2);
        g.setStroke(new BasicStroke(1.2f));
        boolean drawing = false; int px = 0, py = 0;
        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i++) {
            if (i >= dValues.length || Double.isNaN(dValues[i])) { drawing = false; continue; }
            int x = (int) model.barX(i, chartW), y = (int) model.priceToY(dValues[i], chartH);
            if (drawing) g.drawLine(px, py, x, y);
            px = x; py = y; drawing = true;
        }
        drawLabel(g, model.getLeftMargin() + 5, model.getTopMargin() + 15);
    }

    public double[] getDValues() { return dValues; }
}