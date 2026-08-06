package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Kagi chart renderer.
 * Draws vertical lines connected by horizontal segments.
 * Green (yang) lines when the market moves in the predominant direction,
 * red (yin) lines on reversals. Thicker lines when the trend continues,
 * thinner lines at reversal points. Reversal threshold is based on a percentage
 * of the price range.
 */
public class KagiRenderer extends ChartRenderer {

    private static final double REVERSAL_PERCENTAGE = 0.03; // 3% reversal threshold

    /** A segment in the Kagi chart: vertical or horizontal line */
    private static class KagiSegment {
        final double x1, y1, x2, y2;
        final boolean yang;
        final boolean thick;

        KagiSegment(double x1, double y1, double x2, double y2, boolean yang, boolean thick) {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.yang = yang;
            this.thick = thick;
        }
    }

    public KagiRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Kagi";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        List<KagiSegment> segments = computeKagiSegments(data);
        if (segments.isEmpty()) return;

        for (KagiSegment seg : segments) {
            // Map the segment indices to screen coordinates
            double sx1 = model.barX((int) seg.x1, chartW);
            double sy1 = model.priceToY(seg.y1, chartH);
            double sx2 = model.barX((int) seg.x2, chartW);
            double sy2 = model.priceToY(seg.y2, chartH);

            Color color = seg.yang ? ChartColor.BULLISH : ChartColor.BEARISH;
            float thickness = seg.thick ? 3.0f : 1.5f;

            g.setColor(color);
            g.setStroke(new BasicStroke(thickness, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.drawLine((int) sx1, (int) sy1, (int) sx2, (int) sy2);
        }
    }

    /**
     * Build Kagi segments from OHLC data.
     * Kagi uses closing prices and reverses when price moves beyond a threshold.
     */
    private List<KagiSegment> computeKagiSegments(com.jcharts.data.TimeSeries data) {
        List<KagiSegment> segments = new ArrayList<>();
        if (data.size() < 2) return segments;

        double reversalThreshold = computeReversalThreshold(data);

        // State
        int currentXIdx = 0;         // bar index for horizontal position
        double currentPrice = data.getBar(0).getClose();
        boolean yang = true;         // start as yang (up direction)
        boolean thick = true;         // trend continuing

        for (int i = 1; i < data.size(); i++) {
            OHLCBar bar = data.getBar(i);
            double close = bar.getClose();
            double range = Math.abs(close - currentPrice);

            if (range < reversalThreshold * 0.1) continue;

            if (yang) {
                if (close > currentPrice) {
                    // Continue yang trend
                    segments.add(new KagiSegment(
                            currentXIdx, currentPrice,
                            i, close,
                            yang, thick
                    ));
                    currentXIdx = i;
                    currentPrice = close;
                    thick = true;
                } else if (close < currentPrice - reversalThreshold) {
                    // Reverse to yin
                    segments.add(new KagiSegment(
                            currentXIdx, currentPrice,
                            i, close,
                            false, false
                    ));
                    currentXIdx = i;
                    currentPrice = close;
                    yang = false;
                    thick = false;
                }
            } else {
                if (close < currentPrice) {
                    // Continue yin trend
                    segments.add(new KagiSegment(
                            currentXIdx, currentPrice,
                            i, close,
                            yang, thick
                    ));
                    currentXIdx = i;
                    currentPrice = close;
                    thick = true;
                } else if (close > currentPrice + reversalThreshold) {
                    // Reverse to yang
                    segments.add(new KagiSegment(
                            currentXIdx, currentPrice,
                            i, close,
                            true, false
                    ));
                    currentXIdx = i;
                    currentPrice = close;
                    yang = true;
                    thick = false;
                }
            }
        }

        return segments;
    }

    private double computeReversalThreshold(com.jcharts.data.TimeSeries data) {
        double sum = 0;
        for (int i = 0; i < data.size(); i++) {
            sum += data.getBar(i).getRange();
        }
        double avgRange = data.size() > 0 ? sum / data.size() : 0;
        return avgRange * REVERSAL_PERCENTAGE * 10;
    }
}
