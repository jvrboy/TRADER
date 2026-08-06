package com.deriv.swarm.model;

public enum SignalType {
    STRONG_BUY(1.0),
    BUY(0.5),
    NEUTRAL(0.0),
    SELL(-0.5),
    STRONG_SELL(-1.0);

    private final double score;

    SignalType(double score) { this.score = score; }

    public double getScore() { return score; }

    public boolean isBullish() { return score > 0; }
    public boolean isBearish() { return score < 0; }
}
