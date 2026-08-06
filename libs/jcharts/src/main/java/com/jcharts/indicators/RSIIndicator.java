package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Relative Strength Index (0-100 oscillator) */
public class RSIIndicator extends AbstractIndicator {
    private final int period;
    private static final Color BULL_ZONE = new Color(38, 166, 91, 30);
    private static final Color BEAR_ZONE = new Color(234, 57, 67, 30);

    public RSIIndicator(int period) {
        super("RSI(" + period + ")", "RSI" + period, ChartColor.INDICATOR_3);
        this.period = period;
    }

    @Override public void calculate(TimeSeries data) {
        double[] closes = data.getCloses();
        values = new double[closes.length];
        if (closes.length < period + 1) return;
        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++) {
            double chg = closes[i] - closes[i - 1];
            if (chg > 0) avgGain += chg; else avgLoss -= chg;
        }
        avgGain /= period; avgLoss /= period;
        values[period] = avgLoss == 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);
        for (int i = 0; i < period; i++) values[i] = Double.NaN;
        for (int i = period + 1; i < closes.length; i++) {
            double chg = closes[i] - closes[i - 1];
            avgGain = (avgGain * (period - 1) + (chg > 0 ? chg : 0)) / period;
            avgLoss = (avgLoss * (period - 1) + (chg < 0 ? -chg : 0)) / period;
            values[i] = avgLoss == 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);
        }
    }

    @Override public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        // RSI drawn in price area, rescaled to 0-100 price range
        if (model.isAutoScale()) {
            model.setManualMinPrice(0); model.setManualMaxPrice(100);
        }
        // Overbought/oversold zones
        int y70 = (int) model.priceToY(70, chartH);
        int y30 = (int) model.priceToY(30, chartH);
        int y50 = (int) model.priceToY(50, chartH);
        g.setColor(BEAR_ZONE);
        g.fillRect(model.getLeftMargin(), model.getTopMargin(), chartW, y70 - model.getTopMargin());
        g.setColor(BULL_ZONE);
        g.fillRect(model.getLeftMargin(), y30, chartW, model.getTopMargin() + model.getPriceAreaHeight(chartH) - y30);
        g.setStroke(new BasicStroke(0.5f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL, 0, new float[]{4, 4}, 0));
        g.setColor(ChartColor.TEXT_DIM);
        g.drawLine(model.getLeftMargin(), y70, model.getLeftMargin() + chartW, y70);
        g.drawLine(model.getLeftMargin(), y30, model.getLeftMargin() + chartW, y30);
        g.drawLine(model.getLeftMargin(), y50, model.getLeftMargin() + chartW, y50);
        g.setFont(new Font("SansSerif", Font.PLAIN, 9));
        g.drawString("70", model.getLeftMargin() + chartW + 3, y70 + 4);
        g.drawString("30", model.getLeftMargin() + chartW + 3, y30 + 4);
        g.drawString("50", model.getLeftMargin() + chartW + 3, y50 + 4);
        drawLine(g, model, chartW, chartH, values, 0);
        drawLabel(g, model.getLeftMargin() + 5, model.getTopMargin() + 15);
    }
}