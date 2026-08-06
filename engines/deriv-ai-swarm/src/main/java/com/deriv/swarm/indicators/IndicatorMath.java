package com.deriv.swarm.indicators;

import java.util.List;

public final class IndicatorMath {
    private IndicatorMath() {}

    public static double sma(List<Double> data, int period) {
        if (data.size() < period) return 0;
        double sum = 0;
        for (int i = data.size() - period; i < data.size(); i++) sum += data.get(i);
        return sum / period;
    }

    public static double ema(List<Double> data, int period) {
        if (data.size() < period) return 0;
        double k = 2.0 / (period + 1);
        double ema = sma(data, period);
        for (int i = data.size() - period + 1; i < data.size(); i++) {
            ema = data.get(i) * k + ema * (1 - k);
        }
        return ema;
    }

    public static double wma(List<Double> data, int period) {
        if (data.size() < period) return 0;
        double denom = period * (period + 1) / 2.0;
        double sum = 0;
        for (int i = 0; i < period; i++) {
            sum += data.get(data.size() - period + i) * (i + 1);
        }
        return sum / denom;
    }

    public static double stddev(List<Double> data, int period) {
        double mean = sma(data, period);
        double sum = 0;
        for (int i = data.size() - period; i < data.size(); i++) {
            double diff = data.get(i) - mean;
            sum += diff * diff;
        }
        return Math.sqrt(sum / period);
    }

    public static double trueRange(double high, double low, double prevClose) {
        return Math.max(Math.max(high - low, Math.abs(high - prevClose)), Math.abs(low - prevClose));
    }

    public static double rsi(List<Double> closes, int period) {
        if (closes.size() < period + 1) return 50;
        double gain = 0, loss = 0;
        for (int i = closes.size() - period; i < closes.size(); i++) {
            double change = closes.get(i) - closes.get(i - 1);
            if (change > 0) gain += change; else loss -= change;
        }
        double avgGain = gain / period;
        double avgLoss = loss / period;
        if (avgLoss == 0) return 100;
        double rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    public static double max(List<Double> data, int period) {
        double max = Double.MIN_VALUE;
        for (int i = data.size() - period; i < data.size(); i++) {
            if (data.get(i) > max) max = data.get(i);
        }
        return max;
    }

    public static double min(List<Double> data, int period) {
        double min = Double.MAX_VALUE;
        for (int i = data.size() - period; i < data.size(); i++) {
            if (data.get(i) < min) min = data.get(i);
        }
        return min;
    }

    public static double highest(List<Double> data, int from, int count) {
        double max = Double.MIN_VALUE;
        for (int i = from; i < from + count && i < data.size(); i++) {
            if (data.get(i) > max) max = data.get(i);
        }
        return max;
    }

    public static double lowest(List<Double> data, int from, int count) {
        double min = Double.MAX_VALUE;
        for (int i = from; i < from + count && i < data.size(); i++) {
            if (data.get(i) < min) min = data.get(i);
        }
        return min;
    }

    public static double linearRegression(List<Double> data, int period) {
        if (data.size() < period) return data.get(data.size() - 1);
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = period;
        for (int i = 0; i < n; i++) {
            double y = data.get(data.size() - period + i);
            sumX += i; sumY += y; sumXY += i * y; sumX2 += i * i;
        }
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;
        return slope * (n - 1) + intercept;
    }

    public static double correlation(List<Double> x, List<Double> y, int period) {
        int n = Math.min(x.size(), y.size());
        if (n < period) return 0;
        double mx = 0, my = 0;
        for (int i = n - period; i < n; i++) { mx += x.get(i); my += y.get(i); }
        mx /= period; my /= period;
        double num = 0, dx = 0, dy = 0;
        for (int i = n - period; i < n; i++) {
            double a = x.get(i) - mx, b = y.get(i) - my;
            num += a * b; dx += a * a; dy += b * b;
        }
        double denom = Math.sqrt(dx * dy);
        return denom == 0 ? 0 : num / denom;
    }
}
