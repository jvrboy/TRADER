package com.jcharts.core;

import com.jcharts.data.TimeSeries;
import com.jcharts.indicators.Indicator;
import com.jcharts.tools.DrawingTool;
import com.jcharts.types.ChartType;
import com.jcharts.types.CandlestickRenderer;

import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.image.BufferedImage;
import java.io.File;
import javax.imageio.ImageIO;
import java.util.ArrayList;
import java.util.List;

/**
 * Main Swing panel that hosts the chart. Handles mouse interactions (zoom, pan, crosshair),
 * keyboard shortcuts, and delegates rendering to the current ChartRenderer.
 */
public class ChartPanel extends JPanel {
    private final ChartModel model;
    private ChartRenderer renderer;
    private int mouseX = -1, mouseY = -1;
    private boolean isDragging = false;
    private int dragStartX = 0;
    private int dragStartStart = 0;
    private final List<Runnable> listeners = new ArrayList<>();

    public ChartPanel() {
        this.model = new ChartModel();
        this.renderer = new CandlestickRenderer(model);
        initUI();
    }

    public ChartPanel(TimeSeries data) {
        this.model = new ChartModel(data);
        this.renderer = new CandlestickRenderer(model);
        initUI();
    }

    private void initUI() {
        setOpaque(true);
        setBackground(ChartColor.BACKGROUND);
        setPreferredSize(new Dimension(900, 500));
        // Mouse listeners
        addMouseMotionListener(new MouseMotionAdapter() {
            @Override public void mouseMoved(MouseEvent e) { mouseX = e.getX(); mouseY = e.getY(); repaint(); }
            @Override public void mouseDragged(MouseEvent e) {
                if (isDragging) {
                    int dx = e.getX() - dragStartX;
                    int barsScrolled = dx / (getWidth() / model.getVisibleCount());
                    model.setVisibleRange(dragStartStart - barsScrolled, model.getVisibleCount());
                    repaint();
                }
            }
        });
        addMouseListener(new MouseAdapter() {
            @Override public void mousePressed(MouseEvent e) {
                if (e.getButton() == MouseEvent.BUTTON1) {
                    isDragging = true; dragStartX = e.getX(); dragStartStart = model.getVisibleStart();
                }
            }
            @Override public void mouseReleased(MouseEvent e) { isDragging = false; }
            @Override public void mouseExited(MouseEvent e) { mouseX = -1; mouseY = -1; repaint(); }
        });
        addMouseWheelListener(e -> {
            if (e.getWheelRotation() > 0) model.zoomOut(); else model.zoomIn();
            repaint();
        });
        // Keyboard
        setFocusable(true);
        addKeyListener(new KeyAdapter() {
            @Override public void keyPressed(KeyEvent e) {
                switch (e.getKeyCode()) {
                    case KeyEvent.VK_LEFT: model.scrollLeft(Math.max(1, model.getVisibleCount() / 10)); repaint(); break;
                    case KeyEvent.VK_RIGHT: model.scrollRight(Math.max(1, model.getVisibleCount() / 10)); repaint(); break;
                    case KeyEvent.VK_PLUS: case KeyEvent.VK_EQUALS: model.zoomIn(); repaint(); break;
                    case KeyEvent.VK_MINUS: model.zoomOut(); repaint(); break;
                    case KeyEvent.VK_G: model.setShowGrid(!model.isShowGrid()); repaint(); break;
                    case KeyEvent.VK_V: model.setShowVolume(!model.isShowVolume()); repaint(); break;
                    case KeyEvent.VK_C: model.setShowCrosshair(!model.isShowCrosshair()); repaint(); break;
                    case KeyEvent.VK_L: model.setShowLegend(!model.isShowLegend()); repaint(); break;
                    case KeyEvent.VK_HOME: model.setVisibleRange(0, Math.min(100, model.getData().size())); repaint(); break;
                    case KeyEvent.VK_END: int n = model.getData().size(); model.setVisibleRange(Math.max(0, n - 100), 100); repaint(); break;
                }
            }
        });
    }

    @Override
    protected void paintComponent(Graphics g) {
        super.paintComponent(g);
        Graphics2D g2 = (Graphics2D) g;
        renderer.render(g2, getWidth(), getHeight());
        if (!isDragging) renderer.drawCrosshair(g2, mouseX, mouseY, getWidth(), getHeight());
    }

    public ChartModel getModel() { return model; }
    public ChartRenderer getRenderer() { return renderer; }

    public void setChartType(ChartType type) {
        renderer = type.createRenderer(model);
        repaint();
    }

    public void setData(TimeSeries data) {
        model.setData(data);
        for (Indicator ind : model.getIndicators()) ind.calculate(data);
        repaint();
    }

    public void addIndicator(Indicator ind) {
        ind.calculate(model.getData());
        model.addIndicator(ind);
        repaint();
    }

    public void removeIndicator(Indicator ind) { model.removeIndicator(ind); repaint(); }
    public void clearIndicators() { model.clearIndicators(); repaint(); }

    public void addDrawing(DrawingTool tool) { model.addDrawing(tool); repaint(); }
    public void removeDrawing(DrawingTool tool) { model.removeDrawing(tool); repaint(); }
    public void clearDrawings() { model.clearDrawings(); repaint(); }

    public BufferedImage takeScreenshot() {
        BufferedImage img = new BufferedImage(getWidth(), getHeight(), BufferedImage.TYPE_INT_ARGB);
        Graphics2D g2 = img.createGraphics();
        renderer.render(g2, getWidth(), getHeight());
        g2.dispose();
        return img;
    }

    public void saveScreenshot(String filePath) throws Exception {
        ImageIO.write(takeScreenshot(), "PNG", new File(filePath));
    }

    public void addChangeListener(Runnable r) { listeners.add(r); }
    public void refresh() { repaint(); }
}
