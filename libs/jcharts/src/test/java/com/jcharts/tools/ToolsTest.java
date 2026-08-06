package com.jcharts.tools;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.awt.image.BufferedImage;
import java.awt.Graphics2D;
import java.awt.Color;

class ToolsTest {
    private final TimeSeries data = TimeSeries.generateRandom(100, 100, 2, "TST");
    private final ChartModel model = new ChartModel(data);
    private final BufferedImage img = new BufferedImage(800, 400, BufferedImage.TYPE_INT_ARGB);
    private final Graphics2D g2 = img.createGraphics();
    private final int cw = 710, ch = 350;

    private void assertDraws(DrawingTool tool) { assertDoesNotThrow(() -> tool.draw(g2, model, cw, ch)); }
    private void assertName(DrawingTool tool) { assertNotNull(tool.getName()); assertTrue(tool.getName().length() > 0); }
    private void assertColor(DrawingTool tool) {
        tool.setColor(Color.RED); assertEquals(Color.RED, tool.getColor());
    }
    private void assertSelection(DrawingTool tool) {
        tool.setSelected(true); assertTrue(tool.isSelected());
        tool.setSelected(false); assertFalse(tool.isSelected());
    }
    private void assertBounds(DrawingTool tool) { assertNotNull(tool.getBounds()); assertEquals(4, tool.getBounds().length); }

    @Test void testTrendLine() {
        TrendLineTool t = new TrendLineTool(10, 100, 50, 120);
        assertName(t); assertDraws(t); assertColor(t); assertSelection(t); assertBounds(t);
        t = new TrendLineTool(10, 100, 50, 120, false);
        assertDraws(t);
    }
    @Test void testHorizontalLine() {
        HorizontalLineTool t = new HorizontalLineTool(105);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t); assertTrue(t.containsPoint(0, 105.1));
    }
    @Test void testVerticalLine() {
        VerticalLineTool t = new VerticalLineTool(50, "Test");
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t); assertEquals(50, t.getBarIndex());
    }
    @Test void testRectangle() {
        RectangleTool t = new RectangleTool(10, 110, 50, 95);
        assertName(t); assertDraws(t); assertColor(t); assertSelection(t); assertBounds(t);
    }
    @Test void testEllipse() {
        EllipseTool t = new EllipseTool(10, 110, 50, 95);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
    }
    @Test void testFibonacciRetracement() {
        FibonacciRetracementTool t = new FibonacciRetracementTool(10, 90, 50, 120);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
        assertEquals(7, t.getLevels().length);
    }
    @Test void testFibonacciFan() {
        FibonacciFanTool t = new FibonacciFanTool(10, 90, 50, 120);
        assertName(t); assertDraws(t); assertBounds(t);
    }
    @Test void testFibonacciTimezone() {
        FibonacciTimezoneTool t = new FibonacciTimezoneTool(10);
        assertName(t); assertDraws(t); assertBounds(t);
    }
    @Test void testText() {
        TextTool t = new TextTool(50, 105, "Hello");
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t); assertEquals("Hello", t.getText());
    }
    @Test void testArrow() {
        ArrowTool t = new ArrowTool(10, 95, 50, 115);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
    }
    @Test void testMeasurement() {
        MeasurementTool t = new MeasurementTool(10, 100, 50, 120);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
    }
    @Test void testChannel() {
        ChannelTool t = new ChannelTool(10, 95, 50, 115, 30, 120);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
    }
    @Test void testPitchfork() {
        PitchforkTool t = new PitchforkTool(10, 95, 40, 115, 60, 90);
        assertName(t); assertDraws(t); assertColor(t); assertBounds(t);
    }
    @Test void testBrush() {
        BrushTool t = new BrushTool();
        assertName(t); assertColor(t); assertBounds(t);
        for (int i = 0; i < 10; i++) t.addPoint(100 + i * 5, 200 + (int)(Math.sin(i) * 20));
        assertDraws(t); assertEquals(10, t.getPoints().size());
        t.clear(); assertDoesNotThrow(() -> t.draw(g2, model, cw, ch));
    }
}