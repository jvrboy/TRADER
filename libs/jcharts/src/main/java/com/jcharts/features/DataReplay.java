package com.jcharts.features;

import com.jcharts.core.ChartPanel;
import com.jcharts.data.TimeSeries;
import com.jcharts.data.OHLCBar;
import javax.swing.Timer;
import java.util.List;
import java.util.ArrayList;

/** Replays historical data bar-by-bar with play/pause/step controls. */
public class DataReplay {
    private final ChartPanel panel;
    private final List<OHLCBar> allBars;
    private int currentIndex = 0;
    private final Timer timer;
    private int speedMs = 200;
    private boolean playing = false;
    private final List<Runnable> onStepListeners = new ArrayList<>();

    public DataReplay(ChartPanel panel, TimeSeries fullData) {
        this.panel = panel;
        this.allBars = new ArrayList<>(fullData.getBars());
        timer = new Timer(speedMs, e -> stepForward());
    }

    public void play() { playing = true; timer.start(); }
    public void pause() { playing = false; timer.stop(); }
    public void stop() { pause(); currentIndex = 0; rebuildAndRefresh(); }
    public void stepForward() { if (currentIndex < allBars.size()) { currentIndex++; rebuildAndRefresh(); } else pause(); }
    public void stepBackward() { if (currentIndex > 1) { currentIndex--; rebuildAndRefresh(); } }
    public void setSpeed(int ms) { this.speedMs = ms; timer.setDelay(ms); }
    public void goTo(int index) { currentIndex = Math.max(0, Math.min(index, allBars.size())); rebuildAndRefresh(); }

    private void rebuildAndRefresh() {
        TimeSeries partial = new TimeSeries(allBars.subList(0, currentIndex));
        panel.setData(partial);
        panel.getModel().setVisibleRange(Math.max(0, currentIndex - 100), Math.min(100, currentIndex));
        for (Runnable r : onStepListeners) r.run();
    }

    public boolean isPlaying() { return playing; }
    public int getCurrentIndex() { return currentIndex; }
    public int getTotalBars() { return allBars.size(); }
    public double getProgress() { return allBars.isEmpty() ? 0 : (double) currentIndex / allBars.size(); }
    public void addOnStepListener(Runnable r) { onStepListeners.add(r); }
}