package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** A trend line defined by two bar-index/price points with optional extension. */
public class TrendLineTool extends AbstractDrawingTool {
    private final int startBarIdx;
    private final double startPrice;
    private final int endBarIdx;
    private final double endPrice;
    private final boolean extend;

    public TrendLineTool(int startBar, double startPrice, int endBar, double endPrice, boolean extend) {
        super("Trend Line");
        this.startBarIdx = startBar;
        this.startPrice = startPrice;
        this.endBarIdx = endBar;
        this.endPrice = endPrice;
        this.extend = extend;
    }

    public TrendLineTool(int sb, double sp, int eb, double ep) { this(sb, sp, eb, ep, true); }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        double x1 = model.barX(startBarIdx, chartW);
        double y1 = model.priceToY(startPrice, chartH);
        double x2, y2;
        if (extend) {
            double slope = (endPrice - startPrice) / (endBarIdx - startBarIdx);
            int extEnd = model.getVisibleEnd() + 20;
            int extStart = model.getVisibleStart() - 20;
            x1 = model.barX(extStart, chartW);
            y1 = model.priceToY(startPrice + slope * (extStart - startBarIdx), chartH);
            x2 = model.barX(extEnd, chartW);
            y2 = model.priceToY(startPrice + slope * (extEnd - startBarIdx), chartH);
        } else {
            x2 = model.barX(endBarIdx, chartW);
            y2 = model.priceToY(endPrice, chartH);
        }
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawLine((int) x1, (int) y1, (int) x2, (int) y2);
        if (selected) {
            drawHandle(g, (int) model.barX(startBarIdx, chartW), (int) model.priceToY(startPrice, chartH));
            drawHandle(g, (int) model.barX(endBarIdx, chartW), (int) model.priceToY(endPrice, chartH));
        }
        double pctChg = ((endPrice - startPrice) / startPrice) * 100;
        drawLabel(g, String.format("%.2f (%.1f%%)", endPrice - startPrice, pctChg), (int) Math.min(x1, x2) + 5, (int) Math.min(y1, y2) - 5);
    }

    private void drawHandle(Graphics2D g, int x, int y) {
        g.setColor(ChartColor.WHITE);
        g.fillRect(x - 4, y - 4, 8, 8);
        g.setColor(color);
        g.drawRect(x - 4, y - 4, 8, 8);
    }

    @Override public double[] getBounds() {
        return new double[]{Math.min(startBarIdx, endBarIdx), Math.min(startPrice, endPrice),
                Math.max(startBarIdx, endBarIdx), Math.max(startPrice, endPrice)};
    }

    public int getStartBarIdx() { return startBarIdx; }
    public double getStartPrice() { return startPrice; }
    public int getEndBarIdx() { return endBarIdx; }
    public double getEndPrice() { return endPrice; }
}