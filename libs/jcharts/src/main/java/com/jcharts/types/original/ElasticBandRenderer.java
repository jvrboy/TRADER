package com.jcharts.types.original;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * ORIGINAL chart type: Elastic Band renderer.
 * Draws the close price line surrounded by "elastic bands" that stretch and
 * compress based on volatility. Similar to Bollinger Bands but uses an elasticity
 * physics model: the bands respond to volatility changes with spring-like behavior,
 * where high volatility stretches the bands wider and low volatility compresses them.
 * The bands are filled with a gradient to create a visually appealing elastic effect.
 */
public class ElasticBandRenderer extends ChartRenderer {

    private static final int BAND_LOOKBACK = 20;
    private static final double SPRING_CONSTANT = 0.15;
    private static final double DAMPING = 0.85;

    public ElasticBandRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Elastic Band";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);
        int count = end - start;

        // Compute rolling volatility (standard deviation of returns)
        double[] volatility = new double[count];
        double[] returns = new double[count];

        for (int i = 0; i < count; i++) {
            int idx = start + i;
            if (idx > 0) {
                OHLCBar prev = data.getBar(idx - 1);
                OHLCBar curr = data.getBar(idx);
                returns[i] = prev.getClose() > 0
                        ? (curr.getClose() - prev.getClose()) / prev.getClose()
                        : 0;
            } else {
                returns[i] = 0;
            }
        }

        // Rolling volatility with elastic smoothing
        double currentBandWidth = 0;
        for (int i = 0; i < count; i++) {
            // Compute local volatility
            int lookback = Math.min(BAND_LOOKBACK, i + 1);
            double sum = 0;
            double sumSq = 0;
            for (int j = i - lookback + 1; j <= i; j++) {
                if (j >= 0 && j < returns.length) {
                    sum += returns[j];
                    sumSq += returns[j] * returns[j];
                }
            }
            double mean = sum / lookback;
            double variance = (sumSq / lookback) - (mean * mean);
            double localVol = Math.sqrt(Math.max(0, variance));

            // Apply spring physics: target width is proportional to volatility
            // Spring force pulls current width toward target
            double avgRange = getAvgPriceRange(data, start, end);
            double targetWidth = localVol * avgRange * SPRING_CONSTANT;
            double springForce = (targetWidth - currentBandWidth) * SPRING_CONSTANT;
            currentBandWidth = currentBandWidth * DAMPING + springForce + targetWidth * (1 - DAMPING);
            currentBandWidth = Math.max(avgRange * 0.002, currentBandWidth);

            volatility[i] = currentBandWidth;
        }

        // Build band paths
        Path2D.Double upperBand = new Path2D.Double();
        Path2D.Double lowerBand = new Path2D.Double();
        Path2D.Double closeLine = new Path2D.Double();
        Path2D.Double fillArea = new Path2D.Double();
        boolean first = true;

        for (int i = 0; i < count; i++) {
            OHLCBar bar = data.getBar(start + i);
            double x = model.barX(start + i, chartW);
            double closeY = model.priceToY(bar.getClose(), chartH);
            double upperY = model.priceToY(bar.getClose() + volatility[i], chartH);
            double lowerY = model.priceToY(bar.getClose() - volatility[i], chartH);

            if (first) {
                upperBand.moveTo(x, upperY);
                lowerBand.moveTo(x, lowerY);
                closeLine.moveTo(x, closeY);
                fillArea.moveTo(x, upperY);
                first = false;
            } else {
                upperBand.lineTo(x, upperY);
                lowerBand.lineTo(x, lowerY);
                closeLine.lineTo(x, closeY);
                fillArea.lineTo(x, upperY);
            }
        }

        // Close fill area
        if (count > 0) {
            for (int i = count - 1; i >= 0; i--) {
                OHLCBar bar = data.getBar(start + i);
                double x = model.barX(start + i, chartW);
                double lowerY = model.priceToY(bar.getClose() - volatility[i], chartH);
                fillArea.lineTo(x, lowerY);
            }
            fillArea.closePath();
        }

        // Draw filled band area with gradient
        GradientPaint bandGradient = new GradientPaint(
                0, model.getTopMargin(),
                ChartColor.withAlpha(ChartColor.CYAN, 30),
                0, model.getTopMargin() + priceH,
                ChartColor.withAlpha(ChartColor.CYAN, 10)
        );
        g.setPaint(bandGradient);
        g.fill(fillArea);

        // Draw upper band
        g.setStroke(new BasicStroke(1.5f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.CYAN);
        g.draw(upperBand);

        // Draw lower band
        g.draw(lowerBand);

        // Draw inner band (50% width) with different color
        Path2D.Double innerUpper = new Path2D.Double();
        Path2D.Double innerLower = new Path2D.Double();
        first = true;
        for (int i = 0; i < count; i++) {
            OHLCBar bar = data.getBar(start + i);
            double x = model.barX(start + i, chartW);
            double innerWidth = volatility[i] * 0.5;
            double upperY = model.priceToY(bar.getClose() + innerWidth, chartH);
            double lowerY = model.priceToY(bar.getClose() - innerWidth, chartH);
            if (first) {
                innerUpper.moveTo(x, upperY);
                innerLower.moveTo(x, lowerY);
                first = false;
            } else {
                innerUpper.lineTo(x, upperY);
                innerLower.lineTo(x, lowerY);
            }
        }
        g.setStroke(new BasicStroke(1.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.CYAN, 120));
        g.draw(innerUpper);
        g.draw(innerLower);

        // Draw close price line on top
        g.setStroke(new BasicStroke(2.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.WHITE);
        g.draw(closeLine);

        // Glow effect on close line
        g.setStroke(new BasicStroke(5.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.WHITE, 20));
        g.draw(closeLine);

        // Draw dots at extreme volatility points
        for (int i = 1; i < count - 1; i++) {
            if (volatility[i] > volatility[i - 1] && volatility[i] > volatility[i + 1]) {
                // Local volatility peak
                OHLCBar bar = data.getBar(start + i);
                double x = model.barX(start + i, chartW);
                double upperY = model.priceToY(bar.getClose() + volatility[i], chartH);
                double lowerY = model.priceToY(bar.getClose() - volatility[i], chartH);

                g.setColor(ChartColor.withAlpha(ChartColor.YELLOW, 150));
                g.fillOval((int) x - 3, (int) upperY - 3, 6, 6);
                g.fillOval((int) x - 3, (int) lowerY - 3, 6, 6);
            }
        }
    }

    /**
     * Calculate the average price range for band scaling.
     */
    private double getAvgPriceRange(com.jcharts.data.TimeSeries data, int start, int end) {
        double sum = 0;
        int count = 0;
        for (int i = start; i < end; i++) {
            sum += data.getBar(i).getRange();
            count++;
        }
        return count > 0 ? sum / count : 1;
    }
}
