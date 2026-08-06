package com.jcharts.types.original;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Rectangle2D;

/**
 * ORIGINAL chart type: Flow Force renderer.
 * Displays vertical bars representing cumulative buy/sell flow force,
 * computed from close-to-close price changes scaled by volume.
 * Positive flow (buying pressure) shown as upward bars with green gradient.
 * Negative flow (selling pressure) shown as downward bars with red gradient.
 * A running cumulative total creates a flow force profile over time.
 */
public class FlowForceRenderer extends ChartRenderer {

    public FlowForceRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Flow Force";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);
        int count = end - start;

        // Compute raw flow force per bar: (close - open) / open * volume
        double[] rawFlow = new double[count];
        double maxAbsFlow = Double.MIN_VALUE;
        for (int i = 0; i < count; i++) {
            OHLCBar bar = data.getBar(start + i);
            double priceChange = bar.getClose() - bar.getOpen();
            double vol = bar.getVolume();
            rawFlow[i] = priceChange * vol; // directional volume * price change
            maxAbsFlow = Math.max(maxAbsFlow, Math.abs(rawFlow[i]));
        }
        if (maxAbsFlow <= 0) maxAbsFlow = 1;

        // Compute cumulative flow
        double[] cumulativeFlow = new double[count];
        cumulativeFlow[0] = rawFlow[0];
        for (int i = 1; i < count; i++) {
            cumulativeFlow[i] = cumulativeFlow[i - 1] + rawFlow[i];
        }

        // Find cumulative range
        double maxCum = Double.MIN_VALUE;
        double minCum = Double.MAX_VALUE;
        for (double v : cumulativeFlow) {
            maxCum = Math.max(maxCum, v);
            minCum = Math.min(minCum, v);
        }
        double cumRange = maxCum - minCum;
        if (cumRange <= 0) cumRange = 1;

        double barW = model.getBarWidth(chartW);
        int barWidth = Math.max(2, (int) (barW * 0.7));
        int halfBar = barWidth / 2;

        // Zero line for cumulative flow
        double zeroY = model.getTopMargin() + priceH * ((maxCum - 0) / (maxCum - minCum + cumRange * 0.1));
        if (minCum > 0) zeroY = model.getTopMargin() + priceH + 5;
        if (maxCum < 0) zeroY = model.getTopMargin() - 5;

        // Draw zero reference line
        g.setColor(ChartColor.GRID_LINE_LIGHT);
        g.setStroke(new BasicStroke(1.0f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL,
                10.0f, new float[]{6.0f, 4.0f}, 0.0f));
        g.drawLine(model.getLeftMargin(), (int) zeroY, model.getLeftMargin() + chartW, (int) zeroY);

        // Normalize cumulative for display
        double displayMin = Math.min(minCum, 0) - cumRange * 0.05;
        double displayMax = Math.max(maxCum, 0) + cumRange * 0.05;
        double displayRange = displayMax - displayMin;
        if (displayRange <= 0) displayRange = 1;

        zeroY = model.getTopMargin() + priceH * ((displayMax - 0) / displayRange);

        // Draw cumulative flow as vertical bars from zero line
        for (int i = 0; i < count; i++) {
            double x = model.barX(start + i, chartW);
            double value = cumulativeFlow[i];
            double normalizedValue = (displayMax - value) / displayRange;
            double barTopY = model.getTopMargin() + priceH * normalizedValue;

            boolean isPositive = value >= 0;
            int barTop, barHeight;

            if (isPositive) {
                barTop = (int) barTopY;
                barHeight = Math.max(1, (int) (zeroY - barTopY));
            } else {
                barTop = (int) zeroY;
                barHeight = Math.max(1, (int) (barTopY - zeroY));
            }

            int bx = (int) (x - halfBar);

            // Draw gradient-filled bar
            if (isPositive) {
                GradientPaint grad = new GradientPaint(
                        bx, barTop,
                        ChartColor.BULLISH,
                        bx, barTop + barHeight,
                        ChartColor.withAlpha(ChartColor.BULLISH, 100)
                );
                g.setPaint(grad);
            } else {
                GradientPaint grad = new GradientPaint(
                        bx, barTop,
                        ChartColor.withAlpha(ChartColor.BEARISH, 100),
                        bx, barTop + barHeight,
                        ChartColor.BEARISH
                );
                g.setPaint(grad);
            }
            g.fillRect(bx, barTop, barWidth, barHeight);

            // Subtle border
            g.setColor(ChartColor.withAlpha(isPositive ? ChartColor.BULLISH : ChartColor.BEARISH, 150));
            g.drawRect(bx, barTop, barWidth, barHeight);
        }

        // Draw the cumulative line on top
        java.awt.geom.Path2D.Double cumLine = new java.awt.geom.Path2D.Double();
        boolean first = true;
        for (int i = 0; i < count; i++) {
            double x = model.barX(start + i, chartW);
            double normalizedValue = (displayMax - cumulativeFlow[i]) / displayRange;
            double y = model.getTopMargin() + priceH * normalizedValue;
            if (first) { cumLine.moveTo(x, y); first = false; }
            else cumLine.lineTo(x, y);
        }

        g.setStroke(new BasicStroke(2.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.ACCENT);
        g.draw(cumLine);

        // Draw dot at endpoint
        double lastVal = cumulativeFlow[count - 1];
        double lastNorm = (displayMax - lastVal) / displayRange;
        double lastY = model.getTopMargin() + priceH * lastNorm;
        double lastX = model.barX(end - 1, chartW);
        g.setColor(lastVal >= 0 ? ChartColor.BULLISH : ChartColor.BEARISH);
        g.fillOval((int) lastX - 4, (int) lastY - 4, 8, 8);
        g.setColor(ChartColor.WHITE);
        g.fillOval((int) lastX - 2, (int) lastY - 2, 4, 4);
    }
}
