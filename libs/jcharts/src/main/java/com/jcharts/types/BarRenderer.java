package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;

/**
 * Traditional OHLC bar chart renderer.
 * Draws a vertical line from high to low, with short horizontal ticks
 * for the open (left side) and close (right side).
 */
public class BarRenderer extends ChartRenderer {

    private static final float TICK_RATIO = 0.3f;
    private static final float BAR_WIDTH = 1.5f;

    public BarRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "OHLC Bar";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        double barW = model.getBarWidth(chartW);
        int tickLen = Math.max(3, (int) (barW * TICK_RATIO));
        Stroke barStroke = new BasicStroke(BAR_WIDTH, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);
        g.setStroke(barStroke);

        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i++) {
            OHLCBar bar = model.getData().getBar(i);
            double x = model.barX(i, chartW);
            int cx = (int) x;

            double highY = model.priceToY(bar.getHigh(), chartH);
            double lowY = model.priceToY(bar.getLow(), chartH);
            double openY = model.priceToY(bar.getOpen(), chartH);
            double closeY = model.priceToY(bar.getClose(), chartH);

            boolean bullish = bar.isBullish();
            Color color = bullish ? ChartColor.BULLISH : ChartColor.BEARISH;
            g.setColor(color);

            // Vertical line from high to low
            g.drawLine(cx, (int) highY, cx, (int) lowY);

            // Open tick (left side)
            g.drawLine(cx - tickLen, (int) openY, cx, (int) openY);

            // Close tick (right side)
            g.drawLine(cx, (int) closeY, cx + tickLen, (int) closeY);
        }
    }
}
