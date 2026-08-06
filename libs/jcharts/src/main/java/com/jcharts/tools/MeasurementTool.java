package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Measures price distance and percentage change between two points. */
public class MeasurementTool extends AbstractDrawingTool {
    private final int bar1, bar2;
    private final double price1, price2;

    public MeasurementTool(int bar1, double price1, int bar2, double price2) {
        super("Measurement");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(bar1, chartW), y1 = model.priceToY(price1, chartH);
        double x2 = model.barX(bar2, chartW), y2 = model.priceToY(price2, chartH);
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine((int)x1, (int)y1, (int)x2, (int)y1); // horizontal
        g.drawLine((int)x2, (int)y1, (int)x2, (int)y2); // vertical
        g.drawLine((int)x1, (int)y1, (int)x2, (int)y2); // diagonal
        double dist = Math.abs(price2 - price1);
        double pct = (dist / price1) * 100;
        int bars = Math.abs(bar2 - bar1);
        String info = String.format("Dist: %.2f (%.2f%%) Bars: %d", dist, pct, bars);
        drawLabel(g, info, (int) Math.min(x1, x2), (int) Math.min(y1, y2) - 5);
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(bar1,bar2), Math.min(price1,price2), Math.max(bar1,bar2), Math.max(price1,price2)};
    }
}