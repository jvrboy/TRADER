package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * Area chart renderer that fills the region below the close price line.
 * Uses a vertical gradient from accent color at the top to transparent at the bottom,
 * with a smooth line tracing the close prices along the top edge.
 */
public class AreaRenderer extends ChartRenderer {

    public AreaRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Area";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);
        double bottomY = model.priceToY(model.getMinPrice(), chartH);
        double firstX = model.barX(start, chartW);

        // Build the filled area path
        Path2D.Double areaPath = new Path2D.Double();
        Path2D.Double linePath = new Path2D.Double();
        boolean first = true;

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double x = model.barX(i, chartW);
            double y = model.priceToY(bar.getClose(), chartH);

            if (first) {
                areaPath.moveTo(x, bottomY);
                areaPath.lineTo(x, y);
                linePath.moveTo(x, y);
                first = false;
            } else {
                areaPath.lineTo(x, y);
                linePath.lineTo(x, y);
            }
        }

        if (!first) {
            double lastX = model.barX(end - 1, chartW);
            areaPath.lineTo(lastX, bottomY);
            areaPath.closePath();

            // Fill with gradient
            GradientPaint gradient = new GradientPaint(
                    0, model.getTopMargin(),
                    ChartColor.withAlpha(ChartColor.BULLISH, 80),
                    0, model.getTopMargin() + priceH,
                    ChartColor.withAlpha(ChartColor.BULLISH, 8)
            );
            g.setPaint(gradient);
            g.fill(areaPath);

            // Draw top line with glow
            g.setStroke(new BasicStroke(4.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(ChartColor.withAlpha(ChartColor.BULLISH, 40));
            g.draw(linePath);

            g.setStroke(new BasicStroke(2.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(ChartColor.BULLISH);
            g.draw(linePath);

            // Draw dot at the latest close
            OHLCBar lastBar = data.getBar(end - 1);
            double lx = model.barX(end - 1, chartW);
            double ly = model.priceToY(lastBar.getClose(), chartH);
            g.setColor(ChartColor.BULLISH);
            g.fillOval((int) lx - 4, (int) ly - 4, 8, 8);
            g.setColor(ChartColor.WHITE);
            g.fillOval((int) lx - 2, (int) ly - 2, 4, 4);
        }
    }
}
