package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Fibonacci retracement levels between two price points. */
public class FibonacciRetracementTool extends AbstractDrawingTool {
    private static final double[] LEVELS = {0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0};
    private static final String[] LABELS = {"0%", "23.6%", "38.2%", "50%", "61.8%", "78.6%", "100%"};
    private final int bar1, bar2;
    private final double price1, price2;

    public FibonacciRetracementTool(int bar1, double price1, int bar2, double price2) {
        super("Fibonacci Retracement");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(Math.min(bar1, bar2), chartW);
        double x2 = model.barX(Math.max(bar1, bar2), chartW);
        double highP = Math.max(price1, price2), lowP = Math.min(price1, price2);
        double range = highP - lowP;
        // Trend line
        g.setStroke(getStroke());
        g.setColor(ChartColor.withAlpha(color, 150));
        g.drawLine((int)x1, (int)model.priceToY(highP, chartH), (int)x2, (int)model.priceToY(lowP, chartH));
        // Levels
        Font f = new Font("SansSerif", Font.PLAIN, 10);
        g.setFont(f);
        for (int i = 0; i < LEVELS.length; i++) {
            double levelPrice = highP - range * LEVELS[i];
            double y = model.priceToY(levelPrice, chartH);
            g.setStroke(getDashedStroke());
            g.setColor(ChartColor.withAlpha(color, 120));
            g.drawLine((int)x1, (int)y, (int)x2, (int)y);
            drawLabel(g, String.format("%s (%.2f)", LABELS[i], levelPrice), (int)x2 + 3, (int)y);
        }
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(bar1,bar2), Math.min(price1,price2), Math.max(bar1,bar2), Math.max(price1,price2)};
    }
    public double[] getLevels() { return LEVELS.clone(); }
}