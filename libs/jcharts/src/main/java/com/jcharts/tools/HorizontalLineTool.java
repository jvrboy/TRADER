package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** A horizontal price level line spanning the full visible chart. */
public class HorizontalLineTool extends AbstractDrawingTool {
    private final double price;
    private final String label;

    public HorizontalLineTool(double price) { this(price, String.format("%.2f", price)); }
    public HorizontalLineTool(double price, String label) {
        super("Horizontal Line");
        this.price = price;
        this.label = label;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double y = model.priceToY(price, chartH);
        g.setStroke(getDashedStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine(model.getLeftMargin(), (int) y, model.getLeftMargin() + chartW, (int) y);
        drawLabel(g, label, model.getLeftMargin() + chartW + 2, (int) y);
        if (selected) {
            g.setColor(ChartColor.WHITE);
            g.fillOval(model.getLeftMargin() + chartW / 2 - 4, (int) y - 4, 8, 8);
            g.setColor(color);
            g.drawOval(model.getLeftMargin() + chartW / 2 - 4, (int) y - 4, 8, 8);
        }
    }

    @Override public boolean containsPoint(double x, double y) {
        return Math.abs(y - price) < 0.5;
    }

    @Override public double[] getBounds() { return new double[]{0, price, Integer.MAX_VALUE, price}; }
    public double getPrice() { return price; }
}