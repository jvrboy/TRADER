package com.jcharts.tools;

import java.awt.*;

/**
 * Interface for all drawing tools that can be overlaid on a chart.
 * Tools store geometry in data coordinates and render via the ChartModel coordinate mapping.
 */
public interface DrawingTool {
    void draw(java.awt.Graphics2D g, com.jcharts.core.ChartModel model, int chartW, int chartH);
    String getName();
    boolean containsPoint(double x, double y);
    void setColor(Color c);
    Color getColor();
    void setSelected(boolean s);
    boolean isSelected();
    double[] getBounds();
}
