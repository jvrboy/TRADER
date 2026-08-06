package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** A filled rectangle defined by two corner points in bar-index/price space. */
public class RectangleTool extends AbstractDrawingTool {
    private final int bar1, bar2;
    private final double price1, price2;
    private final boolean filled;

    public RectangleTool(int bar1, double price1, int bar2, double price2) { this(bar1, price1, bar2, price2, true); }
    public RectangleTool(int bar1, double price1, int bar2, double price2, boolean filled) {
        super("Rectangle");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
        this.filled = filled;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(Math.min(bar1, bar2), chartW);
        double y1 = model.priceToY(Math.max(price1, price2), chartH);
        double x2 = model.barX(Math.max(bar1, bar2), chartW);
        double y2 = model.priceToY(Math.min(price1, price2), chartH);
        int w = (int)(x2 - x1); int h = (int)(y2 - y1);
        if (filled) {
            g.setColor(new Color(color.getRed(), color.getGreen(), color.getBlue(), 30));
            g.fillRect((int)x1, (int)y1, w, h);
        }
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawRect((int)x1, (int)y1, w, h);
        // Price labels
        double high = Math.max(price1, price2), low = Math.min(price1, price2);
        drawLabel(g, String.format("H: %.2f", high), (int)x1 + 3, (int)y1 + 12);
        drawLabel(g, String.format("L: %.2f", low), (int)x1 + 3, (int)y2 - 3);
        if (selected) { drawHandle(g,(int)x1,(int)y1); drawHandle(g,(int)x2,(int)y1); drawHandle(g,(int)x1,(int)y2); drawHandle(g,(int)x2,(int)y2); }
    }

    private void drawHandle(Graphics2D g, int x, int y) {
        g.setColor(ChartColor.WHITE); g.fillRect(x-4,y-4,8,8);
        g.setColor(color); g.drawRect(x-4,y-4,8,8);
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(bar1,bar2), Math.min(price1,price2), Math.max(bar1,bar2), Math.max(price1,price2)};
    }
    public int getBar1() { return bar1; } public double getPrice1() { return price1; }
    public int getBar2() { return bar2; } public double getPrice2() { return price2; }
}