package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Point &amp; Figure chart renderer.
 * Draws columns of X symbols (price moving up) and O symbols (price moving down).
 * Box size is computed from ATR. Reversal requires a 3-box move in the opposite direction.
 * Only price changes that fill complete boxes are plotted, filtering out noise.
 */
public class PointAndFigureRenderer extends ChartRenderer {

    private static final int ATR_PERIOD = 14;
    private static final int REVERSAL_BOXES = 3; // standard 3-box reversal

    /** Represents a single plotted symbol (X or O) at a given price level */
    private static class PFBox {
        final double price;
        final boolean isX;  // true = X (up), false = O (down)
        final int column;

        PFBox(double price, boolean isX, int column) {
            this.price = price;
            this.isX = isX;
            this.column = column;
        }
    }

    public PointAndFigureRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Point & Figure";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        List<PFBox> boxes = computePFBoxes(data);
        if (boxes.isEmpty()) return;

        int priceH = model.getPriceAreaHeight(chartH);
        int visibleCount = model.getVisibleCount();

        // Find column range for visible area
        int minCol = Integer.MAX_VALUE;
        int maxCol = Integer.MIN_VALUE;
        for (PFBox box : boxes) {
            minCol = Math.min(minCol, box.column);
            maxCol = Math.max(maxCol, box.column);
        }

        int totalColumns = maxCol - minCol + 1;
        if (totalColumns <= 0) return;

        double colWidth = (double) chartW / Math.max(totalColumns, visibleCount);
        double rowHeight = priceH * 0.02; // price step in pixels
        int symbolSize = Math.max(8, Math.min(16, (int) (colWidth * 0.6)));

        g.setFont(new Font("Monospaced", Font.BOLD, symbolSize));

        for (PFBox box : boxes) {
            int colIdx = box.column - minCol;
            double x = model.getLeftMargin() + colIdx * colWidth + colWidth / 2;
            double y = model.priceToY(box.price, chartH);

            if (x < model.getLeftMargin() || x > model.getLeftMargin() + chartW) continue;
            if (y < model.getTopMargin() || y > model.getTopMargin() + priceH) continue;

            if (box.isX) {
                g.setColor(ChartColor.BULLISH);
                drawXSymbol(g, (int) x, (int) y, symbolSize / 2);
            } else {
                g.setColor(ChartColor.BEARISH);
                drawOSymbol(g, (int) x, (int) y, symbolSize / 2);
            }
        }
    }

    /**
     * Draw an X symbol at the given center coordinates.
     */
    private void drawXSymbol(Graphics2D g, int cx, int cy, int half) {
        int thickness = Math.max(1, half / 4);
        g.setStroke(new BasicStroke(thickness));
        g.drawLine(cx - half, cy - half, cx + half, cy + half);
        g.drawLine(cx - half, cy + half, cx + half, cy - half);
    }

    /**
     * Draw an O symbol (circle) at the given center coordinates.
     */
    private void drawOSymbol(Graphics2D g, int cx, int cy, int radius) {
        int thickness = Math.max(1, radius / 3);
        g.setStroke(new BasicStroke(thickness));
        g.drawOval(cx - radius, cy - radius, radius * 2, radius * 2);
    }

    /**
     * Compute Point & Figure boxes from the raw OHLC data.
     */
    private List<PFBox> computePFBoxes(com.jcharts.data.TimeSeries data) {
        List<PFBox> boxes = new ArrayList<>();
        if (data.isEmpty()) return boxes;

        double boxSize = computeATR(data, ATR_PERIOD) * 0.5;
        if (boxSize <= 0) {
            double avgClose = data.getBar(0).getClose();
            boxSize = avgClose * 0.01; // fallback: 1% of price
        }
        double reversalSize = boxSize * REVERSAL_BOXES;

        double currentPrice = Math.floor(data.getBar(0).getClose() / boxSize) * boxSize;
        boolean inXColumn = true; // start in up column
        int currentColumn = 0;

        for (int i = 0; i < data.size(); i++) {
            OHLCBar bar = data.getBar(i);

            if (inXColumn) {
                // Moving up: fill X boxes up to the high
                double targetPrice = currentPrice;
                while (targetPrice + boxSize <= bar.getHigh()) {
                    targetPrice += boxSize;
                    boxes.add(new PFBox(targetPrice, true, currentColumn));
                }
                currentPrice = targetPrice;

                // Check for reversal
                if (bar.getLow() <= currentPrice - reversalSize) {
                    inXColumn = false;
                    currentColumn++;
                    double revTarget = currentPrice;
                    while (revTarget - boxSize >= bar.getLow()) {
                        revTarget -= boxSize;
                        boxes.add(new PFBox(revTarget, false, currentColumn));
                    }
                    currentPrice = revTarget;
                }
            } else {
                // Moving down: fill O boxes down to the low
                double targetPrice = currentPrice;
                while (targetPrice - boxSize >= bar.getLow()) {
                    targetPrice -= boxSize;
                    boxes.add(new PFBox(targetPrice, false, currentColumn));
                }
                currentPrice = targetPrice;

                // Check for reversal
                if (bar.getHigh() >= currentPrice + reversalSize) {
                    inXColumn = true;
                    currentColumn++;
                    double revTarget = currentPrice;
                    while (revTarget + boxSize <= bar.getHigh()) {
                        revTarget += boxSize;
                        boxes.add(new PFBox(revTarget, true, currentColumn));
                    }
                    currentPrice = revTarget;
                }
            }
        }

        return boxes;
    }

    /**
     * Compute Average True Range over the given period.
     */
    private double computeATR(com.jcharts.data.TimeSeries data, int period) {
        if (data.size() < 2) return 0;
        double atrSum = 0;
        int count = 0;
        for (int i = 1; i < data.size() && count < period; i++) {
            OHLCBar curr = data.getBar(i);
            OHLCBar prev = data.getBar(i - 1);
            double tr = Math.max(
                    curr.getHigh() - curr.getLow(),
                    Math.max(
                            Math.abs(curr.getHigh() - prev.getClose()),
                            Math.abs(curr.getLow() - prev.getClose())
                    )
            );
            atrSum += tr;
            count++;
        }
        return count > 0 ? atrSum / count : 0;
    }
}
