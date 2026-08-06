package com.jcharts.types.original;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;

/**
 * ORIGINAL chart type: Volume Heatmap renderer.
 * Draws color-coded rectangular cells where the color intensity represents
 * the combined activity metric: volume * |percentage change|.
 * Blue = low activity, Yellow = medium activity, Red = high activity.
 * Each cell corresponds to one bar, creating a heatmap-style visualization
 * of trading intensity across time.
 */
public class VolumeHeatmapRenderer extends ChartRenderer {

    public VolumeHeatmapRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Volume Heatmap";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();
        int priceH = model.getPriceAreaHeight(chartH);

        // Compute activity metrics for normalization
        double maxActivity = Double.MIN_VALUE;
        double minActivity = Double.MAX_VALUE;
        double[] activities = new double[end - start];

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double pctChange = bar.getOpen() > 0
                    ? Math.abs((bar.getClose() - bar.getOpen()) / bar.getOpen())
                    : 0;
            double activity = bar.getVolume() * pctChange;
            activities[i - start] = activity;
            maxActivity = Math.max(maxActivity, activity);
            minActivity = Math.min(minActivity, activity);
        }

        double activityRange = maxActivity - minActivity;
        if (activityRange <= 0) activityRange = 1;

        double barW = model.getBarWidth(chartW);
        int cellW = Math.max(2, (int) (barW * 0.95));

        // Compute total rows for the heatmap grid
        int rows = 20;
        int cellH = Math.max(2, priceH / rows);

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double x = model.barX(i, chartW);

            // Normalize activity to 0..1
            double normalized = (activities[i - start] - minActivity) / activityRange;

            // Map activity to color: blue -> yellow -> red
            Color heatColor = activityToColor(normalized);

            // Draw the full-height cell with the activity color
            int bx = (int) (x - cellW / 2);

            // Draw a subtle grid of cells with slight intensity variation by price level
            double highY = model.priceToY(bar.getHigh(), chartH);
            double lowY = model.priceToY(bar.getLow(), chartH);

            for (int row = 0; row < rows; row++) {
                int cy = model.getTopMargin() + row * cellH;

                // Check if this row is within the bar's price range
                boolean inRange = cy >= lowY && cy <= highY;
                double intensity = inRange ? normalized : normalized * 0.15;

                Color cellColor = activityToColor(Math.min(1.0, intensity));
                g.setColor(cellColor);
                g.fillRect(bx, cy, cellW, cellH - 1);

                // Subtle border
                g.setColor(ChartColor.withAlpha(ChartColor.BLACK, 40));
                g.drawRect(bx, cy, cellW, cellH - 1);
            }

            // Draw the close price marker
            double closeY = model.priceToY(bar.getClose(), chartH);
            g.setColor(ChartColor.WHITE);
            g.fillRect(bx - 1, (int) closeY - 1, cellW + 2, 3);
        }

        // Draw activity legend
        drawHeatmapLegend(g, chartW, chartH);
    }

    /**
     * Maps normalized activity (0..1) to a color gradient: blue -> yellow -> red.
     */
    private Color activityToColor(double t) {
        t = Math.max(0, Math.min(1, t));
        if (t < 0.5) {
            // Blue to Yellow
            double ratio = t * 2.0;
            int r = (int) (49 + (242 - 49) * ratio);
            int g2 = (int) (211 + (185 - 211) * ratio);
            int b = (int) (242 + (49 - 242) * ratio);
            return new Color(r, g2, b);
        } else {
            // Yellow to Red
            double ratio = (t - 0.5) * 2.0;
            int r = (int) (242 + (234 - 242) * ratio);
            int g2 = (int) (185 + (57 - 185) * ratio);
            int b = (int) (49 + (67 - 49) * ratio);
            return new Color(r, g2, b);
        }
    }

    /**
     * Draw a color legend in the top-right corner.
     */
    private void drawHeatmapLegend(Graphics2D g, int chartW, int chartH) {
        int legendX = model.getLeftMargin() + chartW - 120;
        int legendY = model.getTopMargin() + 5;
        int legendW = 100;
        int legendH = 12;

        // Draw gradient bar
        for (int px = 0; px < legendW; px++) {
            double t = (double) px / legendW;
            Color c = activityToColor(t);
            g.setColor(c);
            g.drawLine(legendX + px, legendY, legendX + px, legendY + legendH);
        }

        // Border
        g.setColor(ChartColor.TEXT_DIM);
        g.drawRect(legendX, legendY, legendW, legendH);

        // Labels
        g.setFont(smallFont);
        g.setColor(ChartColor.TEXT_DIM);
        g.drawString("Low", legendX, legendY + legendH + 12);
        g.drawString("Med", legendX + legendW / 2 - 10, legendY + legendH + 12);
        g.drawString("High", legendX + legendW - 22, legendY + legendH + 12);
    }
}
