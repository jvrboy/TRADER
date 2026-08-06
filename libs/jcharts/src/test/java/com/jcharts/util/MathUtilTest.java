package com.jcharts.util;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class MathUtilTest {
    @Test void testClamp() { assertEquals(5, MathUtil.clamp(10, 0, 5)); assertEquals(0, MathUtil.clamp(-5, 0, 10)); assertEquals(5, MathUtil.clamp(5, 0, 10)); }
    @Test void testLerp() { assertEquals(1.5, MathUtil.lerp(1, 2, 0.5)); assertEquals(1, MathUtil.lerp(1, 2, 0)); assertEquals(2, MathUtil.lerp(1, 2, 1)); }
    @Test void testRound() { assertEquals(1.23, MathUtil.round2(1.234)); assertEquals(1.235, MathUtil.roundN(1.2345, 3)); }
    @Test void testSigmoid() { assertEquals(0.5, MathUtil.sigmoid(0), 0.001); assertTrue(MathUtil.sigmoid(10) > 0.99); }
    @Test void testTanh() { assertEquals(0, MathUtil.tanh(0), 0.001); assertTrue(MathUtil.tanh(10) > 0.99); }
    @Test void testMeanStddev() {
        double[] d = {2, 4, 4, 4, 5, 5, 7, 9};
        assertEquals(5.0, MathUtil.mean(d), 0.001);
        assertEquals(2.0, MathUtil.stddev(d), 0.01);
    }
    @Test void testMinMaxSum() {
        double[] d = {1, 5, 3, 9, 2};
        assertEquals(1, MathUtil.min(d)); assertEquals(9, MathUtil.max(d)); assertEquals(20, MathUtil.sum(d));
    }
    @Test void testCorrelation() {
        double[] x = {1,2,3,4,5}, y = {2,4,6,8,10};
        double corr = MathUtil.correlation(x, y);
        assertTrue(corr > 0.99);
    }
    @Test void testDiffCumulative() {
        double[] d = {1, 3, 6, 10};
        double[] diff = MathUtil.diff(d);
        assertEquals(0, diff[0]); assertEquals(2, diff[1]); assertEquals(3, diff[2]);
        double[] cum = MathUtil.cumulative(diff);
        assertEquals(0, cum[0]); assertEquals(2, cum[1]); assertEquals(5, cum[2]);
    }
}