package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** A vertical line at a specific bar index. */
public class VerticalLineTool extends AbstractDrawingTool {
    private final int barIndex;
    private final String label;

    public VerticalLineTool(int barIndex) { this(barIndex, ""); }
    public VerticalLineTool(int barIndex, String label) {
        super("Vertical Line");
        this.barIndex = barIndex;
        this.label = label;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        if (barIndex < model.getVisibleStart() || barIndex >= model.getVisibleEnd()) return;
        double x = model.barX(barIndex, chartW);
        g.setStroke(getDashedStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine((int) x, model.getTopMargin(), (int) x, model.getTopMargin() + chartH);
        String lbl = label.isEmpty() ? String.valueOf(barIndex) : label;
        drawLabel(g, lbl, (int) x + 5, model.getTopMargin() + 15);
    }

    @Override public double[] getBounds() { return new double[]{barIndex, 0, barIndex, Double.MAX_VALUE}; }
    public int getBarIndex() { return barIndex; }
}