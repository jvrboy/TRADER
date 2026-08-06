"""
plotting.py — Visualize divergences with matplotlib.

Renders a two-panel chart (price + oscillator) with divergence lines drawn
between the exact pivots that produced the signal, colored by direction and
styled by type (solid = regular, dashed = hidden).
"""

from __future__ import annotations

import pandas as pd

from .divergence import Divergence
from .indicators import compute_indicator


def plot_divergences(
    df: pd.DataFrame,
    divergences: list[Divergence],
    indicator: str,
    indicator_params: dict | None = None,
    title: str = "",
    save_path: str | None = None,
    show: bool = True,
    last_bars: int = 300,
):
    import matplotlib.pyplot as plt

    df = df.tail(last_bars)
    osc = compute_indicator(df, indicator, indicator_params)
    offset = 0  # df already sliced; recompute pivot indices relative to slice

    fig, (ax1, ax2) = plt.subplots(
        2, 1, figsize=(14, 8), sharex=True,
        gridspec_kw={"height_ratios": [2.2, 1]},
    )
    fig.patch.set_facecolor("#0d1117")
    for ax in (ax1, ax2):
        ax.set_facecolor("#0d1117")
        ax.tick_params(colors="#8b949e")
        for spine in ax.spines.values():
            spine.set_color("#30363d")
        ax.grid(color="#21262d", linewidth=0.5)

    ax1.plot(df.index, df["close"], color="#58a6ff", linewidth=1.0, label="close")
    ax2.plot(osc.index, osc, color="#d29922", linewidth=1.0, label=indicator)

    styles = {"regular": "-", "hidden": "--"}
    colors = {"bullish": "#3fb950", "bearish": "#f85149"}

    plotted = 0
    for d in divergences:
        if d.indicator != indicator:
            continue
        if d.start_time not in df.index or d.end_time not in df.index:
            continue
        c = colors[d.direction]
        ls = styles[d.kind]
        ax1.plot(
            [d.start_time, d.end_time],
            [d.price_start, d.price_end],
            color=c, linestyle=ls, linewidth=1.8, marker="o", markersize=4,
        )
        ax2.plot(
            [d.start_time, d.end_time],
            [d.osc_start, d.osc_end],
            color=c, linestyle=ls, linewidth=1.8, marker="o", markersize=4,
        )
        ax1.annotate(
            f"{d.kind[0].upper()}{d.direction[0].upper()} {d.grade}",
            xy=(d.end_time, d.price_end),
            fontsize=7, color=c, xytext=(4, 8), textcoords="offset points",
        )
        plotted += 1

    ax1.set_title(
        title or f"Divergences ({plotted} shown) — {indicator}",
        color="#e6edf3",
    )
    ax1.legend(loc="upper left", facecolor="#161b22", labelcolor="#e6edf3")
    ax2.legend(loc="upper left", facecolor="#161b22", labelcolor="#e6edf3")
    plt.tight_layout()

    if save_path:
        fig.savefig(save_path, dpi=130, facecolor=fig.get_facecolor())
        print(f"[plot] saved -> {save_path}")
    if show:
        plt.show()
    plt.close(fig)
    return fig
