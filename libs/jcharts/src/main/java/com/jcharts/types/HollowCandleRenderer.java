package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;

/**
 * Hollow candlestick chart renderer.
 * Bullish candles are drawn as hollow (outline only) green boxes.
 * Bearish candles are drawn as filled red boxes.
 * This style emphasizes trend direction with visual weight.
 */
public class HollowCandleRenderer extends ChartRenderer {

    private static final float BODY_WIDTH_RATIO = 0.7f;
    private static final float WICK_WIDTH = 1.5f;

    public HollowCandleRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Hollow Candle";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        double barW = model.getBarWidth(chartW);
        int bodyW = Math.max(2, (int) (barW * BODY_WIDTH_RATIO));
        int halfBody = bodyW / 2;
        Stroke wickStroke = new BasicStroke(WICK_WIDTH, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);
        Stroke borderStroke = new BasicStroke(1.2f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);

        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i++) {
            OHLCBar bar = model.getData().getBar(i);
            double x = model.barX(i, chartW);
            int cx = (int) x;

            double openY = model.priceToY(bar.getOpen(), chartH);
            double closeY = model.priceToY(bar.getClose(), chartH);
            double highY = model.priceToY(bar.getHigh(), chartH);
            double lowY = model.priceToY(bar.getLow(), chartH);

            boolean bullish = bar.isBullish();

            // Draw wick
            g.setStroke(wickStroke);
            g.setColor(bullish ? ChartColor.BULLISH : ChartColor.BEARISH);
            g.drawLine(cx, (int) highY, cx, (int) lowY);

            // Body rectangle
            int bodyTop = (int) Math.min(openY, closeY);
            int bodyBottom = (int) Math.max(openY, closeY);
            int bodyHeight = Math.max(1, bodyBottom - bodyTop);

            if (bullish) {
                // Hollow body: fill with background, outline with green
                g.setColor(ChartColor.BACKGROUND);
                g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
                g.setStroke(borderStroke);
                g.setColor(ChartColor.BULLISH);
                g.drawRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
            } else {
                // Filled body: solid red fill with darker border
                g.setColor(ChartColor.BEARISH);
                g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
                g.setStroke(borderStroke);
                g.setColor(ChartColor.withAlpha(ChartColor.BEARISH, 220));
                g.drawRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
            }

            // For doji-like candles, ensure a thin line is visible
            if (bodyHeight <= 2) {
                g.setStroke(new BasicStroke(1.0f));
                g.setColor(bullish ? ChartColor.BULLISH : ChartColor.BEARISH);
                g.drawLine(cx - halfBody, bodyTop, cx + halfBody, bodyTop);
            }
        }
    }
}
