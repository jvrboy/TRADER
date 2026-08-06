package com.jcharts.features;

import com.jcharts.core.ChartPanel;
import javax.swing.*;
import java.awt.*;
import java.util.ArrayList;
import java.util.List;

/** Manages multiple chart panels in a vertical or grid layout, sharing the same data but potentially different chart types and indicators. */
public class MultiPanelLayout {
    public enum LayoutMode { VERTICAL, GRID_2X1, GRID_2X2 }
    private final List<ChartPanel> panels = new ArrayList<>();
    private final JPanel container;
    private LayoutMode mode = LayoutMode.VERTICAL;
    private double[] splitRatios;

    public MultiPanelLayout() {
        container = new JPanel();
        container.setBackground(com.jcharts.core.ChartColor.BACKGROUND);
        container.setLayout(new BorderLayout());
    }

    public void addPanel(ChartPanel panel) { panels.add(panel); relayout(); }
    public void removePanel(ChartPanel panel) { panels.remove(panel); relayout(); }
    public List<ChartPanel> getPanels() { return panels; }
    public JPanel getContainer() { return container; }

    public void setLayoutMode(LayoutMode m) { this.mode = m; relayout(); }
    public void setSplitRatios(double[] r) { this.splitRatios = r; relayout(); }

    private void relayout() {
        container.removeAll();
        if (panels.isEmpty()) return;
        switch (mode) {
            case VERTICAL: {
                JPanel vp = new JPanel(new BorderLayout());
                vp.setBackground(com.jcharts.core.ChartColor.BACKGROUND);
                JSplitPane sp = null;
                for (int i = 0; i < panels.size(); i++) {
                    if (i == 0) { sp = new JSplitPane(JSplitPane.VERTICAL_SPLIT, panels.get(0),
                            i + 1 < panels.size() ? panels.get(1) : new JLabel(""));
                        sp.setResizeWeight(0.7); sp.setDividerSize(2); }
                    else if (i + 1 < panels.size()) {
                        JSplitPane next = new JSplitPane(JSplitPane.VERTICAL_SPLIT, sp, panels.get(i + 1));
                        next.setResizeWeight(0.7); next.setDividerSize(2); sp = next;
                    }
                }
                if (sp != null) vp.add(sp, BorderLayout.CENTER); else vp.add(panels.get(0), BorderLayout.CENTER);
                container.add(vp, BorderLayout.CENTER);
                break;
            }
            case GRID_2X1: {
                JPanel row = new JPanel(new GridLayout(1, 2, 2, 0));
                row.setBackground(com.jcharts.core.ChartColor.BACKGROUND);
                for (ChartPanel p : panels) row.add(p);
                container.add(row, BorderLayout.CENTER);
                break;
            }
            case GRID_2X2: {
                JPanel grid = new JPanel(new GridLayout(2, 2, 2, 2));
                grid.setBackground(com.jcharts.core.ChartColor.BACKGROUND);
                int count = Math.min(panels.size(), 4);
                for (int i = 0; i < count; i++) grid.add(panels.get(i));
                container.add(grid, BorderLayout.CENTER);
                break;
            }
        }
        container.revalidate();
    }

    public void syncScrolling(boolean sync) {
        if (!sync) return;
        for (ChartPanel p : panels) {
            p.addChangeListener(() -> {
                int start = p.getModel().getVisibleStart();
                int count = p.getModel().getVisibleCount();
                for (ChartPanel other : panels) {
                    if (other != p) other.getModel().setVisibleRange(start, count);
                    other.refresh();
                }
            });
        }
    }
}
