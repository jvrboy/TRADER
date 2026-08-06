#!/usr/bin/env python3
"""
main.py — CLI entrypoint for the Divergence System.

Usage:
    # One full multi-timeframe scan (offline demo data):
    python main.py scan

    # Scan real crypto data from Binance:
    python main.py scan --provider ccxt --symbols BTC/USDT ETH/USDT

    # Scan stocks via yfinance:
    python main.py scan --provider yfinance --symbols AAPL TSLA --interval 5m

    # Run the ALIVE engine (continuous scanning + lifecycle tracking):
    python main.py live --provider ccxt --symbols BTC/USDT --poll 60

    # Plot detected divergences to a PNG:
    python main.py scan --plot out.png --plot-indicator rsi

    # Use a JSON config file:
    python main.py live --config config.example.json
"""

from __future__ import annotations

import argparse
import json
import sys

import asyncio

from divergence_system import (
    AliveDivergenceEngine,
    Config,
    ConsoleAlertHandler,
    JsonlAlertHandler,
    MasterOrchestrator,
    MultiTimeframeScanner,
    WebhookAlertHandler,
    get_provider,
)


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description="Multi-timeframe divergence system")
    p.add_argument(
        "mode",
        choices=["scan", "live", "colony", "replay"],
        help="scan: one-shot | live: alive engine | colony: live agent colony | replay: colony backtest",
    )
    p.add_argument("--warmup", type=int, help="replay warmup bars (default 1000)")
    p.add_argument("--step", type=int, help="replay step in base bars (default 12)")
    p.add_argument("--config", help="JSON config file path")
    p.add_argument("--provider", choices=["synthetic", "yfinance", "ccxt"])
    p.add_argument("--exchange", help="ccxt exchange id (default binance)")
    p.add_argument("--symbols", nargs="+", help="symbols to scan")
    p.add_argument("--interval", help="base (lowest) timeframe, e.g. 5m")
    p.add_argument("--timeframes", nargs="+", help="timeframes to analyze, e.g. 5m 15m 1h 4h")
    p.add_argument("--indicators", nargs="+", help="indicators, e.g. rsi macd stochastic")
    p.add_argument("--min-score", type=float, help="minimum quality score (0-100)")
    p.add_argument("--poll", type=float, help="live poll interval seconds")
    p.add_argument("--webhook", help="webhook URL for alerts")
    p.add_argument("--json", action="store_true", help="print scan results as JSON")
    p.add_argument("--plot", help="save divergence chart PNG to this path")
    p.add_argument("--plot-indicator", default="rsi", help="indicator to plot (default rsi)")
    return p


def make_config(args: argparse.Namespace) -> Config:
    cfg = Config.from_json(args.config) if args.config else Config()
    if args.provider:
        cfg.provider = args.provider
    if args.exchange:
        cfg.exchange = args.exchange
    if args.symbols:
        cfg.symbols = args.symbols
    if args.interval:
        cfg.base_interval = args.interval
    if args.timeframes:
        cfg.timeframes = args.timeframes
    if args.indicators:
        cfg.indicators = args.indicators
    if args.min_score is not None:
        cfg.min_score = args.min_score
    if args.poll is not None:
        cfg.poll_seconds = args.poll
    if args.webhook:
        cfg.webhook_url = args.webhook
    return cfg


def make_fetch_fn(cfg: Config):
    if cfg.provider == "ccxt":
        provider = get_provider("ccxt", exchange=cfg.exchange)
        return lambda s: provider.fetch(s, interval=cfg.base_interval, limit=cfg.ccxt_limit)
    if cfg.provider == "yfinance":
        provider = get_provider("yfinance")
        return lambda s: provider.fetch(s, interval=cfg.base_interval, lookback=cfg.lookback)
    provider = get_provider("synthetic")
    return lambda s: provider.fetch(s, interval=cfg.base_interval)


def main() -> int:
    args = build_parser().parse_args()
    cfg = make_config(args)

    scanner = MultiTimeframeScanner(
        timeframes=cfg.timeframes,
        indicators=cfg.indicators,
        indicator_params=cfg.indicator_params,
        min_score=cfg.min_score,
        max_signal_age_bars=cfg.max_signal_age_bars,
        pivot_left=cfg.pivot_left,
        pivot_right=cfg.pivot_right,
        include_hidden=cfg.include_hidden,
    )
    fetch_fn = make_fetch_fn(cfg)

    if args.mode == "scan":
        exit_code = 0
        for symbol in cfg.symbols:
            print(f"\n=== Scanning {symbol} ===")
            try:
                base_df = fetch_fn(symbol)
            except Exception as exc:
                print(f"  fetch failed: {exc}")
                exit_code = 1
                continue

            divergences = scanner.scan(base_df, symbol)
            reports = scanner.confluence(divergences, symbol)

            if args.json:
                print(json.dumps([r.to_dict() for r in reports], indent=2, default=str))
            else:
                if not divergences:
                    print("  no active divergences above the quality threshold.")
                for r in reports:
                    print(
                        f"\n  {r.direction.upper()} confluence: {r.confluence_score:.1f} "
                        f"({r.strength}) | TFs: {', '.join(r.timeframes)} "
                        f"| indicators: {', '.join(r.indicators)}"
                    )
                    for d in r.divergences[:10]:
                        print(
                            f"    [{d.grade:>2}] {d.score:5.1f}  {d.timeframe:<4} "
                            f"{d.indicator:<10} {d.signal:<16} "
                            f"{d.start_time:%Y-%m-%d %H:%M} -> {d.end_time:%Y-%m-%d %H:%M}"
                        )

            if args.plot:
                from divergence_system.mtf import resample_ohlcv
                from divergence_system.plotting import plot_divergences

                tf = cfg.timeframes[0]
                df_tf = resample_ohlcv(base_df, tf)
                plot_divergences(
                    df_tf,
                    [d for d in divergences if d.timeframe == tf],
                    indicator=args.plot_indicator,
                    title=f"{symbol} {tf} — {args.plot_indicator} divergences",
                    save_path=args.plot,
                    show=False,
                )
        return exit_code

    if args.mode == "replay":
        warmup = args.warmup or cfg.replay_warmup
        step = args.step or cfg.replay_step
        exit_code = 0
        for symbol in cfg.symbols:
            print(f"\n=== Replaying {symbol} through the agent colony ===")
            try:
                base_df = fetch_fn(symbol)
            except Exception as exc:
                print(f"  fetch failed: {exc}")
                exit_code = 1
                continue
            orch = MasterOrchestrator(cfg)
            summary = asyncio.run(
                orch.run_replay(base_df, warmup=warmup, step=step, symbol=symbol)
            )
            print(f"\n  alerts emitted       : {summary['alerts_emitted']}")
            print(f"  divergences resolved : {summary['graveyard_size']}")
            print(f"  still alive          : {len(summary['alive'])}")
            for name, s in sorted(
                summary["reliability"].items(), key=lambda kv: -kv[1]["win_rate"]
            ):
                print(
                    f"    {name:<20} total={s['total']:<3} win_rate={s['win_rate']:.1%} "
                    f"(completed={s['completed']} invalidated={s['invalidated']} "
                    f"expired={s['expired']})"
                )
        return exit_code

    if args.mode == "colony":
        orch = MasterOrchestrator(cfg)
        asyncio.run(orch.run_live(fetch_fn, poll_seconds=cfg.poll_seconds))
        return 0

    # live mode (classic single-loop alive engine)
    handlers = [ConsoleAlertHandler(), JsonlAlertHandler(cfg.jsonl_log)]
    if cfg.webhook_url:
        handlers.append(WebhookAlertHandler(cfg.webhook_url))

    engine = AliveDivergenceEngine(
        scanner=scanner,
        fetch_fn=fetch_fn,
        symbols=cfg.symbols,
        alert_handlers=handlers,
        target_atr_multiple=cfg.target_atr_multiple,
        expiry_bars=cfg.expiry_bars,
        poll_seconds=cfg.poll_seconds,
    )
    engine.run_forever()
    return 0


if __name__ == "__main__":
    sys.exit(main())
