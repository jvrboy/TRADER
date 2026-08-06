package com.deriv.swarm.core;

import java.time.Instant;
import java.util.Map;
import java.util.UUID;

public class AgentMessage {
    private final String id;
    private final String senderId;
    private final String recipientId;
    private final String type;
    private final String payload;
    private final Instant timestamp;
    private final Map<String, String> metadata;
    private final int priority;

    public AgentMessage(String senderId, String recipientId, String type, String payload) {
        this(UUID.randomUUID().toString(), senderId, recipientId, type, payload,
             Instant.now(), Map.of(), 5);
    }

    public AgentMessage(String id, String senderId, String recipientId, String type,
                        String payload, Instant timestamp, Map<String, String> metadata, int priority) {
        this.id = id;
        this.senderId = senderId;
        this.recipientId = recipientId;
        this.type = type;
        this.payload = payload;
        this.timestamp = timestamp;
        this.metadata = metadata;
        this.priority = priority;
    }

    public String getId() { return id; }
    public String getSenderId() { return senderId; }
    public String getRecipientId() { return recipientId; }
    public String getType() { return type; }
    public String getPayload() { return payload; }
    public Instant getTimestamp() { return timestamp; }
    public Map<String, String> getMetadata() { return metadata; }
    public int getPriority() { return priority; }

    public boolean isBroadcast() { return "*".equals(recipientId); }

    public static AgentMessage broadcast(String senderId, String type, String payload) {
        return new AgentMessage(senderId, "*", type, payload);
    }

    public AgentMessage withPriority(int p) {
        return new AgentMessage(id, senderId, recipientId, type, payload, timestamp, metadata, p);
    }

    @Override
    public String toString() {
        return String.format("Msg[%s->%s %s prio=%d]", senderId, recipientId, type, priority);
    }
}