package com.jcharts.types;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartRenderer;
import com.jcharts.types.original.ElasticBandRenderer;
import com.jcharts.types.original.FlowForceRenderer;
import com.jcharts.types.original.MomentumWaveRenderer;
import com.jcharts.types.original.QuantumRibbonRenderer;
import com.jcharts.types.original.VolumeHeatmapRenderer;

/**
 * Enumeration of all available chart types with display names and factory methods.
 * Each enum value can create its corresponding renderer instance.
 * Standard chart types are listed first, followed by original/custom types.
 */
public enum ChartType {

    // Standard chart types
    CANDLESTICK("Candlestick", "Classic OHLC candlestick chart with filled bodies", CandlestickRenderer.class),
    LINE("Line", "Simple line chart connecting close prices", LineRenderer.class),
    BAR("OHLC Bar", "Traditional OHLC bar chart with open/close ticks", BarRenderer.class),
    AREA("Area", "Area chart filled below the close price line", AreaRenderer.class),
    HOLLOW_CANDLE("Hollow Candle", "Hollow green bullish, filled red bearish candles", HollowCandleRenderer.class),
    HEIKIN_ASHI("Heikin Ashi", "Smoothed candles using Heikin Ashi formula", HeikinAshiRenderer.class),
    RENKO("Renko", "Price-only bricks with fixed size based on ATR", RenkoRenderer.class),
    KAGI("Kagi", "Vertical/horizontal lines with yang/yin coloring", KagiRenderer.class),
    POINT_AND_FIGURE("Point & Figure", "Columns of X (up) and O (down) symbols", PointAndFigureRenderer.class),
    LINE_BREAK("Line Break", "Three Line Break with white up / black down boxes", LineBreakRenderer.class),
    EQUIVOLUME("Equivolume", "Box width proportional to volume, height = range", EquivolumeRenderer.class),
    PERCENTAGE("Percentage", "Percentage change from first visible bar", PercentageRenderer.class),

    // Original/custom chart types
    VOLUME_HEATMAP("Volume Heatmap", "Color-coded cells: intensity = volume * |%change|", VolumeHeatmapRenderer.class),
    MOMENTUM_WAVE("Momentum Wave", "Sine-wave overlay modulated by momentum", MomentumWaveRenderer.class),
    FLOW_FORCE("Flow Force", "Cumulative buy/sell flow force with gradient bars", FlowForceRenderer.class),
    ELASTIC_BAND("Elastic Band", "Price with elasticity-based volatility bands", ElasticBandRenderer.class),
    QUANTUM_RIBBON("Quantum Ribbon", "Multiple EMAs as a flowing gradient ribbon", QuantumRibbonRenderer.class);

    private final String displayName;
    private final String description;
    private final Class<? extends ChartRenderer> rendererClass;

    ChartType(String displayName, String description, Class<? extends ChartRenderer> rendererClass) {
        this.displayName = displayName;
        this.description = description;
        this.rendererClass = rendererClass;
    }

    /** Returns the human-readable display name for this chart type. */
    public String getDisplayName() {
        return displayName;
    }

    /** Returns a short description of this chart type. */
    public String getDescription() {
        return description;
    }

    /** Returns the renderer class associated with this chart type. */
    public Class<? extends ChartRenderer> getRendererClass() {
        return rendererClass;
    }

    /**
     * Creates a new renderer instance for this chart type.
     *
     * @param model the ChartModel to pass to the renderer constructor
     * @return a new ChartRenderer instance
     * @throws RuntimeException if the renderer cannot be instantiated
     */
    public ChartRenderer createRenderer(ChartModel model) {
        try {
            return rendererClass.getConstructor(ChartModel.class).newInstance(model);
        } catch (Exception e) {
            throw new RuntimeException("Failed to create renderer for chart type: " + name(), e);
        }
    }

    /**
     * Looks up a ChartType by its display name (case-insensitive).
     *
     * @param displayName the display name to search for
     * @return the matching ChartType, or CANDLESTICK as default if not found
     */
    public static ChartType fromDisplayName(String displayName) {
        if (displayName == null) return CANDLESTICK;
        for (ChartType type : values()) {
            if (type.displayName.equalsIgnoreCase(displayName)) {
                return type;
            }
        }
        return CANDLESTICK;
    }

    /**
     * Returns all standard (non-original) chart types.
     */
    public static ChartType[] getStandardTypes() {
        return new ChartType[]{
                CANDLESTICK, LINE, BAR, AREA, HOLLOW_CANDLE,
                HEIKIN_ASHI, RENKO, KAGI, POINT_AND_FIGURE,
                LINE_BREAK, EQUIVOLUME, PERCENTAGE
        };
    }

    /**
     * Returns all original/custom chart types.
     */
    public static ChartType[] getOriginalTypes() {
        return new ChartType[]{
                VOLUME_HEATMAP, MOMENTUM_WAVE, FLOW_FORCE,
                ELASTIC_BAND, QUANTUM_RIBBON
        };
    }

    @Override
    public String toString() {
        return displayName;
    }
}
