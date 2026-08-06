package com.jcharts.features;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import com.jcharts.indicators.Indicator;
import com.jcharts.tools.DrawingTool;
import com.jcharts.types.ChartType;
import java.util.ArrayList;
import java.util.List;

/** Saves and restores chart configurations (type, indicators, drawings, colors, settings). */
public class ChartTemplate {
    private String name;
    private ChartType chartType;
    private final List<Indicator> indicators = new ArrayList<>();
    private final List<DrawingTool> drawings = new ArrayList<>();
    private boolean showVolume = true;
    private boolean showGrid = true;
    private boolean showCrosshair = true;
    private boolean showLegend = true;
    private boolean autoScale = true;
    private double volumeHeightRatio = 0.2;

    public ChartTemplate(String name) { this.name = name; }

    public static ChartTemplate fromModel(ChartModel model, ChartType type) {
        ChartTemplate t = new ChartTemplate("Custom");
        t.chartType = type;
        t.indicators.addAll(model.getIndicators());
        t.drawings.addAll(model.getDrawings());
        t.showVolume = model.isShowVolume();
        t.showGrid = model.isShowGrid();
        t.showCrosshair = model.isShowCrosshair();
        t.showLegend = model.isShowLegend();
        t.autoScale = model.isAutoScale();
        t.volumeHeightRatio = model.getVolumeHeightRatio();
        return t;
    }

    public void applyTo(ChartModel model) {
        model.clearIndicators();
        model.clearDrawings();
        for (Indicator ind : indicators) model.addIndicator(ind);
        for (DrawingTool d : drawings) model.addDrawing(d);
        model.setShowVolume(showVolume);
        model.setShowGrid(showGrid);
        model.setShowCrosshair(showCrosshair);
        model.setShowLegend(showLegend);
        model.setAutoScale(autoScale);
        model.setVolumeHeightRatio(volumeHeightRatio);
    }

    public ChartType getChartType() { return chartType; }
    public void setChartType(ChartType t) { chartType = t; }
    public String getName() { return name; }
    public void setName(String n) { name = n; }
    public List<Indicator> getIndicators() { return indicators; }
    public List<DrawingTool> getDrawings() { return drawings; }
}
