package com.jcharts.features;

import com.jcharts.data.OHLCBar;
import com.jcharts.data.TimeSeries;
import java.io.*;
import java.util.ArrayList;
import java.util.List;

/** Exports chart data to CSV and JSON formats. */
public class DataExporter {

    public static void toCSV(TimeSeries data, String filePath) throws IOException {
        try (PrintWriter pw = new PrintWriter(new FileWriter(filePath))) {
            pw.println("Timestamp,Open,High,Low,Close,Volume");
            for (OHLCBar bar : data.getBars()) {
                pw.printf("%s,%.4f,%.4f,%.4f,%.4f,%.0f%n",
                        bar.getTimeString(), bar.getOpen(), bar.getHigh(), bar.getLow(), bar.getClose(), bar.getVolume());
            }
        }
    }

    public static void toJSON(TimeSeries data, String filePath) throws IOException {
        try (PrintWriter pw = new PrintWriter(new FileWriter(filePath))) {
            pw.println("[");
            List<OHLCBar> bars = data.getBars();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bars.size(); i++) {
                OHLCBar b = bars.get(i);
                sb.setLength(0);
                sb.append("  {\"t\":\"").append(b.getTimeString()).append("\"");
                sb.append(",\"o\":").append(String.format("%.4f", b.getOpen()));
                sb.append(",\"h\":").append(String.format("%.4f", b.getHigh()));
                sb.append(",\"l\":").append(String.format("%.4f", b.getLow()));
                sb.append(",\"c\":").append(String.format("%.4f", b.getClose()));
                sb.append(",\"v\":").append(String.format("%.0f", b.getVolume()));
                if (i < bars.size() - 1) sb.append(",");
                pw.println(sb.toString());
            }
            pw.println("]");
        }
    }

    public static TimeSeries fromCSV(String filePath) throws IOException {
        List<OHLCBar> bars = new ArrayList<>();
        try (BufferedReader br = new BufferedReader(new FileReader(filePath))) {
            String line = br.readLine(); // skip header
            while ((line = br.readLine()) != null) {
                String[] p = line.split(",");
                if (p.length < 6) continue;
                bars.add(new OHLCBar(bars.size() * 86400000L,
                        Double.parseDouble(p[1]), Double.parseDouble(p[2]),
                        Double.parseDouble(p[3]), Double.parseDouble(p[4]), Double.parseDouble(p[5])));
            }
        }
        return new TimeSeries(bars);
    }
}
