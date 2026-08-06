package com.jcharts.types.original;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * ORIGINAL chart type: Quantum Ribbon renderer.
 * Renders multiple Exponential Moving Averages (EMAs) as a flowing gradient ribbon.
 * The ribbon width visually indicates trend strength - wider ribbons mean stronger
 * trends, narrow/converging ribbons indicate weak or ranging conditions.
 * Colors transition smoothly through a spectrum from the fastest to slowest EMA.
 */
public class QuantumRibbonRenderer extends ChartRenderer {

    private static final int[] EMA_PERIODS = {5, 8, 13, 21, 34, 55};
    private static final Color[] RIBBON_COLORS = {
            new Color(49, 211, 242),   // Cyan (fastest)
            new Color(77, 132, 240),   // Blue
            new Color(155, 89, 255),   // Purple
            new Color(211, 49, 242),   // Magenta
            new Color(242, 147, 49),   // Orange
            new Color(242, 185, 49)    // Yellow (slowest)
    };

    public QuantumRibbonRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Quantum Ribbon";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);
        int count = end - start;

        // Compute EMA values for each period
        int numEMAs = EMA_PERIODS.length;
        double[][] emas = new double[numEMAs][count];

        // We need data before 'start' for EMA warmup
        int warmupNeeded = EMA_PERIODS[EMA_PERIODS.length - 1];
        int dataStart = Math.max(0, start - warmupNeeded);

        for (int e = 0; e < numEMAs; e++) {
            int period = EMA_PERIODS[e];
            double multiplier = 2.0 / (period + 1.0);
            double ema = 0;
            boolean initialized = false;

            for (int i = dataStart; i < end; i++) {
                double close = data.getBar(i).getClose();
                if (!initialized) {
                    // Use SMA for initial value
                    if (i >= dataStart + period - 1) {
                        double smaSum = 0;
                        for (int j = i - period + 1; j <= i; j++) {
                            smaSum += data.getBar(j).getClose();
                        }
                        ema = smaSum / period;
                        initialized = true;
                    }
                } else {
                    ema = (close - ema) * multiplier + ema;
                }

                if (i >= start) {
                    emas[e][i - start] = ema;
                }
            }
        }

        // Draw the ribbon as filled bands between consecutive EMAs
        for (int e = 0; e < numEMAs - 1; e++) {
            Path2D.Double ribbonBand = new Path2D.Double();
            boolean first = true;

            // Upper edge (faster EMA)
            for (int i = 0; i < count; i++) {
                double x = model.barX(start + i, chartW);
                double y = model.priceToY(emas[e][i], chartH);
                if (first) { ribbonBand.moveTo(x, y); first = false; }
                else ribbonBand.lineTo(x, y);
            }

            // Lower edge (slower EMA, reversed)
            for (int i = count - 1; i >= 0; i--) {
                double x = model.barX(start + i, chartW);
                double y = model.priceToY(emas[e + 1][i], chartH);
                ribbonBand.lineTo(x, y);
            }
            ribbonBand.closePath();

            // Fill with gradient blend of the two EMA colors
            Color c1 = RIBBON_COLORS[e];
            Color c2 = RIBBON_COLORS[e + 1];
            Color fillColor = blendColors(c1, c2, 0.5);
            g.setColor(ChartColor.withAlpha(fillColor, 50 + e * 10));
            g.fill(ribbonBand);
        }

        // Draw individual EMA lines with glow
        for (int e = 0; e < numEMAs; e++) {
            Path2D.Double emaLine = new Path2D.Double();
            boolean first = true;

            for (int i = 0; i < count; i++) {
                double x = model.barX(start + i, chartW);
                double y = model.priceToY(emas[e][i], chartH);
                if (first) { emaLine.moveTo(x, y); first = false; }
                else emaLine.lineTo(x, y);
            }

            Color color = RIBBON_COLORS[e];
            float thickness = 1.0f + (numEMAs - 1 - e) * 0.15f; // fastest EMA is slightly thicker

            // Glow
            g.setStroke(new BasicStroke(thickness + 3.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(ChartColor.withAlpha(color, 25));
            g.draw(emaLine);

            // Main line
            g.setStroke(new BasicStroke(thickness, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(ChartColor.withAlpha(color, 200));
            g.draw(emaLine);
        }

        // Draw trend strength indicator at the right edge
        if (count > 0) {
            int lastIdx = count - 1;
            double fastest = emas[0][lastIdx];
            double slowest = emas[numEMAs - 1][lastIdx];
            double spread = Math.abs(fastest - slowest);

            // Normalize spread
            double avgPrice = (fastest + slowest) / 2.0;
            double normalizedSpread = avgPrice > 0 ? (spread / avgPrice) * 100.0 : 0;

            // Draw trend strength badge
            int badgeX = model.getLeftMargin() + chartW - 100;
            int badgeY = model.getTopMargin() + 8;
            String strength = normalizedSpread > 1.5 ? "STRONG" :
                    normalizedSpread > 0.5 ? "MODERATE" : "WEAK";
            Color strengthColor = normalizedSpread > 1.5 ? ChartColor.BULLISH :
                    normalizedSpread > 0.5 ? ChartColor.YELLOW : ChartColor.TEXT_DIM;

            g.setColor(ChartColor.withAlpha(strengthColor, 40));
            g.fillRect(badgeX, badgeY, 80, 18);
            g.setColor(strengthColor);
            g.setFont(smallFont);
            g.drawString("Trend: " + strength, badgeX + 4, badgeY + 13);
        }

        // Draw convergence/divergence markers
        for (int i = 1; i < count - 1; i++) {
            double prevSpread = Math.abs(emas[0][i - 1] - emas[numEMAs - 1][i - 1]);
            double currSpread = Math.abs(emas[0][i] - emas[numEMAs - 1][i]);
            double nextSpread = Math.abs(emas[0][i + 1] - emas[numEMAs - 1][i + 1]);

            // Detect squeeze (minimum spread point)
            if (currSpread < prevSpread && currSpread < nextSpread) {
                double x = model.barX(start + i, chartW);
                double y = model.priceToY(emas[numEMAs / 2][i], chartH);
                g.setColor(ChartColor.withAlpha(ChartColor.YELLOW, 100));
                g.fillOval((int) x - 3, (int) y - 3, 6, 6);
            }
        }
    }

    /**
     * Blend two colors with a given ratio.
     */
    private Color blendColors(Color c1, Color c2, double ratio) {
        int r = (int) (c1.getRed() + (c2.getRed() - c1.getRed()) * ratio);
        int g = (int) (c1.getGreen() + (c2.getGreen() - c1.getGreen()) * ratio);
        int b = (int) (c1.getBlue() + (c2.getBlue() - c1.getBlue()) * ratio);
        return new Color(r, g, b);
    }
}
