#!/usr/bin/env python3
"""
demo.py — End-to-end offline demonstration of the whole system.

Runs entirely on synthetic data (no internet, no API keys):
  1. Generates realistic OHLCV data
  2. PART 1: one-shot multi-timeframe, multi-indicator scan + confluence
  3. PART 2: replays history through the FULL AGENT COLONY — orchestrator,
     timeframe agents, indicator sub-agents, lifecycle agent (living
     divergences), confluence agent, and alert agent — and prints the
     colony's accumulated reliability statistics.
"""

from __future__ import annotations

import asyncio

from divergence_system import (
    Config,
    MasterOrchestrator,
    MultiTimeframeScanner,
    SyntheticProvider,
)


def part1_scan() -> None:
    provider = SyntheticProvider()
    symbols = ["BTC/DEMO", "ETH/DEMO"]

    scanner = MultiTimeframeScanner(
        timeframes=["5m", "15m", "1h", "4h"],
        indicators=["rsi", "macd", "macd_hist", "stochastic", "cci", "mfi", "obv"],
        min_score=40.0,
        max_signal_age_bars=20,
    )

    print("=" * 72)
    print("PART 1 — One-shot multi-timeframe scan")
    print("=" * 72)
    for symbol in symbols:
        df = provider.fetch(symbol, interval="5m", bars=4000)
        divs = scanner.scan(df, symbol)
        print(f"\n{symbol}: {len(divs)} quality divergences found")
        for r in scanner.confluence(divs, symbol):
            print(
                f"  {r.direction.upper():<8} confluence={r.confluence_score:5.1f} "
                f"[{r.strength}] TFs={','.join(r.timeframes)} "
                f"inds={','.join(r.indicators)}"
            )
            for d in r.divergences[:5]:
                print(
                    f"    [{d.grade:>2}] {d.score:5.1f} {d.timeframe:<4} "
                    f"{d.indicator:<10} {d.signal}"
                )


def part2_colony() -> None:
    print()
    print("=" * 72)
    print("PART 2 — Agent colony replay (living divergences)")
    print("=" * 72)

    cfg = Config(
        provider="synthetic",
        symbols=["BTC/DEMO"],
        timeframes=["15m", "1h", "4h"],
        indicators=["rsi", "macd", "stochastic", "cci"],
        min_score=45.0,
        alerts_file=None,  # console only for the demo
    )
    provider = SyntheticProvider()
    base = provider.fetch(cfg.symbols[0], interval="5m", bars=3000)

    orch = MasterOrchestrator(cfg, provider)
    summary = asyncio.run(orch.run_replay(base, warmup=1500, step=12))

    print(f"\nalerts emitted       : {summary['alerts_emitted']}")
    print(f"divergences resolved : {summary['graveyard_size']}")
    print(f"still alive          : {len(summary['alive'])}")
    if summary["reliability"]:
        print("\nColony experience — reliability per indicator@timeframe:")
        for name, s in sorted(summary["reliability"].items(), key=lambda kv: -kv[1]["win_rate"]):
            print(
                f"  {name:<20} resolved={s['total']:<3} completed={s['completed']:<3} "
                f"invalidated={s['invalidated']:<3} expired={s['expired']:<3} "
                f"win_rate={s['win_rate']:.1%}"
            )
    print("\nDemo complete. Try: python main.py replay --provider ccxt --symbols BTC/USDT")


if __name__ == "__main__":
    part1_scan()
    part2_colony()
