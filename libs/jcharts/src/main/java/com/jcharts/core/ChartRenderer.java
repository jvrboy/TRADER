package com.jcharts.core;

import com.jcharts.data.TimeSeries;

import java.awt.*;
import java.awt.geom.Rectangle2D;
import java.text.DecimalFormat;

/**
 * Base rendering engine that handles grid, axes, labels, crosshair, and delegates
 * to chart-type-specific renderers. All paint operations go through this class.
 */
public abstract class ChartRenderer {

    protected final ChartModel model;
    protected final DecimalFormat priceFormat = new DecimalFormat("#,##0.00");
    protected final DecimalFormat volumeFormat = new DecimalFormat("#,##0");
    protected final DecimalFormat compactFormat = new DecimalFormat("#,##0.#");
    protected Font titleFont = new Font("SansSerif", Font.BOLD, 14);
    protected Font labelFont = new Font("SansSerif", Font.PLAIN, 10);
    protected Font smallFont = new Font("SansSerif", Font.PLAIN, 9);
    protected Font crosshairFont = new Font("Monospaced", Font.PLAIN, 11);
    protected Stroke gridStroke = new BasicStroke(0.5f);
    protected Stroke axisStroke = new BasicStroke(1.0f);

    protected ChartRenderer(ChartModel model) {
        this.model = model;
    }

    public final void render(Graphics2D g, int width, int height) {
        g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
        g.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);
        g.setRenderingHint(RenderingHints.KEY_RENDERING, RenderingHints.VALUE_RENDER_QUALITY);

        int chartW = model.getChartWidth(width);
        int chartH = model.getChartHeight(height);

        drawBackground(g, width, height);
        if (model.isShowGrid()) drawGrid(g, chartW, chartH, width, height);
        drawChartContent(g, chartW, chartH, width, height);
        if (model.isShowVolume()) drawVolume(g, chartW, chartH, width, height);
        drawDrawings(g, chartW, chartH, width, height);
        drawIndicators(g, chartW, chartH, width, height);
        if (model.isShowPriceAxis()) drawPriceAxis(g, chartW, chartH, width, height);
        if (model.isShowTimeAxis()) drawTimeAxis(g, chartW, chartH, width, height);
        if (model.isShowLegend()) drawLegend(g, width, height);
        drawTitle(g, width);
    }

    protected void drawBackground(Graphics2D g, int width, int height) {
        g.setColor(ChartColor.BACKGROUND);
        g.fillRect(0, 0, width, height);
    }

    protected void drawGrid(Graphics2D g, int chartW, int chartH, int width, int height) {
        g.setColor(ChartColor.GRID_LINE);
        g.setStroke(gridStroke);
        int priceH = model.getPriceAreaHeight(chartH);
        int volTop = model.getTopMargin() + priceH;

        // Horizontal price grid lines
        double range = model.getMaxPrice() - model.getMinPrice();
        if (range > 0) {
            double step = niceStep(range, 8);
            double start = Math.ceil(model.getMinPrice() / step) * step;
            for (double price = start; price <= model.getMaxPrice(); price += step) {
                double y = model.priceToY(price, chartH);
                g.drawLine(model.getLeftMargin(), (int) y, model.getLeftMargin() + chartW, (int) y);
            }
        }

        // Vertical time grid lines
        int visibleBars = model.getVisibleCount();
        int timeStep = Math.max(1, visibleBars / 8);
        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i += timeStep) {
            double x = model.barX(i, chartW);
            g.drawLine((int) x, model.getTopMargin(), (int) x, volTop);
        }
    }

    protected void drawPriceAxis(Graphics2D g, int chartW, int chartH, int width, int height) {
        g.setColor(ChartColor.TEXT_DIM);
        g.setFont(labelFont);
        g.setStroke(axisStroke);
        int priceH = model.getPriceAreaHeight(chartH);
        int xAxis = model.getLeftMargin() + chartW;

        g.drawLine(xAxis, model.getTopMargin(), xAxis, model.getTopMargin() + priceH);

        double range = model.getMaxPrice() - model.getMinPrice();
        if (range > 0) {
            double step = niceStep(range, 8);
            double start = Math.ceil(model.getMinPrice() / step) * step;
            for (double price = start; price <= model.getMaxPrice(); price += step) {
                double y = model.priceToY(price, chartH);
                g.drawString(priceFormat.format(price), xAxis + 5, (int) y + 4);
            }
        }
    }

    protected void drawTimeAxis(Graphics2D g, int chartW, int chartH, int width, int height) {
        g.setColor(ChartColor.TEXT_DIM);
        g.setFont(labelFont);
        int volTop = model.getTopMargin() + model.getPriceAreaHeight(chartH);
        g.drawLine(model.getLeftMargin(), volTop, model.getLeftMargin() + chartW, volTop);

        int visibleBars = model.getVisibleCount();
        int timeStep = Math.max(1, visibleBars / 8);
        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i += timeStep) {
            double x = model.barX(i, chartW);
            String timeStr = model.getData().getBar(i).getTimeString();
            g.drawString(timeStr, (int) x - 25, volTop + 15);
        }
    }

    protected void drawVolume(Graphics2D g, int chartW, int chartH, int width, int height) {
        int priceH = model.getPriceAreaHeight(chartH);
        int volH = model.getVolumeAreaHeight(chartH);
        int volTop = model.getTopMargin() + priceH;
        double maxVol = model.getMaxVolume();
        double barW = model.getBarWidth(chartW);

        for (int i = model.getVisibleStart(); i < model.getVisibleEnd(); i++) {
            com.jcharts.data.OHLCBar bar = model.getData().getBar(i);
            double x = model.barX(i, chartW);
            double h = maxVol > 0 ? (bar.getVolume() / maxVol) * volH : 0;
            g.setColor(bar.isBullish() ? ChartColor.VOLUME_BULL : ChartColor.VOLUME_BEAR);
            g.fillRect((int) (x - barW / 2), volTop + volH - (int) h, (int) barW, (int) h);
        }
    }

    protected void drawTitle(Graphics2D g, int width) {
        if (model.getTitle() == null || model.getTitle().isEmpty()) return;
        g.setColor(ChartColor.TEXT);
        g.setFont(titleFont);
        g.drawString(model.getTitle(), model.getLeftMargin() + 5, model.getTopMargin() - 12);
        if (model.getSubtitle() != null && !model.getSubtitle().isEmpty()) {
            g.setColor(ChartColor.TEXT_DIM);
            g.setFont(smallFont);
            g.drawString(model.getSubtitle(), model.getLeftMargin() + 5 + g.getFontMetrics().stringWidth(model.getTitle()) + 10, model.getTopMargin() - 12);
        }
    }

    protected void drawLegend(Graphics2D g, int width, int height) {
        TimeSeries data = model.getData();
        if (data.isEmpty()) return;
        int lastIdx = Math.min(model.getVisibleEnd() - 1, data.size() - 1);
        if (lastIdx < 0) return;
        com.jcharts.data.OHLCBar last = data.getBar(lastIdx);
        String info = String.format("O:%.2f H:%.2f L:%.2f C:%.2f V:%s",
                last.getOpen(), last.getHigh(), last.getLow(), last.getClose(),
                last.getVolume() >= 1e6 ? compactFormat.format(last.getVolume() / 1e6) + "M" :
                last.getVolume() >= 1e3 ? compactFormat.format(last.getVolume() / 1e3) + "K" :
                String.valueOf((long) last.getVolume()));
        g.setColor(last.isBullish() ? ChartColor.BULLISH : ChartColor.BEARISH);
        g.setFont(labelFont);
        g.drawString(info, model.getLeftMargin() + 5, model.getTopMargin() - 1);
    }

    protected void drawDrawings(Graphics2D g, int chartW, int chartH, int width, int height) {
        for (com.jcharts.tools.DrawingTool tool : model.getDrawings()) {
            tool.draw(g, model, chartW, chartH);
        }
    }

    protected void drawIndicators(Graphics2D g, int chartW, int chartH, int width, int height) {
        for (com.jcharts.indicators.Indicator ind : model.getIndicators()) {
            ind.draw(g, model, chartW, chartH);
        }
    }

    public void drawCrosshair(Graphics2D g, int mouseX, int mouseY, int width, int height) {
        if (!model.isShowCrosshair() || mouseX < 0 || mouseY < 0) return;
        int chartW = model.getChartWidth(width);
        int chartH = model.getChartHeight(height);

        g.setColor(ChartColor.CROSSHAIR);
        g.setStroke(new BasicStroke(0.5f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_BEVEL,
                10.0f, new float[]{5.0f, 5.0f}, 0.0f));

        // Horizontal line
        g.drawLine(model.getLeftMargin(), mouseY, model.getLeftMargin() + chartW, mouseY);
        // Vertical line
        g.drawLine(mouseX, model.getTopMargin(), mouseX, model.getTopMargin() + chartH);

        // Price label
        double price = model.yToPrice(mouseY, chartH);
        g.setStroke(new BasicStroke(1.0f));
        g.setColor(ChartColor.ACCENT);
        g.fillRect(model.getLeftMargin() + chartW, mouseY - 9, model.getRightMargin(), 18);
        g.setColor(ChartColor.WHITE);
        g.setFont(crosshairFont);
        g.drawString(priceFormat.format(price), model.getLeftMargin() + chartW + 4, mouseY + 4);

        // Time label
        int barIdx = model.yToBarIndex(mouseX, chartW);
        if (barIdx >= 0 && barIdx < model.getData().size()) {
            String timeStr = model.getData().getBar(barIdx).getTimeString();
            int tw = g.getFontMetrics().stringWidth(timeStr) + 8;
            g.setColor(ChartColor.ACCENT);
            g.fillRect(mouseX - tw / 2, model.getTopMargin() + chartH, tw, 20);
            g.setColor(ChartColor.WHITE);
            g.drawString(timeStr, mouseX - tw / 2 + 4, model.getTopMargin() + chartH + 14);
        }
    }

    protected abstract void drawChartContent(Graphics2D g, int chartW, int chartH, int width, int height);

    public abstract String getChartTypeName();

    protected double niceStep(double range, int targetSteps) {
        double rough = range / targetSteps;
        double mag = Math.pow(10, Math.floor(Math.log10(rough)));
        double residual = rough / mag;
        double nice;
        if (residual <= 1.5) nice = 1;
        else if (residual <= 3) nice = 2;
        else if (residual <= 7) nice = 5;
        else nice = 10;
        return nice * mag;
    }
}
