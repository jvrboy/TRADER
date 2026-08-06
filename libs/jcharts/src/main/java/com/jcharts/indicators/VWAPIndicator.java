package com.jcharts.indicators;

import com.jcharts.data.TimeSeries;
import com.jcharts.core.ChartColor;

/** Volume Weighted Average Price. */
public class VWAPIndicator extends AbstractIndicator {
    public VWAPIndicator() {
        super("VWAP", "VWAP", ChartColor.INDICATOR_5);
    }

    @Override public void calculate(TimeSeries data) {
        int n = data.size();
        values = new double[n];
        double cumTPV = 0, cumVol = 0;
        for (int i = 0; i < n; i++) {
            double tp = (data.getBar(i).getHigh() + data.getBar(i).getLow() + data.getBar(i).getClose()) / 3.0;
            cumTPV += tp * data.getBar(i).getVolume();
            cumVol += data.getBar(i).getVolume();
            values[i] = cumVol > 0 ? cumTPV / cumVol : Double.NaN;
        }
    }
}