package com.sandbox.cli;

/**
 * ANSI color formatting for terminal output.
 * Provides a clean API for colored and styled text.
 */
public class ColorFormatter {

    public static final String RESET = "\u001B[0m";
    public static final String BLACK = "\u001B[30m";
    public static final String RED = "\u001B[31m";
    public static final String GREEN = "\u001B[32m";
    public static final String YELLOW = "\u001B[33m";
    public static final String BLUE = "\u001B[34m";
    public static final String MAGENTA = "\u001B[35m";
    public static final String CYAN = "\u001B[36m";
    public static final String WHITE = "\u001B[37m";
    public static final String BRIGHT_BLACK = "\u001B[90m";
    public static final String BRIGHT_RED = "\u001B[91m";
    public static final String BRIGHT_GREEN = "\u001B[92m";
    public static final String BRIGHT_YELLOW = "\u001B[93m";
    public static final String BRIGHT_BLUE = "\u001B[94m";
    public static final String BRIGHT_MAGENTA = "\u001B[95m";
    public static final String BRIGHT_CYAN = "\u001B[96m";
    public static final String BRIGHT_WHITE = "\u001B[97m";
    public static final String BG_RED = "\u001B[41m";
    public static final String BG_GREEN = "\u001B[42m";
    public static final String BG_YELLOW = "\u001B[43m";
    public static final String BG_BLUE = "\u001B[44m";
    public static final String BOLD = "\u001B[1m";
    public static final String DIM = "\u001B[2m";
    public static final String ITALIC = "\u001B[3m";
    public static final String UNDERLINE = "\u001B[4m";
    private static boolean noColor = false;
    public static void setNoColor(boolean value) { noColor = value; }
    public static boolean isNoColor() { return noColor; }
    public static String format(String color, String text) {
        if (noColor) return text;
        return color + text + RESET;
    }
    public static String red(String text) { return format(RED, text); }
    public static String green(String text) { return format(GREEN, text); }
    public static String yellow(String text) { return format(YELLOW, text); }
    public static String blue(String text) { return format(BLUE, text); }
    public static String magenta(String text) { return format(MAGENTA, text); }
    public static String cyan(String text) { return format(CYAN, text); }
    public static String bold(String text) { return format(BOLD, text); }
    public static String dim(String text) { return format(DIM, text); }
    public static String brightGreen(String text) { return format(BRIGHT_GREEN, text); }
    public static String brightRed(String text) { return format(BRIGHT_RED, text); }
    public static String brightYellow(String text) { return format(BRIGHT_YELLOW, text); }
    public static String brightBlue(String text) { return format(BRIGHT_BLUE, text); }
    public static String brightCyan(String text) { return format(BRIGHT_CYAN, text); }
    public static String success(String text) { return format(BRIGHT_GREEN + BOLD, text); }
    public static String error(String text) { return format(BRIGHT_RED + BOLD, text); }
    public static String warning(String text) { return format(BRIGHT_YELLOW + BOLD, text); }
    public static String info(String text) { return format(BRIGHT_CYAN, text); }
    public static String header(String text) { return format(BRIGHT_BLUE + BOLD, text); }
    public static String prompt(String text) { return format(BRIGHT_GREEN + BOLD, text); }
    public static String stripAnsi(String text) {
        return text.replaceAll("\\u001B\\[[;\\d]*m", "");
    }
}