package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Three Line Break chart renderer.
 * Displays price action as a series of rising (white/green) and falling (black/red) boxes.
 * A new box in the same direction opens when price exceeds the high (or low) of the
 * current box. A reversal occurs only when price exceeds the high (or low) of the
 * last three consecutive boxes in the opposite direction.
 */
public class LineBreakRenderer extends ChartRenderer {

    private static final int LINE_BREAK_COUNT = 3; // Three Line Break

    /** Represents a single Line Break box */
    private static class LBBox {
        final double open, high, low, close;
        final boolean bullish;
        final int index;

        LBBox(double open, double high, double low, double close, boolean bullish, int index) {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
            this.bullish = bullish;
            this.index = index;
        }
    }

    public LineBreakRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Line Break";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        List<LBBox> boxes = computeLineBreakBoxes(data);
        if (boxes.isEmpty()) return;

        double barW = model.getBarWidth(chartW);
        int bodyW = Math.max(2, (int) (barW * 0.85));
        int halfBody = bodyW / 2;
        Stroke wickStroke = new BasicStroke(1.5f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER);

        // Offset to keep boxes visually connected (no gaps)
        int boxSpacing = Math.max(1, (int) barW) + 1;

        // Calculate total width needed and center
        int totalWidth = boxes.size() * boxSpacing;
        int startX = model.getLeftMargin() + Math.max(0, (chartW - totalWidth) / 2);

        int priceH = model.getPriceAreaHeight(chartH);

        for (int i = 0; i < boxes.size(); i++) {
            LBBox box = boxes.get(i);
            int cx = startX + i * boxSpacing;

            double openY = model.priceToY(box.open, chartH);
            double closeY = model.priceToY(box.close, chartH);
            double highY = model.priceToY(box.high, chartH);
            double lowY = model.priceToY(box.low, chartH);

            Color color = box.bullish ? ChartColor.BULLISH : ChartColor.BEARISH;
            Color fillColor = box.bullish ? ChartColor.WHITE : ChartColor.BEARISH;

            // Draw wick
            g.setStroke(wickStroke);
            g.setColor(color);
            g.drawLine(cx, (int) highY, cx, (int) lowY);

            // Draw body
            int bodyTop = (int) Math.min(openY, closeY);
            int bodyBottom = (int) Math.max(openY, closeY);
            int bodyHeight = Math.max(1, bodyBottom - bodyTop);

            if (box.bullish) {
                // White (hollow) body with green outline
                g.setColor(fillColor);
                g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
                g.setColor(color);
                g.drawRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
            } else {
                // Filled red body
                g.setColor(fillColor);
                g.fillRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
                g.setColor(ChartColor.withAlpha(ChartColor.BEARISH, 220));
                g.drawRect(cx - halfBody, bodyTop, bodyW, bodyHeight);
            }
        }
    }

    /**
     * Compute Three Line Break boxes from raw OHLC data.
     */
    private List<LBBox> computeLineBreakBoxes(com.jcharts.data.TimeSeries data) {
        List<LBBox> boxes = new ArrayList<>();
        if (data.isEmpty()) return boxes;

        // First box uses the first bar
        OHLCBar first = data.getBar(0);
        boxes.add(new LBBox(first.getOpen(), first.getHigh(), first.getLow(), first.getClose(),
                first.isBullish(), 0));

        for (int i = 1; i < data.size(); i++) {
            OHLCBar bar = data.getBar(i);
            LBBox lastBox = boxes.get(boxes.size() - 1);

            if (lastBox.bullish) {
                // Current trend is bullish - check for continuation or reversal
                if (bar.getClose() > lastBox.high) {
                    // New bullish box
                    boxes.add(new LBBox(lastBox.close, bar.getHigh(), lastBox.close, bar.getClose(), true, boxes.size()));
                } else if (boxes.size() >= LINE_BREAK_COUNT && bar.getClose() < getLowestLow(boxes, LINE_BREAK_COUNT)) {
                    // Reversal: price broke below the low of last N bearish boxes
                    // Actually for line break reversal, we check the lowest low of the last N boxes
                    // that are in the SAME opposite direction, or just last N boxes
                    double revLow = lastBox.low;
                    // Find the lowest low of the last LINE_BREAK_COUNT boxes
                    int startIdx = Math.max(0, boxes.size() - LINE_BREAK_COUNT);
                    for (int j = startIdx; j < boxes.size(); j++) {
                        revLow = Math.min(revLow, boxes.get(j).low);
                    }
                    if (bar.getClose() < revLow) {
                        boxes.add(new LBBox(lastBox.close, lastBox.close, bar.getLow(), bar.getClose(), false, boxes.size()));
                    }
                }
            } else {
                // Current trend is bearish - check for continuation or reversal
                if (bar.getClose() < lastBox.low) {
                    // New bearish box
                    boxes.add(new LBBox(lastBox.close, lastBox.close, bar.getLow(), bar.getClose(), false, boxes.size()));
                } else if (boxes.size() >= LINE_BREAK_COUNT) {
                    double revHigh = lastBox.high;
                    int startIdx = Math.max(0, boxes.size() - LINE_BREAK_COUNT);
                    for (int j = startIdx; j < boxes.size(); j++) {
                        revHigh = Math.max(revHigh, boxes.get(j).high);
                    }
                    if (bar.getClose() > revHigh) {
                        boxes.add(new LBBox(lastBox.close, bar.getHigh(), lastBox.close, bar.getClose(), true, boxes.size()));
                    }
                }
            }
        }

        return boxes;
    }

    private double getLowestLow(List<LBBox> boxes, int count) {
        double lowest = Double.MAX_VALUE;
        int start = Math.max(0, boxes.size() - count);
        for (int i = start; i < boxes.size(); i++) {
            lowest = Math.min(lowest, boxes.get(i).low);
        }
        return lowest;
    }
}
