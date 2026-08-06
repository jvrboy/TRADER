package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.data.OHLCBar;
import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;
import java.util.*;

/** Volume Profile: horizontal histogram showing volume distribution by price level. */
public class VolumeProfileIndicator extends AbstractIndicator {
    private static final int NUM_BINS = 24;
    private double[] priceLevels;
    private double[] volumes;
    private double pocPrice; // Point of Control
    private double valueAreaHigh, valueAreaLow;

    public VolumeProfileIndicator() {
        super("Volume Profile", "VP", ChartColor.INDICATOR_4);
    }

    @Override public void calculate(TimeSeries data) {
        if (data.isEmpty()) return;
        double minP = Double.MAX_VALUE, maxP = -Double.MAX_VALUE;
        for (OHLCBar bar : data.getBars()) {
            minP = Math.min(minP, bar.getLow());
            maxP = Math.max(maxP, bar.getHigh());
        }
        double step = (maxP - minP) / NUM_BINS;
        priceLevels = new double[NUM_BINS];
        volumes = new double[NUM_BINS];
        for (int i = 0; i < NUM_BINS; i++) priceLevels[i] = minP + step * (i + 0.5);
        for (OHLCBar bar : data.getBars()) {
            double tp = (bar.getHigh() + bar.getLow()) / 2.0;
            int bin = (int) ((tp - minP) / step);
            bin = Math.max(0, Math.min(NUM_BINS - 1, bin));
            volumes[bin] += bar.getVolume();
        }
        // POC
        int pocIdx = 0;
        for (int i = 1; i < NUM_BINS; i++) if (volumes[i] > volumes[pocIdx]) pocIdx = i;
        pocPrice = priceLevels[pocIdx];
        // Value Area (70% of volume around POC)
        double totalVol = Arrays.stream(volumes).sum();
        double target = totalVol * 0.7;
        int lo = pocIdx, hi = pocIdx;
        double accum = volumes[pocIdx];
        while (accum < target && (lo > 0 || hi < NUM_BINS - 1)) {
            double addLo = lo > 0 ? volumes[lo - 1] : 0;
            double addHi = hi < NUM_BINS - 1 ? volumes[hi + 1] : 0;
            if (addLo >= addHi && lo > 0) { lo--; accum += volumes[lo]; }
            else if (hi < NUM_BINS - 1) { hi++; accum += volumes[hi]; }
            else if (lo > 0) { lo--; accum += volumes[lo]; }
            else break;
        }
        valueAreaHigh = priceLevels[hi] + step / 2;
        valueAreaLow = priceLevels[lo] - step / 2;
    }

    @Override public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        if (priceLevels == null || priceLevels.length == 0) return;
        double maxVol = 0;
        for (double v : volumes) maxVol = Math.max(maxVol, v);
        int profileWidth = model.getRightMargin() - 5;
        double step = (model.getMaxPrice() - model.getMinPrice()) / NUM_BINS;
        for (int i = 0; i < NUM_BINS; i++) {
            double price = priceLevels[i];
            if (price < model.getMinPrice() || price > model.getMaxPrice()) continue;
            double y = model.priceToY(price, chartH);
            double w = maxVol > 0 ? (volumes[i] / maxVol) * profileWidth : 0;
            boolean isPOC = Math.abs(price - pocPrice) < step;
            boolean inVA = price >= valueAreaLow && price <= valueAreaHigh;
            g.setColor(isPOC ? ChartColor.ORANGE : inVA ? ChartColor.withAlpha(color, 150) : ChartColor.withAlpha(color, 60));
            int x = model.getLeftMargin() + chartW + 2;
            g.fillRect(x, (int)(y - step/2), (int)w, (int)step + 1);
        }
    }

    public double getPOCPrice() { return pocPrice; }
    public double getValueAreaHigh() { return valueAreaHigh; }
    public double getValueAreaLow() { return valueAreaLow; }
}