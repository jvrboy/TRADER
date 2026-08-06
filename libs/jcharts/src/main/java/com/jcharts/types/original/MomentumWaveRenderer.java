package com.jcharts.types.original;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * ORIGINAL chart type: Momentum Wave renderer.
 * Overlays a sine-wave visualization where the wave amplitude represents
 * the magnitude of price momentum (rate of change), and the color shifts
 * between green (positive momentum / upward movement) and red (negative
 * momentum / downward movement). The wave oscillates around a central baseline
 * with the close price forming the underlying path.
 */
public class MomentumWaveRenderer extends ChartRenderer {

    private static final int MOMENTUM_PERIOD = 5;

    public MomentumWaveRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Momentum Wave";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);

        // Compute momentum values
        double[] momentum = computeMomentum(data, start, end);
        double maxMom = Double.MIN_VALUE;
        double minMom = Double.MIN_VALUE;
        for (int i = 0; i < momentum.length; i++) {
            maxMom = Math.max(maxMom, Math.abs(momentum[i]));
        }
        if (maxMom <= 0) maxMom = 1;

        // Draw the base close price line (subtle)
        Path2D.Double basePath = new Path2D.Double();
        boolean first = true;
        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double x = model.barX(i, chartW);
            double y = model.priceToY(bar.getClose(), chartH);
            if (first) { basePath.moveTo(x, y); first = false; }
            else basePath.lineTo(x, y);
        }
        g.setStroke(new BasicStroke(1.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.TEXT_DIM, 60));
        g.draw(basePath);

        // Draw momentum wave overlay
        // The wave oscillates as a sine pattern modulated by momentum amplitude
        double waveFrequency = 0.3; // cycles per bar
        double waveAmplitude = priceH * 0.04; // max wave displacement in pixels

        // Draw filled wave area
        Path2D.Double waveAreaGreen = new Path2D.Double();
        Path2D.Double waveAreaRed = new Path2D.Double();
        Path2D.Double waveLine = new Path2D.Double();
        boolean firstWave = true;

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double x = model.barX(i, chartW);
            double closeY = model.priceToY(bar.getClose(), chartH);

            // Compute wave displacement
            int dataIdx = i - start;
            double normalizedMom = momentum[dataIdx] / maxMom;
            double phase = i * waveFrequency;
            double waveDisp = Math.sin(phase) * normalizedMom * waveAmplitude;
            double waveY = closeY - waveDisp; // negative because Y is inverted

            if (firstWave) {
                waveLine.moveTo(x, waveY);
                firstWave = false;
            } else {
                waveLine.lineTo(x, waveY);
            }
        }

        // Draw the wave glow layers
        g.setStroke(new BasicStroke(8.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.ACCENT, 15));
        g.draw(waveLine);

        // Draw wave with per-segment coloring based on momentum direction
        for (int i = start; i < end - 1; i++) {
            int dataIdx = i - start;
            double mom = momentum[dataIdx];

            double x1 = model.barX(i, chartW);
            double x2 = model.barX(i + 1, chartW);
            double closeY1 = model.priceToY(data.getBar(i).getClose(), chartH);
            double closeY2 = model.priceToY(data.getBar(i + 1).getClose(), chartH);

            double normalizedMom1 = momentum[dataIdx] / maxMom;
            double normalizedMom2 = momentum[dataIdx + 1] / maxMom;
            double phase1 = i * waveFrequency;
            double phase2 = (i + 1) * waveFrequency;
            double wy1 = closeY1 - Math.sin(phase1) * normalizedMom1 * waveAmplitude;
            double wy2 = closeY2 - Math.sin(phase2) * normalizedMom2 * waveAmplitude;

            // Color based on momentum direction
            Color segColor;
            if (mom >= 0) {
                double intensity = Math.min(1.0, Math.abs(mom) / maxMom);
                segColor = ChartColor.lerp(
                        ChartColor.withAlpha(ChartColor.BULLISH, 80),
                        ChartColor.BULLISH,
                        intensity
                );
            } else {
                double intensity = Math.min(1.0, Math.abs(mom) / maxMom);
                segColor = ChartColor.lerp(
                        ChartColor.withAlpha(ChartColor.BEARISH, 80),
                        ChartColor.BEARISH,
                        intensity
                );
            }

            // Draw glow
            g.setStroke(new BasicStroke(6.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(ChartColor.withAlpha(segColor, 30));
            g.drawLine((int) x1, (int) wy1, (int) x2, (int) wy2);

            // Draw main wave segment
            g.setStroke(new BasicStroke(2.5f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
            g.setColor(segColor);
            g.drawLine((int) x1, (int) wy1, (int) x2, (int) wy2);
        }

        // Draw end-point marker
        if (end > start) {
            int lastIdx = end - 1 - start;
            double lx = model.barX(end - 1, chartW);
            double lCloseY = model.priceToY(data.getBar(end - 1).getClose(), chartH);
            double lNormMom = momentum[lastIdx] / maxMom;
            double lPhase = (end - 1) * waveFrequency;
            double lwy = lCloseY - Math.sin(lPhase) * lNormMom * waveAmplitude;

            Color dotColor = momentum[lastIdx] >= 0 ? ChartColor.BULLISH : ChartColor.BEARISH;
            g.setColor(dotColor);
            g.fillOval((int) lx - 5, (int) lwy - 5, 10, 10);
            g.setColor(ChartColor.WHITE);
            g.fillOval((int) lx - 2, (int) lwy - 2, 4, 4);
        }
    }

    /**
     * Compute momentum as the rate of price change over the given period.
     */
    private double[] computeMomentum(com.jcharts.data.TimeSeries data, int start, int end) {
        double[] momentum = new double[end - start];
        for (int i = start; i < end; i++) {
            if (i >= MOMENTUM_PERIOD) {
                OHLCBar current = data.getBar(i);
                OHLCBar prev = data.getBar(i - MOMENTUM_PERIOD);
                double pctChange = prev.getClose() > 0
                        ? ((current.getClose() - prev.getClose()) / prev.getClose())
                        : 0;
                momentum[i - start] = pctChange;
            } else {
                momentum[i - start] = 0;
            }
        }
        return momentum;
    }
}
