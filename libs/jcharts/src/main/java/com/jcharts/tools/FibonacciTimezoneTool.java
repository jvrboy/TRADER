package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Vertical lines at Fibonacci time intervals from a starting bar. */
public class FibonacciTimezoneTool extends AbstractDrawingTool {
    private static final double[] RATIOS = {1, 2, 3, 5, 8, 13, 21, 34};
    private final int startBar;

    public FibonacciTimezoneTool(int startBar) {
        super("Fibonacci Timezone");
        this.startBar = startBar;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        g.setStroke(getDashedStroke());
        Font f = new Font("SansSerif", Font.PLAIN, 9);
        g.setFont(f);
        for (double r : RATIOS) {
            int targetBar = startBar + (int) r;
            if (targetBar < model.getVisibleStart() || targetBar >= model.getVisibleEnd()) continue;
            double x = model.barX(targetBar, chartW);
            g.setColor(ChartColor.withAlpha(color, 80));
            g.drawLine((int) x, model.getTopMargin(), (int) x, model.getTopMargin() + chartH);
            g.setColor(color);
            g.drawString(String.format("F(%.0f)", r), (int) x + 3, model.getTopMargin() + 12);
        }
    }

    @Override public double[] getBounds() { return new double[]{startBar, 0, startBar + 34, Double.MAX_VALUE}; }
}