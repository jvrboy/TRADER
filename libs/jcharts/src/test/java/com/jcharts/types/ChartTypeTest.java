package com.jcharts.types;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.awt.image.BufferedImage;
import java.awt.Graphics2D;

/** Tests that every chart type can be instantiated, rendered without error, and has a valid name. */
class ChartTypeTest {
    private final TimeSeries data = TimeSeries.generateRandom(100, 100, 2, "TEST");
    private final BufferedImage img = new BufferedImage(800, 400, BufferedImage.TYPE_INT_ARGB);
    private final Graphics2D g2 = img.createGraphics();

    @Test void testAllTypesNotNull() {
        for (ChartType t : ChartType.values()) {
            assertNotNull(t.getDisplayName());
            assertNotNull(t.getDescription());
            assertNotNull(t.createRenderer(new ChartModel(data)));
            assertEquals(t, ChartType.fromDisplayName(t.getDisplayName()));
        }
    }

    @Test void testAllTypesRender() {
        for (ChartType t : ChartType.values()) {
            ChartModel model = new ChartModel(data);
            var renderer = t.createRenderer(model);
            assertNotNull(renderer.getChartTypeName());
            assertDoesNotThrow(() -> renderer.render(g2, 800, 400));
        }
    }

    @Test void testStandardTypes() { assertTrue(ChartType.getStandardTypes().length >= 6); }
    @Test void testOriginalTypes() { assertTrue(ChartType.getOriginalTypes().length >= 5); }
    @Test void testFromDisplayName() { assertEquals(ChartType.CANDLESTICK, ChartType.fromDisplayName("Candlestick")); }
    @Test void testFromDisplayNameNull() { assertNotNull(ChartType.fromDisplayName("Nonexistent")); }
}
