package com.jcharts.data;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class TimeSeriesTest {
    private TimeSeries ts = TimeSeries.generateRandom(200, 100, 2.0, "TEST");

    @Test void testSize() { assertEquals(200, ts.size()); assertFalse(ts.isEmpty()); }
    @Test void testSymbol() { assertEquals("TEST", ts.getSymbol()); }
    @Test void testGetBar() { assertNotNull(ts.getBar(0)); assertThrows(IndexOutOfBoundsException.class, () -> ts.getBar(200)); }
    @Test void testSubSeries() { TimeSeries sub = ts.subSeries(50, 100); assertEquals(50, sub.size()); }
    @Test void testLast() { TimeSeries last = ts.last(20); assertEquals(20, last.size()); }
    @Test void testGlobalMaxMin() { assertTrue(ts.getGlobalMax() > ts.getGlobalMin()); }
    @Test void testArrays() {
        double[] c = ts.getCloses(), h = ts.getHighs(), l = ts.getLows(), o = ts.getOpens(), v = ts.getVolumes();
        assertEquals(200, c.length); assertEquals(200, h.length); assertEquals(200, l.length);
        assertEquals(200, o.length); assertEquals(200, v.length);
    }
    @Test void testTypicalPrices() { assertEquals(200, ts.getTypicalPrices().length); }
    @Test void testHL2HLC3OHLC4() {
        assertEquals(200, ts.getHL2().length);
        assertEquals(200, ts.getHLC3().length);
        assertEquals(200, ts.getOHLC4().length);
    }
    @Test void testAvgVolume() { assertTrue(ts.getAvgVolume() > 0); }
    @Test void testMerge() {
        TimeSeries a = TimeSeries.generateRandom(50, 100, 1, "A");
        TimeSeries b = TimeSeries.generateRandom(50, 100, 1, "B");
        TimeSeries m = TimeSeries.merge(a, b);
        assertEquals(100, m.size());
    }
    @Test void testSorted() {
        for (int i = 1; i < ts.size(); i++)
            assertTrue(ts.getBar(i).getTimestamp() >= ts.getBar(i-1).getTimestamp());
    }
}