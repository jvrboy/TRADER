package com.jcharts.core;

import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;
import com.jcharts.indicators.Indicator;
import com.jcharts.tools.DrawingTool;

import java.util.*;
import java.util.List;

/**
 * Central data model for the chart. Holds the time series data, chart configuration,
 * active indicators, drawing tools, and viewport state. All chart components read from this model.
 */
public class ChartModel {

    private TimeSeries data;
    private int visibleStart = 0;
    private int visibleCount = 100;
    private double minPrice = Double.MAX_VALUE;
    private double maxPrice = Double.MIN_VALUE;
    private double minVolume = 0;
    private double maxVolume = Double.MIN_VALUE;
    private boolean showVolume = true;
    private boolean showGrid = true;
    private boolean showCrosshair = true;
    private boolean showLegend = true;
    private boolean showTimeAxis = true;
    private boolean showPriceAxis = true;
    private boolean autoScale = true;
    private double manualMinPrice = 0;
    private double manualMaxPrice = 100;
    private String title = "";
    private String subtitle = "";
    private final List<Indicator> indicators = new ArrayList<>();
    private final List<DrawingTool> drawings = new ArrayList<>();
    private final Map<String, Object> properties = new HashMap<>();
    private int rightMargin = 80;
    private int bottomMargin = 30;
    private int topMargin = 40;
    private int leftMargin = 10;
    private double volumeHeightRatio = 0.2;

    public ChartModel() {
        this.data = new TimeSeries();
    }

    public ChartModel(TimeSeries data) {
        setData(data);
    }

    public void setData(TimeSeries data) {
        this.data = data;
        if (!data.isEmpty()) {
            visibleStart = Math.max(0, data.size() - visibleCount);
            recalcRange();
        }
    }

    public TimeSeries getData() { return data; }

    public void recalcRange() {
        if (data.isEmpty()) return;
        int end = Math.min(visibleStart + visibleCount, data.size());
        minPrice = Double.MAX_VALUE;
        maxPrice = Double.MIN_VALUE;
        maxVolume = Double.MIN_VALUE;
        for (int i = visibleStart; i < end; i++) {
            OHLCBar bar = data.getBar(i);
            if (bar.getLow() < minPrice) minPrice = bar.getLow();
            if (bar.getHigh() > maxPrice) maxPrice = bar.getHigh();
            if (bar.getVolume() > maxVolume) maxVolume = bar.getVolume();
        }
        double padding = (maxPrice - minPrice) * 0.05;
        minPrice -= padding;
        maxPrice += padding;
    }

    public int getVisibleStart() { return visibleStart; }
    public int getVisibleCount() { return visibleCount; }
    public int getVisibleEnd() { return Math.min(visibleStart + visibleCount, data.size()); }
    public double getMinPrice() { return autoScale ? minPrice : manualMinPrice; }
    public double getMaxPrice() { return autoScale ? maxPrice : manualMaxPrice; }
    public double getMinVolume() { return minVolume; }
    public double getMaxVolume() { return maxVolume; }

    public void setVisibleRange(int start, int count) {
        this.visibleStart = Math.max(0, Math.min(start, data.size() - 1));
        this.visibleCount = Math.max(1, Math.min(count, data.size() - visibleStart));
        recalcRange();
    }

    public void scrollLeft(int bars) {
        setVisibleRange(visibleStart - bars, visibleCount);
    }

    public void scrollRight(int bars) {
        setVisibleRange(visibleStart + bars, visibleCount);
    }

    public void zoomIn() {
        int newCount = Math.max(10, visibleCount * 3 / 4);
        int newStart = visibleStart + (visibleCount - newCount) / 2;
        setVisibleRange(newStart, newCount);
    }

    public void zoomOut() {
        int newCount = Math.min(data.size(), visibleCount * 4 / 3);
        int newStart = visibleStart - (newCount - visibleCount) / 2;
        setVisibleRange(newStart, newCount);
    }

    public boolean isShowVolume() { return showVolume; }
    public void setShowVolume(boolean showVolume) { this.showVolume = showVolume; }
    public boolean isShowGrid() { return showGrid; }
    public void setShowGrid(boolean showGrid) { this.showGrid = showGrid; }
    public boolean isShowCrosshair() { return showCrosshair; }
    public void setShowCrosshair(boolean showCrosshair) { this.showCrosshair = showCrosshair; }
    public boolean isShowLegend() { return showLegend; }
    public void setShowLegend(boolean showLegend) { this.showLegend = showLegend; }
    public boolean isShowTimeAxis() { return showTimeAxis; }
    public void setShowTimeAxis(boolean showTimeAxis) { this.showTimeAxis = showTimeAxis; }
    public boolean isShowPriceAxis() { return showPriceAxis; }
    public void setShowPriceAxis(boolean showPriceAxis) { this.showPriceAxis = showPriceAxis; }
    public boolean isAutoScale() { return autoScale; }
    public void setAutoScale(boolean autoScale) { this.autoScale = autoScale; }
    public double getManualMinPrice() { return manualMinPrice; }
    public void setManualMinPrice(double v) { this.manualMinPrice = v; }
    public double getManualMaxPrice() { return manualMaxPrice; }
    public void setManualMaxPrice(double v) { this.manualMaxPrice = v; }
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    public String getSubtitle() { return subtitle; }
    public void setSubtitle(String subtitle) { this.subtitle = subtitle; }

    public List<Indicator> getIndicators() { return Collections.unmodifiableList(indicators); }
    public void addIndicator(Indicator indicator) { indicators.add(indicator); }
    public void removeIndicator(Indicator indicator) { indicators.remove(indicator); }
    public void clearIndicators() { indicators.clear(); }

    public List<DrawingTool> getDrawings() { return Collections.unmodifiableList(drawings); }
    public void addDrawing(DrawingTool tool) { drawings.add(tool); }
    public void removeDrawing(DrawingTool tool) { drawings.remove(tool); }
    public void clearDrawings() { drawings.clear(); }

    public int getRightMargin() { return rightMargin; }
    public void setRightMargin(int rightMargin) { this.rightMargin = rightMargin; }
    public int getBottomMargin() { return bottomMargin; }
    public void setBottomMargin(int bottomMargin) { this.bottomMargin = bottomMargin; }
    public int getTopMargin() { return topMargin; }
    public void setTopMargin(int topMargin) { this.topMargin = topMargin; }
    public int getLeftMargin() { return leftMargin; }
    public void setLeftMargin(int leftMargin) { this.leftMargin = leftMargin; }
    public double getVolumeHeightRatio() { return volumeHeightRatio; }
    public void setVolumeHeightRatio(double ratio) { this.volumeHeightRatio = Math.max(0, Math.min(0.5, ratio)); }

    public void setProperty(String key, Object value) { properties.put(key, value); }
    public Object getProperty(String key) { return properties.get(key); }

    public int getChartWidth(int totalWidth) { return totalWidth - leftMargin - rightMargin; }
    public int getChartHeight(int totalHeight) { return totalHeight - topMargin - bottomMargin; }
    public int getPriceAreaHeight(int chartHeight) { return (int) (chartHeight * (1.0 - volumeHeightRatio)); }
    public int getVolumeAreaHeight(int chartHeight) { return (int) (chartHeight * volumeHeightRatio); }

    public double barX(int barIndex, int chartWidth) {
        return leftMargin + (barIndex - visibleStart) * ((double) chartWidth / visibleCount);
    }

    public double priceToY(double price, int chartHeight) {
        double range = getMaxPrice() - getMinPrice();
        if (range == 0) return topMargin + getPriceAreaHeight(chartHeight) / 2.0;
        int priceH = getPriceAreaHeight(chartHeight);
        return topMargin + priceH - ((price - getMinPrice()) / range) * priceH;
    }

    public double yToPrice(double y, int chartHeight) {
        int priceH = getPriceAreaHeight(chartHeight);
        double ratio = (topMargin + priceH - y) / priceH;
        return getMinPrice() + ratio * (getMaxPrice() - getMinPrice());
    }

    public int yToBarIndex(double x, int chartWidth) {
        int idx = (int) ((x - leftMargin) / ((double) chartWidth / visibleCount)) + visibleStart;
        return Math.max(0, Math.min(idx, data.size() - 1));
    }

    public double getBarWidth(int chartWidth) {
        return (double) chartWidth / visibleCount * 0.7;
    }
}
