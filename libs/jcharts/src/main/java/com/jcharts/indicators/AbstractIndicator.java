package com.jcharts.indicators;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import com.jcharts.data.TimeSeries;
import java.awt.*;

/** Base class for single-value line indicators. */
public abstract class AbstractIndicator implements Indicator {
    protected double[] values;
    protected Color color;
    protected final String name;
    protected final String shortName;
    protected float strokeWidth = 1.5f;

    protected AbstractIndicator(String name, String shortName, Color color) {
        this.name = name; this.shortName = shortName; this.color = color;
    }

    @Override public String getName() { return name; }
    @Override public String getShortName() { return shortName; }
    @Override public Color getColor() { return color; }
    @Override public void setColor(Color c) { this.color = c; }
    @Override public double[] getValues() { return values; }

    protected void drawLine(Graphics2D g, ChartModel model, int chartW, int chartH, double[] vals, int offset) {
        if (vals == null || vals.length == 0) return;
        g.setStroke(new BasicStroke(strokeWidth));
        g.setColor(color);
        int startIdx = model.getVisibleStart();
        int endIdx = model.getVisibleEnd();
        boolean drawing = false;
        int prevX = 0, prevY = 0;
        for (int i = startIdx; i < endIdx; i++) {
            int vi = i - offset;
            if (vi < 0 || vi >= vals.length || Double.isNaN(vals[vi])) { drawing = false; continue; }
            int x = (int) model.barX(i, chartW);
            int y = (int) model.priceToY(vals[vi], chartH);
            if (drawing) g.drawLine(prevX, prevY, x, y);
            prevX = x; prevY = y; drawing = true;
        }
    }

    protected void drawLabel(Graphics2D g, int x, int y) {
        Font f = new Font("SansSerif", Font.PLAIN, 10);
        g.setFont(f);
        String label = shortName;
        if (values != null && values.length > 0) {
            double last = values[values.length - 1];
            if (!Double.isNaN(last)) label += ": " + String.format("%.2f", last);
        }
        FontMetrics fm = g.getFontMetrics();
        int tw = fm.stringWidth(label) + 10;
        g.setColor(new Color(color.getRed(), color.getGreen(), color.getBlue(), 200));
        g.fillRect(x, y - 2, tw, 16);
        g.setColor(Color.WHITE);
        g.drawString(label, x + 5, y + 10);
    }

    protected static double[] sma(double[] src, int period) {
        double[] out = new double[src.length];
        for (int i = 0; i < src.length; i++) {
            if (i < period - 1) { out[i] = Double.NaN; continue; }
            double sum = 0;
            for (int j = i - period + 1; j <= i; j++) sum += src[j];
            out[i] = sum / period;
        }
        return out;
    }

    protected static double[] ema(double[] src, int period) {
        double[] out = new double[src.length];
        double mult = 2.0 / (period + 1);
        double sum = 0;
        for (int i = 0; i < Math.min(period, src.length); i++) sum += src[i];
        out[period - 1] = sum / period;
        for (int i = 0; i < period - 1; i++) out[i] = Double.NaN;
        for (int i = period; i < src.length; i++) out[i] = (src[i] - out[i - 1]) * mult + out[i - 1];
        return out;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        drawLine(g, model, chartW, chartH, values, 0);
        int labelX = model.getLeftMargin() + 5;
        int labelY = model.getTopMargin() + 15;
        int idx = 0;
        for (Indicator ind : model.getIndicators()) {
            if (ind == this) break;
            idx++;
        }
        labelY += idx * 16;
        drawLabel(g, labelX, labelY);
    }
}