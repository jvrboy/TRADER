package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Fibonacci fan lines radiating from a point at standard angles. */
public class FibonacciFanTool extends AbstractDrawingTool {
    private static final double[] RATIOS = {0.382, 0.5, 0.618};
    private final int bar1, bar2;
    private final double price1, price2;

    public FibonacciFanTool(int bar1, double price1, int bar2, double price2) {
        super("Fibonacci Fan");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(bar1, chartW);
        double y1 = model.priceToY(price1, chartH);
        double x2 = model.barX(bar2, chartW);
        double y2 = model.priceToY(price2, chartH);
        double range = Math.abs(price2 - price1);
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine((int)x1, (int)y1, (int)x2, (int)y2);
        for (double r : RATIOS) {
            double targetPrice = price1 + (price2 > price1 ? -range * r : range * r);
            double yTarget = model.priceToY(targetPrice, chartH);
            g.setStroke(getDashedStroke());
            g.setColor(ChartColor.withAlpha(color, 100));
            g.drawLine((int)x1, (int)y1, model.getLeftMargin() + chartW, (int)yTarget);
        }
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(bar1,bar2), Math.min(price1,price2), Math.max(bar1,bar2), Math.max(price1,price2)};
    }
}