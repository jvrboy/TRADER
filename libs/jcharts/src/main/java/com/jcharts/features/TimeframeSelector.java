package com.jcharts.features;

import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.temporal.ChronoUnit;
import java.util.ArrayList;
import java.util.List;
import java.util.function.Consumer;

/** Converts a TimeSeries to different timeframes by aggregating bars. */
public class TimeframeSelector {
    public enum Timeframe {
        M1("1m", ChronoUnit.MINUTES, 1),
        M5("5m", ChronoUnit.MINUTES, 5),
        M15("15m", ChronoUnit.MINUTES, 15),
        M30("30m", ChronoUnit.MINUTES, 30),
        H1("1H", ChronoUnit.HOURS, 1),
        H4("4H", ChronoUnit.HOURS, 4),
        D1("1D", ChronoUnit.DAYS, 1),
        W1("1W", ChronoUnit.WEEKS, 1),
        MN1("1M", ChronoUnit.MONTHS, 1);

        public final String label;
        final ChronoUnit unit;
        final int amount;
        Timeframe(String label, ChronoUnit unit, int amount) { this.label = label; this.unit = unit; this.amount = amount; }
    }

    public static TimeSeries convert(TimeSeries source, Timeframe tf) {
        if (source.isEmpty()) return source;
        List<OHLCBar> aggregated = new ArrayList<>();
        LocalDateTime currentStart = null;
        double o = 0, h = Double.MIN_VALUE, l = Double.MAX_VALUE, c = 0;
        double vol = 0;
        long ts = 0;

        for (OHLCBar bar : source.getBars()) {
            LocalDateTime barTime = bar.getDateTime();
            LocalDateTime bucketStart = floorToTimeframe(barTime, tf);

            if (currentStart == null || !bucketStart.equals(currentStart)) {
                if (currentStart != null && h > Double.MIN_VALUE) {
                    aggregated.add(new OHLCBar(ts, o, h, l, c, vol));
                }
                currentStart = bucketStart;
                o = bar.getOpen(); h = bar.getHigh(); l = bar.getLow(); c = bar.getClose(); vol = bar.getVolume();
                ts = barTime.toEpochSecond(ZoneOffset.UTC) * 1000;
            } else {
                h = Math.max(h, bar.getHigh());
                l = Math.min(l, bar.getLow());
                c = bar.getClose(); vol += bar.getVolume();
            }
        }
        if (h > Double.MIN_VALUE) aggregated.add(new OHLCBar(ts, o, h, l, c, vol));
        return new TimeSeries(aggregated, source.getSymbol(), tf.label);
    }

    private static LocalDateTime floorToTimeframe(LocalDateTime dt, Timeframe tf) {
        switch (tf) {
            case M1: return dt.truncatedTo(ChronoUnit.MINUTES);
            case M5: return dt.truncatedTo(ChronoUnit.HOURS).plusMinutes((dt.getMinute() / 5) * 5);
            case M15: return dt.truncatedTo(ChronoUnit.HOURS).plusMinutes((dt.getMinute() / 15) * 15);
            case M30: return dt.truncatedTo(ChronoUnit.HOURS).plusMinutes((dt.getMinute() / 30) * 30);
            case H1: return dt.truncatedTo(ChronoUnit.HOURS);
            case H4: return dt.truncatedTo(ChronoUnit.DAYS).plusHours((dt.getHour() / 4) * 4);
            case D1: return dt.truncatedTo(ChronoUnit.DAYS);
            case W1: return dt.minusDays(dt.getDayOfWeek().getValue() - 1).truncatedTo(ChronoUnit.DAYS);
            case MN1: return dt.withDayOfMonth(1).truncatedTo(ChronoUnit.DAYS);
            default: return dt;
        }
    }
}
