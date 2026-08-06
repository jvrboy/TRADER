package com.jcharts.features;

import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;

/** Price alert system that monitors the latest bar and fires callbacks when conditions are met. */
public class PriceAlert {
    public enum Condition { ABOVE, BELOW, CROSS_ABOVE, CROSS_BELOW, RSI_ABOVE, RSI_BELOW, VOLUME_SPIKE }

    public static class Alert {
        final String id;
        final String message;
        final Condition condition;
        final double value;
        boolean triggered;
        final Consumer<String> callback;

        Alert(String id, String message, Condition condition, double value, Consumer<String> callback) {
            this.id = id; this.message = message; this.condition = condition;
            this.value = value; this.callback = callback;
        }
    }

    private final List<Alert> alerts = new ArrayList<>();
    private double volumeAvg = 0;
    private double volumeSpikeMultiplier = 2.0;

    public String addAlert(String message, Condition condition, double value, Consumer<String> callback) {
        String id = java.util.UUID.randomUUID().toString().substring(0, 8);
        alerts.add(new Alert(id, message, condition, value, callback));
        return id;
    }

    public void removeAlert(String id) { alerts.removeIf(a -> a.id.equals(id)); }
    public List<Alert> getAlerts() { return alerts; }

    public void check(TimeSeries data) {
        if (data.isEmpty() || alerts.isEmpty()) return;
        OHLCBar last = data.getBar(data.size() - 1);
        OHLCBar prev = data.size() > 1 ? data.getBar(data.size() - 2) : last;

        for (Alert a : alerts) {
            if (a.triggered) continue;
            boolean fire = false;
            switch (a.condition) {
                case ABOVE: fire = last.getClose() > a.value; break;
                case BELOW: fire = last.getClose() < a.value; break;
                case CROSS_ABOVE: fire = prev.getClose() <= a.value && last.getClose() > a.value; break;
                case CROSS_BELOW: fire = prev.getClose() >= a.value && last.getClose() < a.value; break;
                case VOLUME_SPIKE: fire = volumeAvg > 0 && last.getVolume() > volumeAvg * volumeSpikeMultiplier; break;
                default: break;
            }
            if (fire) { a.triggered = true; a.callback.accept(a.message); }
        }
    }

    public void updateVolumeAvg(TimeSeries data) {
        double[] vols = data.getVolumes();
        double sum = 0; int cnt = Math.min(20, vols.length);
        for (int i = vols.length - cnt; i < vols.length; i++) sum += vols[i];
        volumeAvg = cnt > 0 ? sum / cnt : 0;
    }
}
