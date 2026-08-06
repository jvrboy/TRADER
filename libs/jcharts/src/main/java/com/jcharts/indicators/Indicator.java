package com.jcharts.indicators;

import com.jcharts.core.ChartModel;
import java.awt.*;

/** Interface for all technical indicators. */
public interface Indicator {
    void calculate(com.jcharts.data.TimeSeries data);
    void draw(Graphics2D g, ChartModel model, int chartW, int chartH);
    String getName();
    String getShortName();
    Color getColor();
    void setColor(Color c);
    double[] getValues();
}
