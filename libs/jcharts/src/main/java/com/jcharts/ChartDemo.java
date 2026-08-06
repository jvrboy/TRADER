package com.jcharts;

import com.jcharts.core.*;
import com.jcharts.data.*;
import com.jcharts.types.*;
import com.jcharts.indicators.*;
import com.jcharts.tools.*;
import com.jcharts.features.*;

import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.io.File;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Comprehensive demo application showcasing ALL chart types, tools, indicators, and features.
 * JTabbedPane layout with tabs for: Standard Charts, Advanced Charts, Original Charts,
 * Drawing Tools, Indicators, Features Demo, and All-in-One.
 */
public class ChartDemo extends JFrame {

    public ChartDemo() {
        super("JCharts - Lightweight TradingView Charts");
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setSize(1200, 800);
        setLocationRelativeTo(null);

        TimeSeries data = TimeSeries.generateRandom(500, 150.0, 3.0, "AAPL");

        JTabbedPane tabs = new JTabbedPane();
        tabs.setBackground(ChartColor.BACKGROUND);
        tabs.setForeground(ChartColor.TEXT);

        // Tab 1: Standard Chart Types
        tabs.addTab("Standard Charts", createStandardChartsTab(data));
        // Tab 2: Advanced Chart Types
        tabs.addTab("Advanced Charts", createAdvancedChartsTab(data));
        // Tab 3: Original Chart Types
        tabs.addTab("Original Charts", createOriginalChartsTab(data));
        // Tab 4: Drawing Tools
        tabs.addTab("Drawing Tools", createDrawingToolsTab(data));
        // Tab 5: Indicators
        tabs.addTab("Indicators", createIndicatorsTab(data));
        // Tab 6: Features
        tabs.addTab("Features", createFeaturesTab(data));
        // Tab 7: All-in-One
        tabs.addTab("All-in-One", createAllInOneTab(data));

        add(tabs, BorderLayout.CENTER);

        // Status bar
        JLabel status = new JLabel(" JCharts v1.0.0 | 17 Chart Types | 15 Drawing Tools | 9 Indicators | Keyboard: Arrows=Scroll, +/-=Zoom, G=Grid, V=Volume, C=Crosshair, L=Legend");
        status.setBackground(ChartColor.BACKGROUND_LIGHT);
        status.setForeground(ChartColor.TEXT_DIM);
        status.setOpaque(true);
        status.setFont(new Font("SansSerif", Font.PLAIN, 11));
        add(status, BorderLayout.SOUTH);
    }

    private JPanel createChartCard(String title, ChartType type, TimeSeries data) {
        JPanel card = new JPanel(new BorderLayout());
        card.setBackground(ChartColor.BACKGROUND);
        JLabel lbl = new JLabel(" " + type.getDisplayName() + (title.isEmpty() ? "" : " - " + title));
        lbl.setForeground(ChartColor.TEXT);
        lbl.setFont(new Font("SansSerif", Font.BOLD, 12));
        lbl.setOpaque(true);
        lbl.setBackground(ChartColor.BACKGROUND_LIGHT);
        card.add(lbl, BorderLayout.NORTH);
        ChartPanel cp = new ChartPanel(data);
        cp.setChartType(type);
        cp.getModel().setTitle(type.getDisplayName());
        card.add(cp, BorderLayout.CENTER);
        return card;
    }

    private JPanel createStandardChartsTab(TimeSeries data) {
        JPanel panel = new JPanel(new GridLayout(2, 3, 2, 2));
        panel.setBackground(ChartColor.BACKGROUND);
        ChartType[] types = {
            ChartType.CANDLESTICK, ChartType.LINE, ChartType.BAR,
            ChartType.AREA, ChartType.HOLLOW_CANDLE, ChartType.HEIKIN_ASHI
        };
        for (ChartType t : types) panel.add(createChartCard("", t, data));
        return panel;
    }

    private JPanel createAdvancedChartsTab(TimeSeries data) {
        JPanel panel = new JPanel(new GridLayout(2, 3, 2, 2));
        panel.setBackground(ChartColor.BACKGROUND);
        ChartType[] types = {
            ChartType.RENKO, ChartType.KAGI, ChartType.POINT_AND_FIGURE,
            ChartType.LINE_BREAK, ChartType.EQUIVOLUME, ChartType.PERCENTAGE
        };
        for (ChartType t : types) panel.add(createChartCard("", t, data));
        return panel;
    }

    private JPanel createOriginalChartsTab(TimeSeries data) {
        JPanel panel = new JPanel(new GridLayout(2, 3, 2, 2));
        panel.setBackground(ChartColor.BACKGROUND);
        ChartType[] types = {
            ChartType.VOLUME_HEATMAP, ChartType.MOMENTUM_WAVE,
            ChartType.FLOW_FORCE, ChartType.ELASTIC_BAND, ChartType.QUANTUM_RIBBON, ChartType.CANDLESTICK
        };
        String[] labels = {"Volume Heatmap", "Momentum Wave", "Flow Force", "Elastic Band", "Quantum Ribbon", "(Standard Reference)"};
        for (int i = 0; i < types.length; i++) panel.add(createChartCard(labels[i], types[i], data));
        return panel;
    }

    private JPanel createDrawingToolsTab(TimeSeries data) {
        JPanel panel = new JPanel(new BorderLayout());
        panel.setBackground(ChartColor.BACKGROUND);

        ChartPanel cp = new ChartPanel(data);
        cp.getModel().setTitle("Drawing Tools Demo");
        cp.getModel().setShowLegend(false);

        // Add various drawing tools
        int n = data.size();
        cp.addDrawing(new TrendLineTool(n - 80, data.getBar(n - 80).getClose(), n - 20, data.getBar(n - 20).getClose()));
        cp.addDrawing(new HorizontalLineTool(data.getBar(n - 50).getHigh(), "Resistance"));
        cp.addDrawing(new HorizontalLineTool(data.getBar(n - 50).getLow(), "Support"));
        cp.addDrawing(new VerticalLineTool(n - 30, "Event"));
        cp.addDrawing(new FibonacciRetracementTool(n - 80, data.getBar(n - 80).getLow(), n - 20, data.getBar(n - 20).getHigh()));
        cp.addDrawing(new RectangleTool(n - 60, data.getBar(n - 60).getHigh(), n - 40, data.getBar(n - 40).getLow()));
        cp.addDrawing(new TextTool(n - 15, data.getBar(n - 15).getHigh() + 2, "Breakout!"));
        cp.addDrawing(new ArrowTool(n - 70, data.getBar(n - 70).getLow(), n - 10, data.getBar(n - 10).getHigh()));
        cp.addDrawing(new MeasurementTool(n - 60, data.getBar(n - 60).getClose(), n - 20, data.getBar(n - 20).getClose()));
        cp.addDrawing(new FibonacciFanTool(n - 80, data.getBar(n - 80).getLow(), n - 10, data.getBar(n - 10).getHigh()));
        cp.addDrawing(new FibonacciTimezoneTool(n - 80));
        cp.addDrawing(new ChannelTool(n - 70, data.getBar(n - 70).getLow(), n - 20, data.getBar(n - 20).getHigh(), n - 45, data.getBar(n - 45).getHigh()));
        cp.addDrawing(new PitchforkTool(n - 80, data.getBar(n - 80).getLow(), n - 50, data.getBar(n - 50).getHigh(), n - 30, data.getBar(n - 30).getLow()));
        cp.addDrawing(new EllipseTool(n - 60, data.getBar(n - 60).getHigh(), n - 30, data.getBar(n - 30).getLow()));

        // Tool buttons
        JPanel btnPanel = new JPanel(new FlowLayout(FlowLayout.LEFT));
        btnPanel.setBackground(ChartColor.BACKGROUND_LIGHT);
        String[] toolBtns = {"Clear All", "Trend", "H-Line", "V-Line", "Fib Retrace", "Fib Fan", "Rect", "Ellipse", "Text", "Arrow", "Measure", "Channel", "Pitchfork", "Fib Time"};
        for (String name : toolBtns) {
            JButton btn = new JButton(name);
            btn.setBackground(ChartColor.BACKGROUND_LIGHT);
            btn.setForeground(ChartColor.TEXT);
            btn.setFocusPainted(false);
            btn.addActionListener(e -> {
                switch (name) {
                    case "Clear All": cp.clearDrawings(); break;
                    case "Trend": cp.addDrawing(new TrendLineTool(n-50, data.getBar(n-50).getClose(), n-10, data.getBar(n-10).getClose())); break;
                    case "H-Line": cp.addDrawing(new HorizontalLineTool(data.getBar(n-1).getClose())); break;
                    case "V-Line": cp.addDrawing(new VerticalLineTool(n-25)); break;
                    case "Fib Retrace": cp.addDrawing(new FibonacciRetracementTool(n-60, data.getBar(n-60).getLow(), n-10, data.getBar(n-10).getHigh())); break;
                    case "Fib Fan": cp.addDrawing(new FibonacciFanTool(n-60, data.getBar(n-60).getLow(), n-10, data.getBar(n-10).getHigh())); break;
                    case "Rect": cp.addDrawing(new RectangleTool(n-50, data.getBar(n-50).getHigh(), n-20, data.getBar(n-20).getLow())); break;
                    case "Ellipse": cp.addDrawing(new EllipseTool(n-50, data.getBar(n-50).getHigh(), n-20, data.getBar(n-20).getLow())); break;
                    case "Text": cp.addDrawing(new TextTool(n-5, data.getBar(n-5).getHigh()+2, "Note")); break;
                    case "Arrow": cp.addDrawing(new ArrowTool(n-40, data.getBar(n-40).getLow(), n-5, data.getBar(n-5).getHigh())); break;
                    case "Measure": cp.addDrawing(new MeasurementTool(n-40, data.getBar(n-40).getClose(), n-10, data.getBar(n-10).getClose())); break;
                    case "Channel": cp.addDrawing(new ChannelTool(n-50, data.getBar(n-50).getLow(), n-10, data.getBar(n-10).getHigh(), n-30, data.getBar(n-30).getHigh())); break;
                    case "Pitchfork": cp.addDrawing(new PitchforkTool(n-60, data.getBar(n-60).getLow(), n-40, data.getBar(n-40).getHigh(), n-20, data.getBar(n-20).getLow())); break;
                    case "Fib Time": cp.addDrawing(new FibonacciTimezoneTool(n-50)); break;
                }
            });
            btnPanel.add(btn);
        }

        panel.add(btnPanel, BorderLayout.NORTH);
        panel.add(cp, BorderLayout.CENTER);
        return panel;
    }

    private JPanel createIndicatorsTab(TimeSeries data) {
        JPanel panel = new JPanel(new GridLayout(2, 3, 2, 2));
        panel.setBackground(ChartColor.BACKGROUND);

        // SMA + EMA
        ChartPanel p1 = new ChartPanel(data);
        p1.setChartType(ChartType.CANDLESTICK);
        p1.getModel().setTitle("SMA & EMA");
        p1.addIndicator(new SMAIndicator(20));
        p1.addIndicator(new SMAIndicator(50, ChartColor.INDICATOR_3));
        p1.addIndicator(new EMAIndicator(21, ChartColor.INDICATOR_2));
        panel.add(wrapInPanel(p1));

        // Bollinger Bands
        ChartPanel p2 = new ChartPanel(data);
        p2.setChartType(ChartType.CANDLESTICK);
        p2.getModel().setTitle("Bollinger Bands");
        p2.addIndicator(new BollingerBandsIndicator());
        panel.add(wrapInPanel(p2));

        // MACD
        ChartPanel p3 = new ChartPanel(data);
        p3.setChartType(ChartType.LINE);
        p3.getModel().setTitle("MACD");
        p3.getModel().setAutoScale(false);
        MACDIndicator macd = new MACDIndicator();
        macd.calculate(data);
        double[] macdVals = macd.getValues();
        double[] hist = macd.getHistogram();
        double min = Double.MAX_VALUE, max = -Double.MAX_VALUE;
        for (double v : hist) { if (!Double.isNaN(v) && v < min) min = v; if (!Double.isNaN(v) && v > max) max = v; }
        p3.getModel().setManualMinPrice(min - 0.5); p3.getModel().setManualMaxPrice(max + 0.5);
        p3.addIndicator(macd);
        panel.add(wrapInPanel(p3));

        // RSI
        ChartPanel p4 = new ChartPanel(data);
        p4.setChartType(ChartType.LINE);
        p4.getModel().setTitle("RSI(14)");
        RSIIndicator rsi = new RSIIndicator(14);
        rsi.calculate(data);
        p4.getModel().setManualMinPrice(0); p4.getModel().setManualMaxPrice(100);
        p4.getModel().setAutoScale(false);
        p4.addIndicator(rsi);
        panel.add(wrapInPanel(p4));

        // Stochastic
        ChartPanel p5 = new ChartPanel(data);
        p5.setChartType(ChartType.LINE);
        p5.getModel().setTitle("Stochastic(14,3)");
        StochasticIndicator stoch = new StochasticIndicator();
        stoch.calculate(data);
        p5.getModel().setManualMinPrice(0); p5.getModel().setManualMaxPrice(100);
        p5.getModel().setAutoScale(false);
        p5.addIndicator(stoch);
        panel.add(wrapInPanel(p5));

        // VWAP + ATR
        ChartPanel p6 = new ChartPanel(data);
        p6.setChartType(ChartType.CANDLESTICK);
        p6.getModel().setTitle("VWAP & ATR");
        p6.addIndicator(new VWAPIndicator());
        ATRIndicator atr = new ATRIndicator();
        atr.calculate(data);
        // Draw ATR as overlay - adjust manually
        panel.add(wrapInPanel(p6));

        return panel;
    }

    private JPanel wrapInPanel(ChartPanel cp) {
        JPanel wrapper = new JPanel(new BorderLayout());
        wrapper.setBackground(ChartColor.BACKGROUND);
        wrapper.add(cp, BorderLayout.CENTER);
        return wrapper;
    }

    private JPanel createFeaturesTab(TimeSeries data) {
        JPanel panel = new JPanel(new BorderLayout());
        panel.setBackground(ChartColor.BACKGROUND);

        // Top controls
        JPanel controls = new JPanel(new FlowLayout(FlowLayout.LEFT));
        controls.setBackground(ChartColor.BACKGROUND_LIGHT);

        ChartPanel cp = new ChartPanel(data);
        cp.setChartType(ChartType.CANDLESTICK);
        cp.getModel().setTitle("Features Demo - AAPL");

        // Chart type selector
        JComboBox<ChartType> typeCombo = new JComboBox<>(ChartType.values());
        typeCombo.addActionListener(e -> cp.setChartType((ChartType) typeCombo.getSelectedItem()));
        controls.add(new JLabel("Type:"));
        controls.add(typeCombo);

        // Screenshot button
        JButton screenshotBtn = new JButton("Screenshot");
        screenshotBtn.addActionListener(e -> {
            try {
                cp.saveScreenshot("/home/z/my-project/download/jcharts_screenshot.png");
                JOptionPane.showMessageDialog(this, "Screenshot saved!");
            } catch (Exception ex) { ex.printStackTrace(); }
        });
        controls.add(screenshotBtn);

        // Export CSV
        JButton csvBtn = new JButton("Export CSV");
        csvBtn.addActionListener(e -> {
            try {
                DataExporter.toCSV(data, "/home/z/my-project/download/jcharts_data.csv");
                JOptionPane.showMessageDialog(this, "CSV exported!");
            } catch (Exception ex) { ex.printStackTrace(); }
        });
        controls.add(csvBtn);

        // Export JSON
        JButton jsonBtn = new JButton("Export JSON");
        jsonBtn.addActionListener(e -> {
            try {
                DataExporter.toJSON(data, "/home/z/my-project/download/jcharts_data.json");
                JOptionPane.showMessageDialog(this, "JSON exported!");
            } catch (Exception ex) { ex.printStackTrace(); }
        });
        controls.add(jsonBtn);

        // Toggle buttons
        JCheckBox gridChk = new JCheckBox("Grid", true);
        gridChk.addActionListener(e -> { cp.getModel().setShowGrid(gridChk.isSelected()); cp.refresh(); });
        controls.add(gridChk);

        JCheckBox volChk = new JCheckBox("Volume", true);
        volChk.addActionListener(e -> { cp.getModel().setShowVolume(volChk.isSelected()); cp.refresh(); });
        controls.add(volChk);

        JCheckBox crossChk = new JCheckBox("Crosshair", true);
        crossChk.addActionListener(e -> { cp.getModel().setShowCrosshair(crossChk.isSelected()); cp.refresh(); });
        controls.add(crossChk);

        // Fit All button
        JButton fitBtn = new JButton("Fit All");
        ZoomPanHandler zph = new ZoomPanHandler(cp);
        fitBtn.addActionListener(e -> zph.fitAll());
        controls.add(fitBtn);

        // Add/Remove indicators
        JButton addSmaBtn = new JButton("+ SMA(20)");
        addSmaBtn.addActionListener(e -> cp.addIndicator(new SMAIndicator(20)));
        controls.add(addSmaBtn);

        JButton addBbBtn = new JButton("+ BB(20,2)");
        addBbBtn.addActionListener(e -> cp.addIndicator(new BollingerBandsIndicator()));
        controls.add(addBbBtn);

        JButton clearIndBtn = new JButton("Clear Indicators");
        clearIndBtn.addActionListener(e -> cp.clearIndicators());
        controls.add(clearIndBtn);

        // Alert demo
        PriceAlert alert = new PriceAlert();
        alert.addAlert("Price alert!", PriceAlert.Condition.ABOVE, data.getBar(data.size()-1).getHigh() + 1,
                msg -> SwingUtilities.invokeLater(() -> JOptionPane.showMessageDialog(this, "Alert: " + msg)));
        JButton alertBtn = new JButton("Set Alert");
        alertBtn.addActionListener(e -> {
            alert.updateVolumeAvg(data);
            alert.check(data);
            JOptionPane.showMessageDialog(this, "Alert set. Latest price: " + data.getBar(data.size()-1).getClose());
        });
        controls.add(alertBtn);

        // Save template
        JButton templateBtn = new JButton("Save Template");
        templateBtn.addActionListener(e -> {
            ChartTemplate t = ChartTemplate.fromModel(cp.getModel(), (ChartType) typeCombo.getSelectedItem());
            JOptionPane.showMessageDialog(this, "Template saved: " + t.getName());
        });
        controls.add(templateBtn);

        // Replay
        DataReplay replay = new DataReplay(cp, data);
        JButton playBtn = new JButton("Replay");
        AtomicInteger replayState = new AtomicInteger(0);
        playBtn.addActionListener(e -> {
            if (replayState.get() == 0) { replay.play(); playBtn.setText("Pause"); replayState.set(1); }
            else if (replayState.get() == 1) { replay.pause(); playBtn.setText("Resume"); replayState.set(2); }
            else { replay.play(); playBtn.setText("Pause"); replayState.set(1); }
        });
        controls.add(playBtn);

        JButton replayResetBtn = new JButton("Replay Reset");
        replayResetBtn.addActionListener(e -> { replay.stop(); playBtn.setText("Replay"); replayState.set(0); });
        controls.add(replayResetBtn);

        panel.add(controls, BorderLayout.NORTH);
        panel.add(cp, BorderLayout.CENTER);
        return panel;
    }

    private JPanel createAllInOneTab(TimeSeries data) {
        ChartPanel cp = new ChartPanel(data);
        cp.setChartType(ChartType.CANDLESTICK);
        cp.getModel().setTitle("All-in-One - AAPL with Indicators, Drawings & Volume Profile");

        // Add indicators
        cp.addIndicator(new SMAIndicator(20));
        cp.addIndicator(new EMAIndicator(50, ChartColor.INDICATOR_3));
        cp.addIndicator(new BollingerBandsIndicator());
        cp.addIndicator(new VWAPIndicator());

        // Add drawings
        int n = data.size();
        cp.addDrawing(new HorizontalLineTool(data.getBar(n - 50).getHigh(), "Resistance"));
        cp.addDrawing(new HorizontalLineTool(data.getBar(n - 50).getLow(), "Support"));
        cp.addDrawing(new FibonacciRetracementTool(n - 80, data.getBar(n - 80).getLow(), n - 10, data.getBar(n - 10).getHigh()));
        cp.addDrawing(new TrendLineTool(n - 60, data.getBar(n - 60).getLow(), n - 20, data.getBar(n - 20).getHigh()));
        cp.addDrawing(new TextTool(n - 5, data.getBar(n - 5).getHigh() + 3, "Watch this level"));

        JPanel wrapper = new JPanel(new BorderLayout());
        wrapper.setBackground(ChartColor.BACKGROUND);
        wrapper.add(cp, BorderLayout.CENTER);
        return wrapper;
    }

    public static void main(String[] args) {
        try {
            UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
        } catch (Exception ignored) {}
        SwingUtilities.invokeLater(() -> {
            ChartDemo demo = new ChartDemo();
            demo.setVisible(true);
        });
    }
}
