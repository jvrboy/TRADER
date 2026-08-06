package com.jcharts.tools;

import com.jcharts.core.ChartModel;
import com.jcharts.core.ChartColor;
import java.awt.*;

/** Text annotation placed at a bar-index/price position. */
public class TextTool extends AbstractDrawingTool {
    private final int barIndex;
    private final double price;
    private final String text;
    private final Font font;

    public TextTool(int barIndex, double price, String text) {
        super("Text Annotation");
        this.barIndex = barIndex; this.price = price; this.text = text;
        this.font = new Font("SansSerif", Font.PLAIN, 12);
    }

    public TextTool(int barIndex, double price, String text, Font font) {
        super("Text Annotation");
        this.barIndex = barIndex; this.price = price; this.text = text; this.font = font;
    }

    @Override
    public void draw(Graphics2D g, ChartModel model, int chartW, int chartH) {
        if (barIndex < model.getVisibleStart() || barIndex >= model.getVisibleEnd()) return;
        double x = model.barX(barIndex, chartW);
        double y = model.priceToY(price, chartH);
        g.setFont(font);
        FontMetrics fm = g.getFontMetrics();
        int tw = fm.stringWidth(text) + 10;
        int th = fm.getHeight() + 6;
        g.setColor(new Color(0, 0, 0, 160));
        g.fillRect((int) x - 5, (int) y - th / 2, tw, th);
        g.setColor(color);
        g.drawRect((int) x - 5, (int) y - th / 2, tw, th);
        g.setColor(Color.WHITE);
        g.drawString(text, (int) x, (int) y + 4);
        if (selected) {
            g.setColor(ChartColor.YELLOW);
            g.drawRect((int) x - 6, (int) y - th / 2 - 1, tw + 2, th + 2);
        }
    }

    @Override public double[] getBounds() { return new double[]{barIndex, price, barIndex, price}; }
    public String getText() { return text; }
}