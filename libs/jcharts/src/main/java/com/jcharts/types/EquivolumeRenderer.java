package com.jcharts.types;

import com.jcharts.core.ChartColor;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.data.OHLCBar;

import java.awt.*;

/**
 * Equivolume chart renderer.
 * Each bar is drawn as a rectangle where the width is proportional to volume
 * and the height represents the high-low price range. This visualizes both
 * price movement and trading volume simultaneously. Color indicates bullish/bearish direction.
 */
public class EquivolumeRenderer extends ChartRenderer {

    private static final double MIN_WIDTH_RATIO = 0.3;
    private static final double MAX_WIDTH_RATIO = 2.5;

    public EquivolumeRenderer(ChartModel model) {
        super(model);
    }

    @Override
    public String getChartTypeName() {
        return "Equivolume";
    }

    @Override
    protected void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height) {
        com.jcharts.data.TimeSeries data = model.getData();
        if (data.isEmpty()) return;

        int start = model.getVisibleStart();
        int end = model.getVisibleEnd();

        // Calculate volume statistics for width mapping
        double minVol = Double.MAX_VALUE;
        double maxVol = Double.MIN_VALUE;
        for (int i = start; i < end; i++) {
            double vol = data.getBar(i).getVolume();
            minVol = Math.min(minVol, vol);
            maxVol = Math.max(maxVol, vol);
        }
        double volRange = maxVol - minVol;
        if (volRange <= 0) volRange = 1;

        double baseBarW = model.getBarWidth(chartW);
        double totalBaseWidth = baseBarW * (end - start);
        double totalPixels = chartW;

        // Map each bar to a box with volume-proportional width
        double x = model.getLeftMargin();
        double spacing = 1; // small gap between boxes

        for (int i = start; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            double volRatio = (bar.getVolume() - minVol) / volRange;
            double boxW = baseBarW * (MIN_WIDTH_RATIO + volRatio * (MAX_WIDTH_RATIO - MIN_WIDTH_RATIO));
            boxW = Math.max(2, boxW);

            double highY = model.priceToY(bar.getHigh(), chartH);
            double lowY = model.priceToY(bar.getLow(), chartH);
            int bodyTop = (int) highY;
            int bodyHeight = Math.max(1, (int) (lowY - highY));

            boolean bullish = bar.isBullish();
            Color color = bullish ? ChartColor.BULLISH : ChartColor.BEARISH;

            // Fill the box
            g.setColor(color);
            g.fillRect((int) x, bodyTop, (int) boxW, bodyHeight);

            // Draw border
            g.setColor(ChartColor.withAlpha(color, 160));
            g.drawRect((int) x, bodyTop, (int) boxW, bodyHeight);

            // Draw open and close markers inside the box
            double openY = model.priceToY(bar.getOpen(), chartH);
            double closeY = model.priceToY(bar.getClose(), chartH);
            g.setColor(ChartColor.withAlpha(ChartColor.WHITE, 120));
            int markerX1 = (int) x + 1;
            int markerX2 = (int) (x + boxW) - 1;
            int markerW = Math.max(1, (int) (boxW * 0.3));

            // Open marker (left side)
            g.fillRect(markerX1, (int) openY, markerW, 1);
            // Close marker (right side)
            g.fillRect(markerX2 - markerW, (int) closeY, markerW, 1);

            x += boxW + spacing;
        }
    }
}
