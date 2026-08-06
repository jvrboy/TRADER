package com.jcharts.core;

import java.awt.Color;
import java.util.HashMap;
import java.util.Map;

/**
 * Centralized color theme management for all chart components.
 * Supports multiple built-in themes and custom color definitions.
 */
public class ChartColor {

    public static final Color BACKGROUND = new Color(19, 23, 34);
    public static final Color BACKGROUND_LIGHT = new Color(36, 39, 51);
    public static final Color GRID_LINE = new Color(37, 42, 58);
    public static final Color GRID_LINE_LIGHT = new Color(50, 55, 75);
    public static final Color TEXT = new Color(178, 179, 191);
    public static final Color TEXT_DIM = new Color(110, 112, 130);
    public static final Color BULLISH = new Color(38, 166, 91);
    public static final Color BULLISH_DIM = new Color(38, 166, 91, 80);
    public static final Color BEARISH = new Color(234, 57, 67);
    public static final Color BEARISH_DIM = new Color(234, 57, 67, 80);
    public static final Color ACCENT = new Color(77, 132, 240);
    public static final Color ACCENT_DIM = new Color(77, 132, 240, 60);
    public static final Color CROSSHAIR = new Color(120, 123, 139);
    public static final Color SELECTION = new Color(77, 132, 240, 100);
    public static final Color VOLUME_BULL = new Color(38, 166, 91, 60);
    public static final Color VOLUME_BEAR = new Color(234, 57, 67, 60);
    public static final Color INDICATOR_1 = new Color(77, 132, 240);
    public static final Color INDICATOR_2 = new Color(234, 57, 67);
    public static final Color INDICATOR_3 = new Color(242, 185, 49);
    public static final Color INDICATOR_4 = new Color(155, 89, 255);
    public static final Color INDICATOR_5 = new Color(38, 166, 91);
    public static final Color WHITE = new Color(255, 255, 255);
    public static final Color BLACK = new Color(0, 0, 0);
    public static final Color ORANGE = new Color(242, 147, 49);
    public static final Color YELLOW = new Color(242, 185, 49);
    public static final Color CYAN = new Color(49, 211, 242);
    public static final Color MAGENTA = new Color(211, 49, 242);
    public static final Color TRANSPARENT = new Color(0, 0, 0, 0);

    // Light theme colors
    public static final Color LT_BACKGROUND = new Color(255, 255, 255);
    public static final Color LT_GRID = new Color(232, 234, 238);
    public static final Color LT_TEXT = new Color(33, 37, 41);
    public static final Color LT_BORDER = new Color(222, 224, 228);

    public enum Theme { DARK, LIGHT, BLUE_NIGHT, SOLARIZED, HIGH_CONTRAST }

    private static Theme currentTheme = Theme.DARK;

    public static void setTheme(Theme theme) { currentTheme = theme; }
    public static Theme getTheme() { return currentTheme; }

    public static Color withAlpha(Color color, int alpha) {
        return new Color(color.getRed(), color.getGreen(), color.getBlue(), alpha);
    }

    public static Color lerp(Color a, Color b, double t) {
        int r = (int) (a.getRed() + (b.getRed() - a.getRed()) * t);
        int g = (int) (a.getGreen() + (b.getGreen() - a.getGreen()) * t);
        int bl = (int) (a.getBlue() + (b.getBlue() - a.getBlue()) * t);
        return new Color(r, g, bl);
    }

    public static String toHex(Color color) {
        return String.format("#%02X%02X%02X", color.getRed(), color.getGreen(), color.getBlue());
    }

    public static Color fromHex(String hex) {
        hex = hex.replace("#", "");
        return new Color(Integer.parseInt(hex.substring(0, 2), 16),
                Integer.parseInt(hex.substring(2, 4), 16),
                Integer.parseInt(hex.substring(4, 6), 16));
    }
}
