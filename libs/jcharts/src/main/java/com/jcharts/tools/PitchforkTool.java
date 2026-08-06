package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Andrew's Pitchfork: median line and two outer parallel lines from 3 anchor points. */
public class PitchforkTool extends AbstractDrawingTool {
    private final int bar1, bar2, bar3;
    private final double price1, price2, price3;

    public PitchforkTool(int bar1, double price1, int bar2, double price2, int bar3, double price3) {
        super("Pitchfork");
        this.bar1 = bar1; this.price1 = price1;
        this.bar2 = bar2; this.price2 = price2;
        this.bar3 = bar3; this.price3 = price3;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        // Median line through midpoint of P2-P3, parallel to P1 direction
        double midBar = (bar2 + bar3) / 2.0;
        double midPrice = (price2 + price3) / 2.0;
        double slope = (price1 - midPrice) / (bar1 - midBar);
        int vs = model.getVisibleStart() - 20, ve = model.getVisibleEnd() + 20;
        // Median line
        double yma = model.priceToY(midPrice + slope * (vs - midBar), chartH);
        double ymb = model.priceToY(midPrice + slope * (ve - midBar), chartH);
        g.setStroke(getStroke());
        g.setColor(selected ? ChartColor.YELLOW : color);
        int xL = (int) model.barX(vs, chartW);
        int xR = (int) model.barX(ve, chartW);
        g.drawLine(xL, (int)yma, xR, (int)ymb);
        // Upper line (through P2)
        double offU = price2 - (midPrice + slope * (bar2 - midBar));
        double yua = model.priceToY(midPrice + offU + slope * (vs - midBar), chartH);
        double yub = model.priceToY(midPrice + offU + slope * (ve - midBar), chartH);
        g.setStroke(getDashedStroke());
        g.setColor(ChartColor.withAlpha(color, 120));
        g.drawLine(xL, (int)yua, xR, (int)yub);
        // Lower line (through P3)
        double offL = price3 - (midPrice + slope * (bar3 - midBar));
        double yla = model.priceToY(midPrice + offL + slope * (vs - midBar), chartH);
        double ylb = model.priceToY(midPrice + offL + slope * (ve - midBar), chartH);
        g.drawLine(xL, (int)yla, xR, (int)ylb);
        // Anchor points
        for (int[] p : new int[][]{
            {(int)model.barX(bar1,chartW), (int)model.priceToY(price1,chartH)},
            {(int)model.barX(bar2,chartW), (int)model.priceToY(price2,chartH)},
            {(int)model.barX(bar3,chartW), (int)model.priceToY(price3,chartH)}
        }) {
            g.setColor(color); g.fillOval(p[0]-4,p[1]-4,8,8);
        }
    }

    @Override public double[] getBounds() {
        double minP=Math.min(Math.min(price1,price2),price3), maxP=Math.max(Math.max(price1,price2),price3);
        return new double[]{Math.min(Math.min(bar1,bar2),bar3), minP, Math.max(Math.max(bar1,bar2),bar3), maxP};
    }
}