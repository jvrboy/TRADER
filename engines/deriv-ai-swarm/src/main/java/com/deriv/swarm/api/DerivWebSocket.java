package com.deriv.swarm.api;

import com.deriv.swarm.model.Candle;
import com.deriv.swarm.model.TickData;
import com.google.gson.*;
import okhttp3.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Instant;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArrayList;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.Consumer;

public class DerivWebSocket {
    private static final Logger log = LoggerFactory.getLogger(DerivWebSocket.class);
    private static final String WS_URL = "wss://ws.derivws.com/websockets/v3?app_id=";

    private final OkHttpClient client = new OkHttpClient.Builder()
            .pingInterval(20, java.util.concurrent.TimeUnit.SECONDS)
            .build();
    private final Gson gson = new Gson();
    private final String appId;
    private WebSocket webSocket;
    private volatile boolean connected = false;
    private final AtomicLong reqId = new AtomicLong(1);
    private final List<Consumer<TickData>> tickListeners = new CopyOnWriteArrayList<>();
    private final List<Consumer<Candle>> candleListeners = new CopyOnWriteArrayList<>();
    private final Map<String, JsonObject> pendingResponses = new ConcurrentHashMap<>();

    public DerivWebSocket(String appId) {
        this.appId = appId;
    }

    public void connect() {
        Request request = new Request.Builder().url(WS_URL + appId).build();
        webSocket = client.newWebSocket(request, new WebSocketListener() {
            @Override
            public void onOpen(WebSocket ws, Response response) {
                connected = true;
                log.info("WebSocket connected to Deriv");
            }

            @Override
            public void onMessage(WebSocket ws, String text) {
                handleMessage(text);
            }

            @Override
            public void onClosing(WebSocket ws, int code, String reason) {
                ws.close(1000, null);
                connected = false;
                log.info("WebSocket closing: {}", reason);
            }

            @Override
            public void onFailure(WebSocket ws, Throwable t, Response resp) {
                connected = false;
                log.error("WebSocket failure: {}", t.getMessage());
            }
        });
    }

    private void handleMessage(String text) {
        try {
            JsonObject msg = JsonParser.parseString(text).getAsJsonObject();
            String msgType = msg.has("msg_type") ? msg.get("msg_type").getAsString() : "";

            if ("tick".equals(msgType)) {
                JsonObject tick = msg.getAsJsonObject("tick");
                TickData td = new TickData(
                        tick.get("symbol").getAsString(),
                        Instant.ofEpochSecond(tick.get("epoch").getAsLong()),
                        tick.get("bid").getAsDouble(),
                        tick.get("ask").getAsDouble()
                );
                tickListeners.forEach(l -> l.accept(td));
            } else if ("ohlc".equals(msgType)) {
                JsonObject ohlc = msg.getAsJsonObject("ohlc");
                Candle c = new Candle(
                        Instant.ofEpochSecond(ohlc.get("open_time").getAsLong()),
                        ohlc.get("open").getAsDouble(),
                        ohlc.get("high").getAsDouble(),
                        ohlc.get("low").getAsDouble(),
                        ohlc.get("close").getAsDouble(),
                        ohlc.has("tick_volume") ? ohlc.get("tick_volume").getAsLong() : 0
                );
                candleListeners.forEach(l -> l.accept(c));
            }

            // Store response for request/response correlation
            if (msg.has("req_id")) {
                pendingResponses.put(msg.get("req_id").getAsString(), msg);
            }
        } catch (Exception e) {
            log.warn("Failed to parse WS message: {}", e.getMessage());
        }
    }

    public void subscribeTicks(String symbol) {
        Map<String, Object> req = new LinkedHashMap<>();
        req.put("ticks", symbol);
        req.put("subscribe", 1);
        req.put("req_id", reqId.getAndIncrement());
        send(req);
        log.info("Subscribed to ticks for {}", symbol);
    }

    public void subscribeCandles(String symbol, String granularity) {
        Map<String, Object> req = new LinkedHashMap<>();
        req.put("ticks_history", symbol);
        req.put("adjust_start_time", 1);
        req.put("count", 100);
        req.put("end", "latest");
        req.put("granularity", granularity);
        req.put("style", "candles");
        req.put("subscribe", 1);
        req.put("req_id", reqId.getAndIncrement());
        send(req);
        log.info("Subscribed to candles for {} ({})", symbol, granularity);
    }

    public void forget(String subscriptionId) {
        Map<String, Object> req = new LinkedHashMap<>();
        req.put("forget", subscriptionId);
        req.put("req_id", reqId.getAndIncrement());
        send(req);
    }

    public void forgetAll(String symbol) {
        Map<String, Object> req = new LinkedHashMap<>();
        req.put("forget_all", "candles");
        req.put("req_id", reqId.getAndIncrement());
        send(req);
    }

    private void send(Map<String, Object> msg) {
        if (webSocket != null) {
            webSocket.send(gson.toJson(msg));
        }
    }

    public void onTick(Consumer<TickData> listener) { tickListeners.add(listener); }
    public void onCandle(Consumer<Candle> listener) { candleListeners.add(listener); }

    public void disconnect() {
        if (webSocket != null) {
            webSocket.close(1000, "Normal closure");
        }
        client.dispatcher().executorService().shutdown();
        connected = false;
        log.info("WebSocket disconnected");
    }

    public boolean isConnected() { return connected; }
}
