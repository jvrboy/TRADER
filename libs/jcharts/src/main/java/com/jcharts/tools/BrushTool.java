package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/** Freehand drawing tool that stores screen-coordinate points and draws a smooth path. */
public class BrushTool extends AbstractDrawingTool {
    private final List<int[]> points = new ArrayList<>();

    public BrushTool() { super("Brush"); }

    public void addPoint(int x, int y) { points.add(new int[]{x, y}); }
    public List<int[]> getPoints() { return points; }
    public void clear() { points.clear(); }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        if (points.size() < 2) return;
        g.setStroke(new BasicStroke(2f, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.setColor(selected ? ChartColor.YELLOW : color);
        g.drawPolyline(points.stream().mapToInt(p -> p[0]).toArray(),
                        points.stream().mapToInt(p -> p[1]).toArray(), points.size());
    }

    @Override public double[] getBounds() {
        if (points.isEmpty()) return new double[4];
        int minX = Integer.MAX_VALUE, maxX = Integer.MIN_VALUE, minY = Integer.MAX_VALUE, maxY = Integer.MIN_VALUE;
        for (int[] p : points) { minX = Math.min(minX, p[0]); maxX = Math.max(maxX, p[0]); minY = Math.min(minY, p[1]); maxY = Math.max(maxY, p[1]); }
        return new double[]{minX, minY, maxX, maxY};
    }
}