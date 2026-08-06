package com.jcharts.tools;

import java.awt.*;

/** Base class for drawing tools with common state management. */
public abstract class AbstractDrawingTool implements DrawingTool {
    protected Color color = new Color(77, 132, 240);
    protected boolean selected;
    protected float strokeWidth = 1.5f;
    protected String name;

    protected AbstractDrawingTool(String name) { this.name = name; }

    @Override public String getName() { return name; }
    @Override public Color getColor() { return color; }
    @Override public void setColor(Color c) { this.color = c; }
    @Override public boolean isSelected() { return selected; }
    @Override public void setSelected(boolean s) { this.selected = s; }
    @Override public boolean containsPoint(double x, double y) { return false; }
    @Override public double[] getBounds() { return new double[]{0,0,0,0}; }

    protected Stroke getStroke() {
        return selected
            ? new BasicStroke(strokeWidth + 1, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND, 0, new float[]{6, 3}, 0)
            : new BasicStroke(strokeWidth, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND);
    }

    protected Stroke getDashedStroke() {
        float[] dash = selected ? new float[]{8, 4} : new float[]{5, 5};
        return new BasicStroke(strokeWidth, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL, 0, dash, 0);
    }

    protected void drawLabel(Graphics2D g, String text, int x, int y) {
        g.setFont(new Font("SansSerif", Font.PLAIN, 10));
        FontMetrics fm = g.getFontMetrics();
        int tw = fm.stringWidth(text) + 8;
        int th = fm.getHeight() + 4;
        g.setColor(new Color(color.getRed(), color.getGreen(), color.getBlue(), 180));
        g.fillRect(x, y - th / 2, tw, th);
        g.setColor(Color.WHITE);
        g.drawString(text, x + 4, y + 4);
    }

    protected double clamp(double v, double min, double max) { return Math.max(min, Math.min(max, v)); }
}
