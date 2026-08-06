package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** A parallel channel defined by two trend lines (3 points: start, end of line 1, offset point for line 2). */
public class ChannelTool extends AbstractDrawingTool {
    private final int bar1, bar2, bar3;
    private final double price1, price2, price3;

    public ChannelTool(int bar1, double price1, int bar2, double price2, int bar3, double price3) {
        super("Channel");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
        this.bar3 = bar3; this.price3 = price3;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double slope = (price2 - price1) / (bar2 - bar1);
        double offset = price3 - (price1 + slope * (bar3 - bar1));
        int vs = model.getVisibleStart() - 10, ve = model.getVisibleEnd() + 10;
        // Line 1
        double y1a = model.priceToY(price1 + slope * (vs - bar1), chartH);
        double y1b = model.priceToY(price1 + slope * (ve - bar1), chartH);
        // Line 2 (parallel)
        double y2a = model.priceToY(price1 + offset + slope * (vs - bar1), chartH);
        double y2b = model.priceToY(price1 + offset + slope * (ve - bar1), chartH);
        int xLeft = (int) model.barX(vs, chartW);
        int xRight = (int) model.barX(ve, chartW);
        // Fill channel
        g.setColor(new Color(color.getRed(), color.getGreen(), color.getBlue(), 20));
        g.fillPolygon(new int[]{xLeft, xRight, xRight, xLeft},
                      new int[]{(int)y1a, (int)y1b, (int)y2b, (int)y2a}, 4);
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine(xLeft, (int)y1a, xRight, (int)y1b);
        g.drawLine(xLeft, (int)y2a, xRight, (int)y2b);
    }

    @Override public double[] getBounds() {
        double minP = Math.min(Math.min(price1, price2), price3);
        double maxP = Math.max(Math.max(price1, price2), price3);
        return new double[]{Math.min(Math.min(bar1,bar2),bar3), minP, Math.max(Math.max(bar1,bar2),bar3), maxP};
    }
}