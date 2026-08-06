package com.jcharts.util;

/** Utility math functions for chart calculations. */
public final class MathUtil {
    private MathUtil() {}

    public static double clamp(double val, double min, double max) { return Math.max(min, Math.min(max, val)); }
    public static int clamp(int val, int min, int max) { return Math.max(min, Math.min(max, val)); }
    public static double lerp(double a, double b, double t) { return a + (b - a) * t; }
    public static double round2(double v) { return Math.round(v * 100.0) / 100.0; }
    public static double roundN(double v, int n) { double f = Math.pow(10, n); return Math.round(v * f) / f; }
    public static double sigmoid(double x) { return 1.0 / (1.0 + Math.exp(-x)); }
    public static double tanh(double x) { return (Math.exp(2*x) - 1) / (Math.exp(2*x) + 1); }
    public static double mean(double[] arr) { double s = 0; for (double v : arr) s += v; return s / arr.length; }
    public static double stddev(double[] arr) { double m = mean(arr); double s = 0; for (double v : arr) s += (v-m)*(v-m); return Math.sqrt(s/arr.length); }
    public static double max(double[] arr) { double m = -Double.MAX_VALUE; for (double v : arr) if (v > m) m = v; return m; }
    public static double min(double[] arr) { double m = Double.MAX_VALUE; for (double v : arr) if (v < m) m = v; return m; }
    public static double sum(double[] arr) { double s = 0; for (double v : arr) s += v; return s; }
    public static double correlation(double[] x, double[] y) {
        if (x.length != y.length || x.length == 0) return 0;
        double mx = mean(x), my = mean(y);
        double num = 0, dx = 0, dy = 0;
        for (int i = 0; i < x.length; i++) {
            double a = x[i] - mx, b = y[i] - my;
            num += a * b; dx += a * a; dy += b * b;
        }
        return Math.sqrt(dx * dy) == 0 ? 0 : num / Math.sqrt(dx * dy);
    }
    public static double[] diff(double[] arr) {
        double[] out = new double[arr.length];
        out[0] = 0;
        for (int i = 1; i < arr.length; i++) out[i] = arr[i] - arr[i-1];
        return out;
    }
    public static double[] cumulative(double[] arr) {
        double[] out = new double[arr.length];
        out[0] = arr[0];
        for (int i = 1; i < arr.length; i++) out[i] = out[i-1] + arr[i];
        return out;
    }
}
