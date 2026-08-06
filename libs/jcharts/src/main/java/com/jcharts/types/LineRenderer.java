package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * Simple line chart renderer connecting close prices with a smooth polyline.
 * Draws a gradient-filled area beneath the line for visual depth, plus a
 * glowing effect by drawing the line multiple times with decreasing width and increasing opacity.
 */
public class LineRenderer extends ChartRenderer {

    public LineRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Line";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);
        double bottomY = model.priceToY(model.getMinPrice(), chartH);

        // Build the polyline path for close prices
        Path2D.Double linePath = new Path2D.Double();
        Path2D.Double fillPath = new Path2D.Double();
        boolean first = true;

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double x = model.barX(i, chartW);
            double y = model.priceToY(bar.getClose(), chartH);

            if (first) {
                linePath.moveTo(x, y);
                fillPath.moveTo(x, y);
                first = false;
            } else {
                linePath.lineTo(x, y);
                fillPath.lineTo(x, y);
            }
        }

        // Fill area beneath the line with gradient
        if (!first) {
            double lastX = model.barX(end - 1, chartW);
            double firstX = model.barX(start, chartW);
            fillPath.lineTo(lastX, bottomY);
            fillPath.lineTo(firstX, bottomY);
            fillPath.closePath();

            GradientPaint gradient = new GradientPaint(
                    0, model.getTopMargin(),
                    ChartColor.withAlpha(ChartColor.ACCENT, 60),
                    0, model.getTopMargin() + priceH,
                    ChartColor.withAlpha(ChartColor.ACCENT, 5)
            );
            g.setPaint(gradient);
            g.fill(fillPath);
        }

        // Draw glow effect (wider, semi-transparent lines beneath)
        g.setStroke(new BasicStroke(6.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.ACCENT, 30));
        g.draw(linePath);

        g.setStroke(new BasicStroke(3.5f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.ACCENT, 80));
        g.draw(linePath);

        // Draw main line
        g.setStroke(new BasicStroke(1.8f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.ACCENT);
        g.draw(linePath);

        // Draw dot at the latest close price
        if (end > start) {
            OHLCBar lastBar = data.getBar(end - 1);
            double lx = model.barX(end - 1, chartW);
            double ly = model.priceToY(lastBar.getClose(), chartH);
            g.setColor(ChartColor.ACCENT);
            g.fillOval((int) lx - 4, (int) ly - 4, 8, 8);
            g.setColor(ChartColor.WHITE);
            g.fillOval((int) lx - 2, (int) ly - 2, 4, 4);
        }
    }
}
