package com.sandbox.network;

import com.sandbox.core.SandboxConfig;

import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Handles all network operations within the sandbox.
 * Supports HTTP GET/POST, file downloads, and DNS resolution.
 */
public class NetworkManager {

    private final SandboxConfig config;
    private int connectTimeoutMs = 15000;
    private int readTimeoutMs = 30000;
    private final Map<String, String> defaultHeaders = new LinkedHashMap<>();

    public NetworkManager(SandboxConfig config) {
        this.config = config;
        this.defaultHeaders.put("User-Agent", "JavaSandbox/2.0");
        this.defaultHeaders.put("Accept", "*/*");
    }

    public String httpGet(String url) throws IOException {
        if (!config.isAllowNetworkAccess()) throw new SecurityException("Network access is disabled");
        HttpURLConnection conn = null;
        try {
            URL target = new URL(url);
            conn = (HttpURLConnection) target.openConnection();
            conn.setRequestMethod("GET");
            conn.setConnectTimeout(connectTimeoutMs);
            conn.setReadTimeout(readTimeoutMs);
            conn.setInstanceFollowRedirects(true);
            applyHeaders(conn);

            int code = conn.getResponseCode();
            String body = readStream(code >= 400 ? conn.getErrorStream() : conn.getInputStream());
            return "[HTTP " + code + "] " + body;
        } finally {
            if (conn != null) conn.disconnect();
        }
    }

    public String httpPost(String url, String body) throws IOException {
        if (!config.isAllowNetworkAccess()) throw new SecurityException("Network access is disabled");
        HttpURLConnection conn = null;
        try {
            URL target = new URL(url);
            conn = (HttpURLConnection) target.openConnection();
            conn.setRequestMethod("POST");
            conn.setConnectTimeout(connectTimeoutMs);
            conn.setReadTimeout(readTimeoutMs);
            conn.setDoOutput(true);
            conn.setInstanceFollowRedirects(true);
            applyHeaders(conn);
            conn.setRequestProperty("Content-Type", "application/json");

            try (OutputStream os = conn.getOutputStream()) {
                os.write(body.getBytes(StandardCharsets.UTF_8));
            }

            int code = conn.getResponseCode();
            String response = readStream(code >= 400 ? conn.getErrorStream() : conn.getInputStream());
            return "[HTTP " + code + "] " + response;
        } finally {
            if (conn != null) conn.disconnect();
        }
    }

    public void downloadFile(String url, String savePath) throws IOException {
        if (!config.isAllowNetworkAccess()) throw new SecurityException("Network access is disabled");
        URL target = new URL(url);
        File outFile = new File(savePath);
        outFile.getParentFile().mkdirs();

        try (InputStream in = target.openStream();
             FileOutputStream out = new FileOutputStream(outFile)) {
            byte[] buffer = new byte[8192];
            int read;
            while ((read = in.read(buffer)) != -1) {
                out.write(buffer, 0, read);
            }
        }
    }

    public String resolveHost(String hostname) throws IOException {
        InetAddress addr = InetAddress.getByName(hostname);
        return addr.getHostAddress();
    }

    public Map<String, String> httpGetWithHeaders(String url) throws IOException {
        if (!config.isAllowNetworkAccess()) throw new SecurityException("Network access is disabled");
        HttpURLConnection conn = null;
        try {
            URL target = new URL(url);
            conn = (HttpURLConnection) target.openConnection();
            conn.setRequestMethod("GET");
            conn.setConnectTimeout(connectTimeoutMs);
            conn.setReadTimeout(readTimeoutMs);
            applyHeaders(conn);

            int code = conn.getResponseCode();
            Map<String, String> result = new LinkedHashMap<>();
            result.put("status", String.valueOf(code));
            for (Map.Entry<String, List<String>> entry : conn.getHeaderFields().entrySet()) {
                if (entry.getKey() != null) result.put(entry.getKey(), String.join(", ", entry.getValue()));
            }
            result.put("body", readStream(code >= 400 ? conn.getErrorStream() : conn.getInputStream()));
            return result;
        } finally {
            if (conn != null) conn.disconnect();
        }
    }

    public void setTimeouts(int connectMs, int readMs) {
        this.connectTimeoutMs = connectMs;
        this.readTimeoutMs = readMs;
    }

    public void addDefaultHeader(String key, String value) {
        defaultHeaders.put(key, value);
    }

    private void applyHeaders(HttpURLConnection conn) {
        for (Map.Entry<String, String> entry : defaultHeaders.entrySet()) {
            conn.setRequestProperty(entry.getKey(), entry.getValue());
        }
    }

    private String readStream(InputStream is) throws IOException {
        if (is == null) return "";
        ByteArrayOutputStream result = new ByteArrayOutputStream();
        byte[] buffer = new byte[8192];
        int read;
        while ((read = is.read(buffer)) != -1) {
            result.write(buffer, 0, read);
        }
        return result.toString(StandardCharsets.UTF_8.name());
    }
}
