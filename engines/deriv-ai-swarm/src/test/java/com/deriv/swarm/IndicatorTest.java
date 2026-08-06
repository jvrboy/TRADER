package com.deriv.swarm;

import com.deriv.swarm.indicators.*;
import com.deriv.swarm.model.*;
import org.junit.jupiter.api.*;
import static org.junit.jupiter.api.Assertions.*;

import java.time.Instant;
import java.util.*;
import java.util.stream.*;

class IndicatorTest {

    private List<Candle> candles;

    @BeforeEach
    void setUp() {
        candles = generateTestCandles(300);
    }

    @TestFactory
    List<DynamicTest> testIndicatorMathSMA() {
        return List.of(
            DynamicTest.dynamicTest("SMA(10)", () -> {
                List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
                double sma = IndicatorMath.sma(data, 10);
                assertTrue(sma > 0);
                double expected = data.stream().skip(data.size() - 10).mapToDouble(d -> d).average().orElse(0);
                assertEquals(expected, sma, 0.0001);
            }),
            DynamicTest.dynamicTest("SMA(50)", () -> {
                List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
                double sma = IndicatorMath.sma(data, 50);
                assertTrue(sma > 0);
            })
        );
    }

    @Test
    void testIndicatorMathEMA() {
        List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        double ema = IndicatorMath.ema(data, 20);
        assertTrue(ema > 0);
        assertNotEquals(0, ema);
    }

    @Test
    void testIndicatorMathWMA() {
        List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        double wma = IndicatorMath.wma(data, 20);
        assertTrue(wma > 0);
    }

    @Test
    void testIndicatorMathStdDev() {
        List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        double std = IndicatorMath.stddev(data, 20);
        assertTrue(std >= 0);
    }

    @Test
    void testIndicatorMathRSI() {
        List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        double rsi = IndicatorMath.rsi(data, 14);
        assertTrue(rsi >= 0 && rsi <= 100, "RSI should be between 0 and 100, got " + rsi);
    }

    @Test
    void testIndicatorMathMax() {
        List<Double> data = candles.stream().map(Candle::getHigh).collect(Collectors.toList());
        double max = IndicatorMath.max(data, 20);
        double expected = data.stream().skip(data.size() - 20).mapToDouble(d -> d).max().orElse(0);
        assertEquals(expected, max, 0.0001);
    }

    @Test
    void testIndicatorMathMin() {
        List<Double> data = candles.stream().map(Candle::getLow).collect(Collectors.toList());
        double min = IndicatorMath.min(data, 20);
        double expected = data.stream().skip(data.size() - 20).mapToDouble(d -> d).min().orElse(0);
        assertEquals(expected, min, 0.0001);
    }

    @Test
    void testIndicatorMathLinearRegression() {
        List<Double> data = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        double lr = IndicatorMath.linearRegression(data, 20);
        assertTrue(lr > 0);
    }

    @Test
    void testIndicatorMathCorrelation() {
        List<Double> x = candles.stream().map(Candle::getClose).collect(Collectors.toList());
        List<Double> y = candles.stream().map(Candle::getHigh).collect(Collectors.toList());
        double corr = IndicatorMath.correlation(x, y, 20);
        assertTrue(corr >= -1 && corr <= 1, "Correlation should be between -1 and 1");
    }

    @Test
    void testTrueRange() {
        double tr = IndicatorMath.trueRange(1.1020, 1.1000, 1.1010);
        assertEquals(0.0020, tr, 0.0001);
    }

    @Test
    void testSampleIndicators() {
        List<String> indicatorClasses = List.of(
            "com.deriv.swarm.indicators.trend.Trend_SMA_20",
            "com.deriv.swarm.indicators.trend.Trend_EMA_20",
            "com.deriv.swarm.indicators.momentum.Momentum_RSI_14",
            "com.deriv.swarm.indicators.volatility.Volatility_ATR_14",
            "com.deriv.swarm.indicators.volume.Volume_OBV",
            "com.deriv.swarm.indicators.statistical.Stat_ZScore_20"
        );

        for (String className : indicatorClasses) {
            try {
                Class<?> cls = Class.forName(className);
                TechnicalIndicator ind = (TechnicalIndicator) cls.getDeclaredConstructor().newInstance();
                assertNotNull(ind.getName());
                assertNotNull(ind.getCategory());
                assertNotNull(ind.getDescription());
                assertTrue(ind.getMinCandles() > 0);

                IndicatorResult result = ind.calculate(candles, "R_100", "5m");
                if (result != null) {
                    assertNotNull(result.getIndicatorName());
                    assertNotNull(result.getValues());
                    assertTrue(result.getValues().containsKey("value"));
                    assertNotNull(result.getSignal());
                }
            } catch (Exception e) {
                fail("Failed to test indicator " + className + ": " + e.getMessage());
            }
        }
    }

    @Test
    void testCandleModel() {
        Candle c = new Candle(Instant.now(), 1.10, 1.11, 1.09, 1.105, 5000);
        assertEquals(1.10, c.getOpen());
        assertEquals(1.11, c.getHigh());
        assertEquals(1.09, c.getLow());
        assertEquals(1.105, c.getClose());
        assertEquals(5000, c.getTickVolume());
        assertTrue(c.isBullish());
        assertFalse(c.isBearish());
        assertEquals(0.005, c.getBody(), 0.0001);
        assertEquals(0.02, c.getRange(), 0.0001);
    }

    @Test
    void testSignalType() {
        assertEquals(1.0, SignalType.STRONG_BUY.getScore());
        assertEquals(0.5, SignalType.BUY.getScore());
        assertEquals(0.0, SignalType.NEUTRAL.getScore());
        assertEquals(-0.5, SignalType.SELL.getScore());
        assertEquals(-1.0, SignalType.STRONG_SELL.getScore());
        assertTrue(SignalType.STRONG_BUY.isBullish());
        assertTrue(SignalType.BUY.isBullish());
        assertFalse(SignalType.NEUTRAL.isBullish());
        assertFalse(SignalType.NEUTRAL.isBearish());
        assertTrue(SignalType.SELL.isBearish());
    }

    @Test
    void testIndicatorResultModel() {
        Map<String, Double> values = Map.of("value", 1.234, "strength", 0.8);
        IndicatorResult result = new IndicatorResult(
            "TestIndicator", "R_100", "5m", Instant.now(),
            values, SignalType.BUY, 0.8, "Test description"
        );
        assertEquals("TestIndicator", result.getIndicatorName());
        assertEquals("R_100", result.getSymbol());
        assertEquals("5m", result.getTimeframe());
        assertEquals(SignalType.BUY, result.getSignal());
        assertEquals(0.8, result.getStrength());
        assertEquals(1.234, result.getValue("value"), 0.0001);
    }

    @Test
    void testTickDataModel() {
        TickData tick = new TickData("R_100", Instant.now(), 1.10000, 1.10002);
        assertEquals("R_100", tick.getSymbol());
        assertEquals(1.10000, tick.getBid());
        assertEquals(1.10002, tick.getAsk());
        assertEquals(0.00002, tick.getSpread(), 0.000001);
        assertEquals(1.10001, tick.getMid(), 0.000001);
    }

    @Test
    void testInsufficientData() {
        List<Candle> shortData = candles.subList(0, 5);
        try {
            Class<?> cls = Class.forName("com.deriv.swarm.indicators.trend.Trend_SMA_20");
            TechnicalIndicator ind = (TechnicalIndicator) cls.getDeclaredConstructor().newInstance();
            IndicatorResult result = ind.calculate(shortData, "R_100", "5m");
            assertNull(result, "Should return null for insufficient data");
        } catch (Exception e) {
            // Expected - class may not be found in test
        }
    }

    private List<Candle> generateTestCandles(int count) {
        Random random = new Random(42);
        double price = 1.10000;
        List<Candle> c = new ArrayList<>();
        for (int i = 0; i < count; i++) {
            double open = price;
            double change = (random.nextDouble() - 0.48) * 0.003;
            double close = open + change;
            double high = Math.max(open, close) + random.nextDouble() * 0.001;
            double low = Math.min(open, close) - random.nextDouble() * 0.001;
            c.add(new Candle(Instant.now().minusSeconds((count - i) * 300L),
                    open, high, low, close, (long) (random.nextInt(10000) + 1000)));
            price = close;
        }
        return c;
    }
}