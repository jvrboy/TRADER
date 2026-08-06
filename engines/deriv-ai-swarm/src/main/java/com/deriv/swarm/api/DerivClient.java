package com.deriv.swarm.api;

import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.TickData;
import com.google.gson.*;
import okhttp3.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.time.Instant;
import java.util.*;

public class DerivClient {
    private static final Logger log = LoggerFactory.getLogger(DerivClient.class);
    private static final String BASE_URL = "https://api.deriv.ws";
    private static final MediaType JSON = MediaType.get("application/json; charset=utf-8");

    private final OkHttpClient httpClient = new OkHttpClient.Builder()
            .connectTimeout(30, java.util.concurrent.TimeUnit.SECONDS)
            .readTimeout(60, java.util.concurrent.TimeUnit.SECONDS)
            .writeTimeout(30, java.util.concurrent.TimeUnit.SECONDS)
            .build();
    private final Gson gson = new Gson();
    private final String appId;
    private String authToken;
    private DerivWebSocket ws;

    public DerivClient(String appId) {
        this.appId = appId;
    }

    public DerivClient() {
        this("1089"); // Default Deriv test app ID
    }

    // --- REST API Calls ---

    public JsonObject callApi(String method, Map<String, Object> params) throws IOException {
        Map<String, Object> request = new LinkedHashMap<>();
        request.put("app_id", Integer.parseInt(appId));
        request.put("req_id", System.currentTimeMillis());
        if (params != null) request.putAll(params);
        request.put(method, 1);

        String json = gson.toJson(request);
        log.debug("API Request: {}", json);

        RequestBody body = RequestBody.create(json, JSON);
        Request httpRequest = new Request.Builder()
                .url(BASE_URL + "/send")
                .post(body)
                .build();

        try (Response response = httpClient.newCall(httpRequest).execute()) {
            String respBody = response.body().string();
            JsonObject resp = JsonParser.parseString(respBody).getAsJsonObject();
            if (resp.has("error")) {
                log.error("API Error: {}", resp.get("error"));
            }
            log.debug("API Response: {}", respBody);
            return resp;
        }
    }

    // --- Public Endpoints ---

    public JsonObject getActiveSymbols() throws IOException {
        return callApi("active_symbols", Map.of(
                "active_symbols", "brief",
                "product_type", "basic"
        ));
    }

    public JsonObject getPayoutForSymbol(String symbol) throws IOException {
        return callApi("payout_for_symbol", Map.of(
                "payout_for_symbol", symbol,
                "contract_type", "CALL"
        ));
    }

    // --- Candle Data ---

    public List<Candle> getCandles(String symbol, String granularity, int count) throws IOException {
        Map<String, Object> params = new LinkedHashMap<>();
        params.put("ticks_history", symbol);
        params.put("adjust_start_time", 1);
        params.put("count", count);
        params.put("end", "latest");
        params.put("granularity", granularity);
        params.put("style", "candles");

        JsonObject resp = callApi("candles", params);
        return parseCandles(resp);
    }

    private List<Candle> parseCandles(JsonObject resp) {
        List<Candle> candles = new ArrayList<>();
        if (resp.has("candles")) {
            JsonArray arr = resp.getAsJsonArray("candles");
            for (JsonElement e : arr) {
                JsonObject c = e.getAsJsonObject();
                candles.add(new Candle(
                        Instant.ofEpochSecond(c.get("epoch").getAsLong()),
                        c.get("open").getAsDouble(),
                        c.get("high").getAsDouble(),
                        c.get("low").getAsDouble(),
                        c.get("close").getAsDouble(),
                        c.has("tick_volume") ? c.get("tick_volume").getAsLong() : 0
                ));
            }
        }
        return candles;
    }

    // --- Tick Streaming ---

    public DerivWebSocket connectWebSocket() {
        this.ws = new DerivWebSocket(appId);
        ws.connect();
        return ws;
    }

    public void subscribeTicks(String symbol) {
        if (ws != null && ws.isConnected()) {
            ws.subscribeTicks(symbol);
        }
    }

    public void subscribeCandles(String symbol, String granularity) {
        if (ws != null && ws.isConnected()) {
            ws.subscribeCandles(symbol, granularity);
        }
    }

    public void disconnect() {
        if (ws != null) ws.disconnect();
        httpClient.dispatcher().executorService().shutdown();
    }

    public String getAppId() { return appId; }
}
