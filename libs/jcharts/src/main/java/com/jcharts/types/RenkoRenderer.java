package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Renko chart renderer.
 * Uses a fixed brick size computed from ATR (Average True Range).
 * Bricks are drawn in green (up moves) and red (down moves) with no gaps.
 * Only price changes exceeding the brick size generate new bricks.
 */
public class RenkoRenderer extends ChartRenderer {

    private static final int ATR_PERIOD = 14;
    private static final double DEFAULT_BRICK_SIZE = 0.5; // fallback percentage

    /** Represents a single Renko brick */
    private static class Brick {
        final double price;
        final boolean up;

        Brick(double price, boolean up) {
            this.price = price;
            this.up = up;
        }
    }

    public RenkoRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Renko";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        List<Brick> bricks = computeRenkoBricks(data);
        if (bricks.isEmpty()) return;

        int priceH = model.getPriceAreaHeight(chartH);
        int visibleCount = model.getVisibleCount();
        double brickPixelW = (double) chartW / visibleCount;
        int brickH = Math.max(2, (int) (priceH * 0.015));

        // Draw bricks from left to right
        for (int i = 0; i < bricks.size(); i++) {
            Brick brick = bricks.get(i);
            int brickIndex = i;
            if (brickIndex < model.getVisibleStart() || brickIndex >= model.getVisibleEnd()) continue;

            double x = model.barX(i, chartW);
            double topY = model.priceToY(brick.price, chartH);
            int bx = (int) (x - brickPixelW / 2);
            int by = (int) topY;
            int bw = Math.max(2, (int) brickPixelW - 1);

            if (brick.up) {
                g.setColor(ChartColor.BULLISH);
                g.fillRect(bx, by, bw, brickH);
                // Subtle border
                g.setColor(ChartColor.withAlpha(ChartColor.BULLISH, 180));
                g.drawRect(bx, by, bw, brickH);
            } else {
                g.setColor(ChartColor.BEARISH);
                g.fillRect(bx, by, bw, brickH);
                // Subtle border
                g.setColor(ChartColor.withAlpha(ChartColor.BEARISH, 180));
                g.drawRect(bx, by, bw, brickH);
            }
        }
    }

    /**
     * Compute Renko bricks from the raw time series using ATR-based brick size.
     */
    private List<Brick> computeRenkoBricks(com.jcharts.data.TimeSeries data) {
        double brickSize = computeATR(data, ATR_PERIOD);
        if (brickSize <= 0) {
            // Fallback: use percentage of average price
            double avgPrice = data.getBar(0).getClose();
            brickSize = avgPrice * DEFAULT_BRICK_SIZE / 100.0;
        }

        List<Brick> bricks = new ArrayList<>();
        if (data.isEmpty()) return bricks;

        double currentPrice = data.getBar(0).getOpen();

        for (int i = 0; i < data.size(); i++) {
            OHLCBar bar = data.getBar(i);

            // Check for up moves
            double targetUp = currentPrice + brickSize;
            if (bar.getHigh() >= targetUp) {
                // Calculate how many bricks we need
                int count = 0;
                double testPrice = currentPrice;
                while (testPrice + brickSize <= bar.getHigh() && count < 100) {
                    testPrice += brickSize;
                    bricks.add(new Brick(testPrice, true));
                    count++;
                }
                currentPrice = testPrice;
            }

            // Check for down moves
            double targetDown = currentPrice - brickSize;
            if (bar.getLow() <= targetDown) {
                int count = 0;
                double testPrice = currentPrice;
                while (testPrice - brickSize >= bar.getLow() && count < 100) {
                    testPrice -= brickSize;
                    bricks.add(new Brick(testPrice, false));
                    count++;
                }
                currentPrice = testPrice;
            }
        }

        return bricks;
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
