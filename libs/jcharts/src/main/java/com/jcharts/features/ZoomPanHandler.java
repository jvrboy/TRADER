package com.jcharts.features;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartPanel;
import java.awt.event.*;

/** Manages zoom and pan behavior with configurable sensitivity and animation. */
public class ZoomPanHandler {
    private final ChartPanel panel;
    private final ChartModel model;
    private double zoomSensitivity = 1.15;
    private int panSensitivity = 5;
    private boolean smoothScrolling = true;
    private javax.swing.Timer scrollTimer;
    private int scrollDirection = 0;

    public ZoomPanHandler(ChartPanel panel) {
        this.panel = panel;
        this.model = panel.getModel();
        setupAutoRepeat();
    }

    private void setupAutoRepeat() {
        scrollTimer = new javax.swing.Timer(50, e -> {
            if (scrollDirection != 0) {
                model.scrollRight(scrollDirection * panSensitivity / 2);
                panel.refresh();
            }
        });
        scrollTimer.setInitialDelay(300);
    }

    public void startScroll(int dir) { scrollDirection = dir; scrollTimer.start(); }
    public void stopScroll() { scrollDirection = 0; scrollTimer.stop(); }

    public void zoomCenter(double factor) {
        int oldCount = model.getVisibleCount();
        int newCount = (int) (oldCount / factor);
        newCount = Math.max(10, Math.min(model.getData().size(), newCount));
        int diff = oldCount - newCount;
        model.setVisibleRange(model.getVisibleStart() + diff / 2, newCount);
        panel.refresh();
    }

    public void fitAll() {
        int n = model.getData().size();
        model.setVisibleRange(0, n);
        panel.refresh();
    }

    public void goToBar(int barIndex) {
        int half = model.getVisibleCount() / 2;
        model.setVisibleRange(barIndex - half, model.getVisibleCount());
        panel.refresh();
    }

    public double getZoomSensitivity() { return zoomSensitivity; }
    public void setZoomSensitivity(double s) { this.zoomSensitivity = s; }
    public int getPanSensitivity() { return panSensitivity; }
    public void setPanSensitivity(int s) { this.panSensitivity = s; }
    public boolean isSmoothScrolling() { return smoothScrolling; }
    public void setSmoothScrolling(boolean s) { this.smoothScrolling = s; }
}
