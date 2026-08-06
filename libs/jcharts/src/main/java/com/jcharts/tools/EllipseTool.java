package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** An ellipse inscribed in a rectangular region defined by two corner points. */
public class EllipseTool extends AbstractDrawingTool {
    private final int bar1, bar2;
    private final double price1, price2;

    public EllipseTool(int bar1, double price1, int bar2, double price2) {
        super("Ellipse");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(Math.min(bar1, bar2), chartW);
        double y1 = model.priceToY(Math.max(price1, price2), chartH);
        double x2 = model.barX(Math.max(bar1, bar2), chartW);
        double y2 = model.priceToY(Math.min(price1, price2), chartH);
        int w = (int)(x2 - x1); int h = (int)(y2 - y1);
        g.setColor(new Color(color.getRed(), color.getGreen(), color.getBlue(), 25));
        g.fillOval((int)x1, (int)y1, w, h);
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawOval((int)x1, (int)y1, w, h);
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(bar1,bar2), Math.min(price1,price2), Math.max(bar1,bar2), Math.max(price1,price2)};
    }
}