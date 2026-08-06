package com.deriv.swarm.model;

import java.time.Instant;

public class TickData {
    private final String symbol;
    private final Instant epoch;
    private final double bid;
    private final double ask;
    private final double spread;

    public TickData(String symbol, Instant epoch, double bid, double ask) {
        this.symbol = symbol;
        this.epoch = epoch;
        this.bid = bid;
        this.ask = ask;
        this.spread = ask - bid;
    }

    public String getSymbol() { return symbol; }
    public Instant getEpoch() { return epoch; }
    public double getBid() { return bid; }
    public double getAsk() { return ask; }
    public double getSpread() { return spread; }
    public double getMid() { return (bid + ask) / 2.0; }

    @Override
    public String toString() {
        return String.format("Tick{%s @ %s: bid=%.5f ask=%.5f}", symbol, epoch, bid, ask);
    }
}
