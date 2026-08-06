package com.jcharts.core;

import com.jcharts.data.TimeSeries;
import com.jcharts.indicators.SMAIndicator;
import com.jcharts.tools.HorizontalLineTool;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class ChartModelTest {
    private final TimeSeries data = TimeSeries.generateRandom(200, 100, 2, "TEST");

    @Test void testConstruction() {
        ChartModel m = new ChartModel(data);
        assertEquals(200, m.getData().size());
        assertTrue(m.isShowVolume());
    }

    @Test void testScrollZoom() {
        ChartModel m = new ChartModel(data);
        int orig = m.getVisibleStart();
        m.scrollLeft(5); assertTrue(m.getVisibleStart() <= orig);
        m.scrollRight(5);
        m.zoomIn(); assertTrue(m.getVisibleCount() <= 100);
        m.zoomOut(); assertTrue(m.getVisibleCount() >= 100);
    }

    @Test void testPriceMapping() {
        ChartModel m = new ChartModel(data);
        m.recalcRange();
        double y1 = m.priceToY(m.getMinPrice(), 400);
        double y2 = m.priceToY(m.getMaxPrice(), 400);
        assertTrue(y1 > y2);
    }

    @Test void testIndicatorsDrawings() {
        ChartModel m = new ChartModel(data);
        m.addIndicator(new SMAIndicator(20)); assertEquals(1, m.getIndicators().size());
        m.addDrawing(new HorizontalLineTool(105)); assertEquals(1, m.getDrawings().size());
        m.clearIndicators(); assertEquals(0, m.getIndicators().size());
        m.clearDrawings(); assertEquals(0, m.getDrawings().size());
    }

    @Test void testSettings() {
        ChartModel m = new ChartModel(data);
        m.setTitle("Test"); assertEquals("Test", m.getTitle());
        m.setSubtitle("Sub"); assertEquals("Sub", m.getSubtitle());
        m.setVolumeHeightRatio(0.3); assertEquals(0.3, m.getVolumeHeightRatio());
        m.setProperty("key", "val"); assertEquals("val", m.getProperty("key"));
    }

    @Test void testAutoManualScale() {
        ChartModel m = new ChartModel(data);
        m.setAutoScale(false);
        m.setManualMinPrice(0); m.setManualMaxPrice(200);
        assertEquals(0, m.getMinPrice()); assertEquals(200, m.getMaxPrice());
    }

    @Test void testVisibilityToggles() {
        ChartModel m = new ChartModel(data);
        m.setShowVolume(false); assertFalse(m.isShowVolume());
        m.setShowGrid(false); assertFalse(m.isShowGrid());
        m.setShowCrosshair(false); assertFalse(m.isShowCrosshair());
        m.setShowLegend(false); assertFalse(m.isShowLegend());
    }

    @Test void testMargins() {
        ChartModel m = new ChartModel(data);
        m.setRightMargin(100); assertEquals(100, m.getRightMargin());
        m.setBottomMargin(40); m.setTopMargin(50); m.setLeftMargin(20);
        assertEquals(40, m.getBottomMargin()); assertEquals(50, m.getTopMargin()); assertEquals(20, m.getLeftMargin());
    }

    @Test void testChartDimensions() {
        ChartModel m = new ChartModel(data);
        assertEquals(810, m.getChartWidth(900));
        int ch = m.getChartHeight(400);
        assertTrue(ch > 0);
    }

    @Test void testBarMapping() {
        ChartModel m = new ChartModel(data);
        double x = m.barX(50, 710);
        int idx = m.yToBarIndex(x, 710);
        assertEquals(50, idx);
    }

    @Test void testBarWidth() {
        ChartModel m = new ChartModel(data);
        assertTrue(m.getBarWidth(710) > 0);
    }
}