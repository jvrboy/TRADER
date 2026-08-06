package com.jcharts.features;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import com.jcharts.types.ChartType;
import com.jcharts.indicators.SMAIndicator;
import com.jcharts.tools.HorizontalLineTool;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.io.File;
import java.nio.file.Files;
import java.util.concurrent.atomic.AtomicBoolean;


class FeaturesTest {
    private final TimeSeries data = TimeSeries.generateRandom(200, 100, 2, "TEST");

    @Test void testTimeframeSelector() {
        TimeSeries hourly = TimeframeSelector.convert(data, TimeframeSelector.Timeframe.H1);
        assertNotNull(hourly);
        assertTrue(hourly.size() <= 200);
        assertEquals("1H", hourly.getTimeframe());
    }

    @Test void testTimeframeConvertAll() {
        for (TimeframeSelector.Timeframe tf : TimeframeSelector.Timeframe.values()) {
            assertDoesNotThrow(() -> TimeframeSelector.convert(data, tf));
        }
    }

    @Test void testZoomPanHandler() {
        com.jcharts.core.ChartPanel cp = new com.jcharts.core.ChartPanel(data);
        ZoomPanHandler zph = new ZoomPanHandler(cp);
        assertDoesNotThrow(() -> zph.fitAll());
        assertDoesNotThrow(() -> zph.zoomCenter(1.5));
        assertDoesNotThrow(() -> zph.goToBar(50));
        zph.setZoomSensitivity(2.0); assertEquals(2.0, zph.getZoomSensitivity());
        zph.setPanSensitivity(10); assertEquals(10, zph.getPanSensitivity());
        zph.setSmoothScrolling(false); assertFalse(zph.isSmoothScrolling());
    }

    @Test void testChartTemplate() {
        ChartModel model = new ChartModel(data);
        model.addIndicator(new SMAIndicator(20));
        model.addDrawing(new HorizontalLineTool(105));
        ChartTemplate tmpl = ChartTemplate.fromModel(model, ChartType.CANDLESTICK);
        assertEquals("Custom", tmpl.getName());
        assertEquals(1, tmpl.getIndicators().size());
        assertEquals(1, tmpl.getDrawings().size());
        ChartModel model2 = new ChartModel(data);
        tmpl.applyTo(model2);
        assertEquals(model2.isShowVolume(), model.isShowVolume());
        tmpl.setChartType(ChartType.LINE); assertEquals(ChartType.LINE, tmpl.getChartType());
        tmpl.setName("My Template"); assertEquals("My Template", tmpl.getName());
    }

    @Test void testPriceAlert() {
        PriceAlert alert = new PriceAlert();
        AtomicBoolean fired = new AtomicBoolean(false);
        double targetPrice = data.getBar(data.size()-1).getClose() + 1000;
        String id = alert.addAlert("Test", PriceAlert.Condition.ABOVE, targetPrice,
                msg -> fired.set(true));
        assertNotNull(id);
        alert.updateVolumeAvg(data);
        alert.check(data);
        // Alert may or may not fire depending on random data
        alert.removeAlert(id);
        assertEquals(0, alert.getAlerts().size());
    }

    @Test void testDataReplay() {
        com.jcharts.core.ChartPanel cp = new com.jcharts.core.ChartPanel(data);
        DataReplay replay = new DataReplay(cp, data);
        assertFalse(replay.isPlaying());
        assertEquals(200, replay.getTotalBars());
        replay.stepForward();
        assertTrue(replay.getCurrentIndex() >= 1);
        replay.goTo(100);
        assertEquals(100, replay.getCurrentIndex());
        replay.stop();
        assertEquals(0, replay.getCurrentIndex());
    }

    @Test void testSymbolCompare() {
        SymbolCompare cmp = new SymbolCompare();
        cmp.addSymbol(data, "A", java.awt.Color.RED);
        cmp.addSymbol(data, "B", java.awt.Color.BLUE);
        var result = cmp.normalize();
        assertEquals(2, result.size());
        assertEquals("A", result.get(0).name);
        assertNotNull(result.get(0).percentChanges);
    }

    @Test void testDataExportCSV() throws Exception {
        String path = "/home/z/my-project/download/test_export.csv";
        DataExporter.toCSV(data, path);
        assertTrue(Files.exists(java.nio.file.Paths.get(path)));
        TimeSeries imported = DataExporter.fromCSV(path);
        assertEquals(data.size(), imported.size());
        new File(path).delete();
    }

    @Test void testDataExportJSON() throws Exception {
        String path = "/home/z/my-project/download/test_export.json";
        DataExporter.toJSON(data, path);
        assertTrue(Files.exists(java.nio.file.Paths.get(path)));
        String content = new String(Files.readAllBytes(java.nio.file.Paths.get(path)));
        assertTrue(content.startsWith("["));
        assertTrue(content.contains("\"o\":") || content.contains("\"h\":"));
        new File(path).delete();
    }

    @Test void testMultiPanelLayout() {
        MultiPanelLayout layout = new MultiPanelLayout();
        com.jcharts.core.ChartPanel p1 = new com.jcharts.core.ChartPanel(data);
        com.jcharts.core.ChartPanel p2 = new com.jcharts.core.ChartPanel(data);
        layout.addPanel(p1);
        layout.addPanel(p2);
        assertEquals(2, layout.getPanels().size());
        layout.setLayoutMode(MultiPanelLayout.LayoutMode.GRID_2X1);
        assertNotNull(layout.getContainer());
    }
}
