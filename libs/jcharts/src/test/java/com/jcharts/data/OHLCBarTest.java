package com.jcharts.data;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.time.LocalDateTime;

class OHLCBarTest {
    @Test void testConstruction() {
        OHLCBar b = new OHLCBar(1000, 100, 105, 99, 103, 1000000);
        assertEquals(1000, b.getTimestamp()); assertEquals(100, b.getOpen());
        assertEquals(105, b.getHigh()); assertEquals(99, b.getLow());
        assertEquals(103, b.getClose()); assertEquals(1000000, b.getVolume());
    }
    @Test void testBullish() { assertTrue(new OHLCBar(0,100,105,99,103,1000).isBullish()); }
    @Test void testBearish() { assertTrue(new OHLCBar(0,103,105,99,100,1000).isBearish()); }
    @Test void testDoji() { assertTrue(new OHLCBar(0,100,110,90,100.05,1000).isDoji()); }
    @Test void testBodySize() { assertEquals(3, new OHLCBar(0,100,105,99,103,1000).getBodySize(), 0.001); }
    @Test void testWicks() {
        OHLCBar b = new OHLCBar(0,100,110,95,103,1000);
        assertEquals(7, b.getUpperWick(), 0.001); assertEquals(5, b.getLowerWick(), 0.001);
    }
    @Test void testRange() { assertEquals(15, new OHLCBar(0,100,110,95,103,1000).getRange(), 0.001); }
    @Test void testMidpoint() { assertEquals(100, new OHLCBar(0,100,110,90,105,1000).getMidpoint(), 0.001); }
    @Test void testInvalidHighLow() { assertThrows(IllegalArgumentException.class, () -> new OHLCBar(0,100,90,95,100,1000)); }
    @Test void testNegative() { assertThrows(IllegalArgumentException.class, () -> new OHLCBar(0,-1,105,99,103,1000)); }
    @Test void testComparable() { assertTrue(new OHLCBar(1000,100,105,99,103,1000).compareTo(new OHLCBar(2000,100,105,99,103,1000)) < 0); }
    @Test void testEquals() {
        OHLCBar a = new OHLCBar(1000,100,105,99,103,1000);
        OHLCBar b = new OHLCBar(1000,110,115,109,113,2000);
        assertEquals(a, b); assertEquals(a.hashCode(), b.hashCode());
    }
    @Test void testToString() { assertTrue(new OHLCBar(0,100,105,99,103,1000).toString().contains("OHLCBar")); }
}
