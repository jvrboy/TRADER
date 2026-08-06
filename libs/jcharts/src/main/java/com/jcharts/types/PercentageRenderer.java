package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;
import java.awt.geom.Path2D;

/**
 * Percentage change chart renderer.
 * Shows the percentage change of each bar's close relative to the first visible bar's close.
 * The baseline (0% line) is drawn at the center, with positive changes colored green
 * and negative changes colored red. A horizontal zero line is drawn for reference.
 */
public class PercentageRenderer extends ChartRenderer {

    public PercentageRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Percentage";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        if (end <= start) return;

        double baseClose = data.getBar(start).getClose();
        if (baseClose <= 0) return;

        int priceH = model.getPriceAreaHeight(chartH);

        // Calculate percentage range
        double minPct = Double.MAX_VALUE;
        double maxPct = Double.MIN_VALUE;
        double[] pcts = new double[end - start];
        for (int i = start; i < end; i++) {
            double pct = ((data.getBar(i).getClose() - baseClose) / baseClose) * 100.0;
            pcts[i - start] = pct;
            minPct = Math.min(minPct, pct);
            maxPct = Math.max(maxPct, pct);
        }

        // Add padding
        double pctRange = maxPct - minPct;
        if (pctRange < 0.01) pctRange = 1.0;
        minPct -= pctRange * 0.1;
        maxPct += pctRange * 0.1;

        // Map percentage to Y coordinate
        double zeroY = model.getTopMargin() + priceH * (maxPct / (maxPct - minPct));

        // Draw zero line
        g.setColor(ChartColor.GRID_LINE_LIGHT);
        g.setStroke(new BasicStroke(1.0f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL,
                10.0f, new float[]{5.0f, 3.0f}, 0.0f));
        g.drawLine(model.getLeftMargin(), (int) zeroY, model.getLeftMargin() + chartW, (int) zeroY);

        // Build the percentage line path
        Path2D.Double linePath = new Path2D.Double();
        Path2D.Double fillPath = new Path2D.Double();
        boolean first = true;

        for (int i = start; i < end; i++) {
            double x = model.barX(i, chartW);
            double pct = pcts[i - start];
            double y = model.getTopMargin() + priceH * ((maxPct - pct) / (maxPct - minPct));

            if (first) {
                linePath.moveTo(x, y);
                fillPath.moveTo(x, y);
                first = false;
            } else {
                linePath.lineTo(x, y);
                fillPath.lineTo(x, y);
            }
        }

        // Fill area below zero line red, above zero line green
        if (!first) {
            double lastX = model.barX(end - 1, chartW);
            double firstX = model.barX(start, chartW);

            // Fill positive area (above zero line)
            Path2D.Double posFill = new Path2D.Double();
            for (int i = start; i < end; i++) {
                double x = model.barX(i, chartW);
                double pct = pcts[i - start];
                double y = model.getTopMargin() + priceH * ((maxPct - pct) / (maxPct - minPct));
                if (i == start) posFill.moveTo(x, y);
                else posFill.lineTo(x, y);
            }
            posFill.lineTo(lastX, zeroY);
            posFill.lineTo(firstX, zeroY);
            posFill.closePath();

            // Fill negative area
            Path2D.Double negFill = new Path2D.Double();
            for (int i = start; i < end; i++) {
                double x = model.barX(i, chartW);
                double pct = pcts[i - start];
                double y = model.getTopMargin() + priceH * ((maxPct - pct) / (maxPct - minPct));
                if (i == start) negFill.moveTo(x, y);
                else negFill.lineTo(x, y);
            }
            negFill.lineTo(lastX, zeroY);
            negFill.lineTo(firstX, zeroY);
            negFill.closePath();

            // Determine dominant direction and fill accordingly
            boolean overallUp = pcts[pcts.length - 1] >= 0;

            // We draw both fills with transparency for a blended effect
            GradientPaint greenGrad = new GradientPaint(
                    0, (int) zeroY,
                    ChartColor.withAlpha(ChartColor.BULLISH, 50),
                    0, model.getTopMargin(),
                    ChartColor.withAlpha(ChartColor.BULLISH, 15)
            );
            GradientPaint redGrad = new GradientPaint(
                    0, (int) zeroY,
                    ChartColor.withAlpha(ChartColor.BEARISH, 50),
                    0, model.getTopMargin() + priceH,
                    ChartColor.withAlpha(ChartColor.BEARISH, 15)
            );

            g.setPaint(greenGrad);
            g.fill(posFill);
            g.setPaint(redGrad);
            g.fill(negFill);
        }

        // Draw line with glow
        g.setStroke(new BasicStroke(5.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.withAlpha(ChartColor.ACCENT, 25));
        g.draw(linePath);

        g.setStroke(new BasicStroke(2.0f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(ChartColor.ACCENT);
        g.draw(linePath);

        // Draw end-point dot
        double lastPct = pcts[pcts.length - 1];
        double lx = model.barX(end - 1, chartW);
        double ly = model.getTopMargin() + priceH * ((maxPct - lastPct) / (maxPct - minPct));
        g.setColor(lastPct >= 0 ? ChartColor.BULLISH : ChartColor.BEARISH);
        g.fillOval((int) lx - 4, (int) ly - 4, 8, 8);
        g.setColor(ChartColor.WHITE);
        g.fillOval((int) lx - 2, (int) ly - 2, 4, 4);

        // Draw percentage labels
        g.setFont(smallFont);
        g.setColor(ChartColor.TEXT_DIM);
        int labelStep = Math.max(1, (end - start) / 6);
        for (int i = start + labelStep; i < end; i += labelStep) {
            double x = model.barX(i, chartW);
            double pct = pcts[i - start];
            double y = model.getTopMargin() + priceH * ((maxPct - pct) / (maxPct - minPct));
            String label = String.format("%+.1f%%", pct);
            g.drawString(label, (int) x - 15, (int) y - 5);
        }
    }
}
