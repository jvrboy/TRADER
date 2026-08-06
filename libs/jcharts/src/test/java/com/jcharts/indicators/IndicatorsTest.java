package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

import java.awt.image.BufferedImage;
import java.awt.Graphics2D;
import java.awt.Color;

class IndicatorsTest {
    private final TimeSeries data = TimeSeries.generateRandom(200, 100, 2, "TEST");

    @Test void testSMA() {
        SMAIndicator sma = new SMAIndicator(20);
        sma.calculate(data);
        double[] v = sma.getValues();
        assertEquals(200, v.length);
        assertTrue(Double.isNaN(v[0]));
        assertFalse(Double.isNaN(v[19]));
        assertNotNull(sma.getName()); assertNotNull(sma.getShortName());
        assertNotNull(sma.getColor());
        sma.setColor(Color.RED); assertEquals(Color.RED, sma.getColor());
        assertEquals(20, sma.getPeriod());
    }

    @Test void testSMAValues() {
        SMAIndicator sma = new SMAIndicator(2);
        double[] test = {10, 20, 30, 40};
        TimeSeries ts = new TimeSeries();
        long t = 0;
        for (double v : test) ts.addBar(new com.jcharts.data.OHLCBar(t++, v, v+1, v-1, v, 100));
        sma.calculate(ts);
        double[] r = sma.getValues();
        assertTrue(Double.isNaN(r[0]));
        assertEquals(15, r[1], 0.001);
        assertEquals(25, r[2], 0.001);
        assertEquals(35, r[3], 0.001);
    }

    @Test void testEMA() {
        EMAIndicator ema = new EMAIndicator(20);
        ema.calculate(data);
        double[] v = ema.getValues();
        assertEquals(200, v.length);
        assertTrue(Double.isNaN(v[0]));
        assertFalse(Double.isNaN(v[19]));
        assertEquals(20, ema.getPeriod());
    }

    @Test void testRSI() {
        RSIIndicator rsi = new RSIIndicator(14);
        rsi.calculate(data);
        double[] v = rsi.getValues();
        assertEquals(200, v.length);
        for (int i = 14; i < 200; i++) {
            if (!Double.isNaN(v[i])) {
                assertTrue(v[i] >= 0 && v[i] <= 100, "RSI out of range: " + v[i]);
            }
        }
    }

    @Test void testMACD() {
        MACDIndicator macd = new MACDIndicator();
        macd.calculate(data);
        assertNotNull(macd.getValues());
        assertNotNull(macd.getSignal());
        assertNotNull(macd.getHistogram());
        assertEquals(200, macd.getValues().length);
    }

    @Test void testBollingerBands() {
        BollingerBandsIndicator bb = new BollingerBandsIndicator(20, 2.0);
        bb.calculate(data);
        double[] upper = bb.getUpper();
        double[] lower = bb.getLower();
        double[] mid = bb.getValues();
        assertEquals(200, upper.length);
        for (int i = 20; i < 200; i++) {
            if (!Double.isNaN(upper[i]) && !Double.isNaN(lower[i])) {
                assertTrue(upper[i] >= mid[i]);
                assertTrue(lower[i] <= mid[i]);
            }
        }
    }

    @Test void testStochastic() {
        StochasticIndicator stoch = new StochasticIndicator(14, 3);
        stoch.calculate(data);
        assertNotNull(stoch.getValues());
        assertNotNull(stoch.getDValues());
        double[] v = stoch.getValues();
        for (int i = 14; i < v.length; i++) {
            if (!Double.isNaN(v[i])) {
                assertTrue(v[i] >= 0 && v[i] <= 100, "Stoch out of range: " + v[i]);
            }
        }
    }

    @Test void testATR() {
        ATRIndicator atr = new ATRIndicator(14);
        atr.calculate(data);
        double[] v = atr.getValues();
        assertEquals(200, v.length);
        for (int i = 14; i < 200; i++) {
            if (!Double.isNaN(v[i])) {
                assertTrue(v[i] >= 0, "ATR negative: " + v[i]);
            }
        }
        assertEquals(14, atr.getPeriod());
    }

    @Test void testVWAP() {
        VWAPIndicator vwap = new VWAPIndicator();
        vwap.calculate(data);
        double[] v = vwap.getValues();
        assertEquals(200, v.length);
        assertFalse(Double.isNaN(v[v.length - 1]));
    }

    @Test void testVolumeProfile() {
        VolumeProfileIndicator vp = new VolumeProfileIndicator();
        vp.calculate(data);
        assertTrue(vp.getPOCPrice() > 0);
        assertTrue(vp.getValueAreaHigh() > vp.getValueAreaLow());
    }

    @Test void testAllIndicatorsRender() {
        com.jcharts.core.ChartModel model = new com.jcharts.core.ChartModel(data);
        BufferedImage img = new BufferedImage(800, 400, BufferedImage.TYPE_INT_ARGB);
        Graphics2D g2 = img.createGraphics();
        int cw = 710, ch = 350;
        for (Indicator ind : new Indicator[]{
            new SMAIndicator(20), new EMAIndicator(20), new BollingerBandsIndicator(),
            new VWAPIndicator(), new ATRIndicator(14), new VolumeProfileIndicator()
        }) {
            ind.calculate(data);
            assertDoesNotThrow(() -> ind.draw(g2, model, cw, ch));
        }
        // MACD with manual scale
        com.jcharts.core.ChartModel macdModel = new com.jcharts.core.ChartModel(data);
        macdModel.setAutoScale(false); macdModel.setManualMinPrice(-5); macdModel.setManualMaxPrice(5);
        MACDIndicator macd = new MACDIndicator(); macd.calculate(data);
        assertDoesNotThrow(() -> macd.draw(g2, macdModel, cw, ch));
        // RSI with manual scale
        com.jcharts.core.ChartModel rsiModel = new com.jcharts.core.ChartModel(data);
        rsiModel.setAutoScale(false); rsiModel.setManualMinPrice(0); rsiModel.setManualMaxPrice(100);
        RSIIndicator rsi = new RSIIndicator(14); rsi.calculate(data);
        assertDoesNotThrow(() -> rsi.draw(g2, rsiModel, cw, ch));
    }
}
