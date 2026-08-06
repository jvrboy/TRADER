package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Heikin Ashi candlestick chart renderer.
 * Computes Heikin Ashi candles from raw OHLC data using the standard formula:
 *   HA-Close = (Open + High + Low + Close) / 4
 *   HA-Open  = (prev HA-Open + prev HA-Close) / 2
 *   HA-High  = max(High, HA-Open, HA-Close)
 *   HA-Low   = min(Low, HA-Open, HA-Close)
 * Renders smooth candles that filter out market noise, showing clearer trend direction.
 */
public class HeikinAshiRenderer extends ChartRenderer {

    private static final float BODY_WIDTH_RATIO = 0.7f;
    private static final float WICK_WIDTH = 1.5f;

    /** Holds computed Heikin Ashi candle data */
    private static class HACandle {
        final double open, high, low, close;

        HACandle(double open, double high, double low, double close) {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
        }

        boolean isBullish() { return close >= open; }
    }

    public HeikinAshiRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Heikin Ashi";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        // Compute HA candles for the full dataset
        List<HACandle> haCandles = computeHeikinAshi(data);

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        double barW = model.getBarWidth(chartW);
        int bodyW = Math.max(2, (int) (barW * BODY_WIDTH_RATIO));
        int halfBody = bodyW / 2;
        Stroke wickStroke = new BasicStroke(WICK_WIDTH, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);

        g.setStroke(wickStroke);

        for (int i = start; i < end; i++) {
            HACandle ha = haCandles.get(i);
            double x = model.barX(i, chartW);
            int cx = (int) x;

            double openY = model.priceToY(ha.open, chartH);
            double closeY = model.priceToY(ha.close, chartH);
            double highY = model.priceToY(ha.high, chartH);
            double lowY = model.priceToY(ha.low, chartH);

            boolean bullish = ha.isBullish();
            Color bodyColor = bullish ? ChartColor.BULLISH : ChartColor.BEARISH;

            // Draw wick
            g.setColor(bodyColor);
            g.drawLine(cx, (int) highY, cx, (int) lowY);

            // Draw candle body
            int bodyTop = (int) Math.min(openY, closeY);
            int bodyBottom = (int) Math.max(openY, closeY);
            int bodyHeight = Math.max(1, bodyBottom - bodyTop);

            g.setColor(bodyColor);
            g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);

            // Upper shadow for bullish candles (only lower wick is drawn)
            // Lower shadow for bearish candles (only upper wick is drawn)
            if (bullish && ha.low < Math.min(ha.open, ha.close)) {
                // Shadow below body - already drawn by wick line
            }
            if (!bullish && ha.high > Math.max(ha.open, ha.close)) {
                // Shadow above body - already drawn by wick line
            }

            // Subtle highlight on bullish candles for depth
            if (bullish && bodyHeight > 4) {
                g.setColor(ChartColor.withAlpha(ChartColor.WHITE, 25));
                g.fillRect(cx - halfBody + 1, bodyTop + 1, bodyW / 3, bodyHeight - 2);
            }
        }
    }

    /**
     * Compute Heikin Ashi candles from the raw OHLC time series.
     */
    private List<HACandle> computeHeikinAshi(com.jcharts.data.TimeSeries data) {
        List<HACandle> haCandles = new ArrayList<>(data.size());
        double prevHAOpen = 0;
        double prevHAClose = 0;

        for (int i = 0; i < data.size(); i++) {
            OHLCBar bar = data.getBar(i);

            double haClose = (bar.getOpen() + bar.getHigh() + bar.getLow() + bar.getClose()) / 4.0;
            double haOpen;
            if (i == 0) {
                haOpen = (bar.getOpen() + bar.getClose()) / 2.0;
            } else {
                haOpen = (prevHAOpen + prevHAClose) / 2.0;
            }
            double haHigh = Math.max(bar.getHigh(), Math.max(haOpen, haClose));
            double haLow = Math.min(bar.getLow(), Math.min(haOpen, haClose));

            haCandles.add(new HACandle(haOpen, haHigh, haLow, haClose));
            prevHAOpen = haOpen;
            prevHAClose = haClose;
        }

        return haCandles;
    }
}
