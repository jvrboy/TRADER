package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** An arrow drawn from one point to another with an arrowhead. */
public class ArrowTool extends AbstractDrawingTool {
    private final int fromBar, toBar;
    private final double fromPrice, toPrice;

    public ArrowTool(int fromBar, double fromPrice, int toBar, double toPrice) {
        super("Arrow");
        this.fromBar = fromBar; this.fromPrice = fromPrice;
        this.toBar = toBar; this.toPrice = toPrice;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(fromBar, chartW), y1 = model.priceToY(fromPrice, chartH);
        double x2 = model.barX(toBar, chartW), y2 = model.priceToY(toPrice, chartH);
        g.setStroke(new BasicStroke(2f));
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine((int)x1, (int)y1, (int)x2, (int)y2);
        // Arrowhead
        double angle = Math.atan2(y2 - y1, x2 - x1);
        int arrowSize = 10;
        int ax1 = (int)(x2 - arrowSize * Math.cos(angle - Math.PI / 6));
        int ay1 = (int)(y2 - arrowSize * Math.sin(angle - Math.PI / 6));
        int ax2 = (int)(x2 - arrowSize * Math.cos(angle + Math.PI / 6));
        int ay2 = (int)(y2 - arrowSize * Math.sin(angle + Math.PI / 6));
        g.fillPolygon(new int[]{(int)x2, ax1, ax2}, new int[]{(int)y2, ay1, ay2}, 3);
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(fromBar,toBar), Math.min(fromPrice,toPrice), Math.max(fromBar,toBar), Math.max(fromPrice,toPrice)};
    }
}