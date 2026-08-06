package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;

/**
 * Classic OHLC candlestick chart renderer.
 * Green filled candles for bullish (close >= open), red filled candles for bearish.
 * Draws rectangular bodies and thin wicks extending to high and low prices.
 */
public class CandlestickRenderer extends ChartRenderer {

    private static final float BODY_WIDTH_RATIO = 0.7f;
    private static final float WICK_WIDTH = 1.5f;

    public CandlestickRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Candlestick";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        int priceH = model.getPriceAreaHeight(chartH);
        double barW = model.getBarWidth(chartW);
        int bodyW = Math.max(2, (int) (barW * BODY_WIDTH_RATIO));
        int halfBody = bodyW / 2;
        Stroke wickStroke = new BasicStroke(WICK_WIDTH, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);

        g.setStroke(wickStroke);

        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i++) {
            OHLCBar bar = model.getData().getBar(i);
            double x = model.barX(i, chartW);
            double openY = model.priceToY(bar.getOpen(), chartH);
            double closeY = model.priceToY(bar.getClose(), chartH);
            double highY = model.priceToY(bar.getHigh(), chartH);
            double lowY = model.priceToY(bar.getLow(), chartH);

            boolean bullish = bar.isBullish();
            Color bodyColor = bullish ? ChartColor.BULLISH : ChartColor.BEARISH;

            // Draw wick (high-low line)
            g.setColor(bodyColor);
            int cx = (int) x;
            g.drawLine(cx, (int) highY, cx, (int) lowY);

            // Draw candle body
            int bodyTop = (int) Math.min(openY, closeY);
            int bodyBottom = (int) Math.max(openY, closeY);
            int bodyHeight = Math.max(1, bodyBottom - bodyTop);

            g.setColor(bodyColor);
            g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);

            // Subtle border for definition
            g.setColor(ChartColor.withAlpha(bodyColor, 200));
            g.drawRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
        }
    }
}
