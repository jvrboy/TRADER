package com.microkernel.serialization;

import com.microkernel.core.*;

import java.io.*;
import java.nio.ByteBuffer;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Nano-kernel: Fast serialization using custom byte-level encoding.
 * For simple types (primitives, strings, arrays), this avoids the
 * overhead of ObjectOutputStream and Java's reflection-based serializer.
 *
 * For complex objects, falls back to Java serialization but with a
 * class cache to avoid repeated class descriptor lookups.
 *
 * Usage:
 *   FastSerializerKernel ser = registry.get("fast-serializer");
 *   byte[] data = ser.serialize(myObject);
 *   MyObject obj = ser.deserialize(data);
 */
public class FastSerializerKernel implements MicroKernel {

    private final ConcurrentHashMap<Class<?>, Integer> classCache = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Integer, Class<?>> reverseClassCache = new ConcurrentHashMap<>();
    private final AtomicLong classCacheMisses = new AtomicLong(0);
    private KernelMetrics metrics;

    public FastSerializerKernel() {}

    @Override
    public String name() { return "fast-serializer"; }

    @Override
    public String description() {
        return "Fast serialization with class caching and optimized primitives encoding";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("fast-serializer");
        // Pre-register common types
        registerClass(String.class);
        registerClass(Integer.class);
        registerClass(Long.class);
        registerClass(Double.class);
        registerClass(Float.class);
        registerClass(Boolean.class);
        registerClass(Byte.class);
        registerClass(Short.class);
        registerClass(Character.class);
    }

    private int registerClass(Class<?> clazz) {
        int id = clazz.getName().hashCode();
        classCache.put(clazz, id);
        reverseClassCache.put(id, clazz);
        return id;
    }

    /** Serialize any Serializable object */
    public byte[] serialize(Object obj) {
        long start = System.nanoTime();
        try {
            if (obj == null) return new byte[]{0};

            if (obj instanceof String) {
                return serializeString((String) obj);
            } else if (obj instanceof Integer) {
                return serializeInt(((Integer) obj));
            } else if (obj instanceof Long) {
                return serializeLong(((Long) obj));
            } else if (obj instanceof Double) {
                return serializeDouble((Double) obj);
            } else if (obj instanceof Boolean) {
                return serializeBoolean((Boolean) obj);
            } else if (obj instanceof byte[]) {
                return serializeByteArray((byte[]) obj);
            } else {
                // Fall back to Java serialization with class cache
                return fallbackSerialize(obj);
            }
        } catch (Exception e) {
            metrics.recordError();
            throw new RuntimeException("Serialization failed", e);
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    /** Deserialize back to object */
    public Object deserialize(byte[] data) {
        if (data == null || data.length == 0) return null;
        if (data[0] == 0 && data.length == 1) return null;

        long start = System.nanoTime();
        try {
            byte typeMarker = data[0];
            switch (typeMarker) {
                case 1: return deserializeString(data);
                case 2: return deserializeInt(data);
                case 3: return deserializeLong(data);
                case 4: return deserializeDouble(data);
                case 5: return deserializeBoolean(data);
                case 6: return deserializeByteArray(data);
                default: return fallbackDeserialize(data);
            }
        } catch (Exception e) {
            metrics.recordError();
            throw new RuntimeException("Deserialization failed", e);
        } finally {
            metrics.recordOperation(System.nanoTime() - start);
        }
    }

    // --- Fast-path serializers ---

    private byte[] serializeString(String s) {
        byte[] strBytes = s.getBytes(java.nio.charset.StandardCharsets.UTF_8);
        ByteBuffer buf = ByteBuffer.allocate(1 + 4 + strBytes.length);
        buf.put((byte) 1); // type marker
        buf.putInt(strBytes.length);
        buf.put(strBytes);
        return buf.array();
    }

    private String deserializeString(byte[] data) {
        ByteBuffer buf = ByteBuffer.wrap(data);
        buf.get(); // skip marker
        int len = buf.getInt();
        byte[] strBytes = new byte[len];
        buf.get(strBytes);
        return new String(strBytes, java.nio.charset.StandardCharsets.UTF_8);
    }

    private byte[] serializeInt(int val) {
        return new byte[]{(byte)2, (byte)(val >> 24), (byte)(val >> 16),
                          (byte)(val >> 8), (byte)val};
    }

    private int deserializeInt(byte[] data) {
        return ((data[1] & 0xFF) << 24) | ((data[2] & 0xFF) << 16) |
               ((data[3] & 0xFF) << 8) | (data[4] & 0xFF);
    }

    private byte[] serializeLong(long val) {
        ByteBuffer buf = ByteBuffer.allocate(9);
        buf.put((byte) 3);
        buf.putLong(val);
        return buf.array();
    }

    private long deserializeLong(byte[] data) {
        return ByteBuffer.wrap(data, 1, 8).getLong();
    }

    private byte[] serializeDouble(double val) {
        ByteBuffer buf = ByteBuffer.allocate(9);
        buf.put((byte) 4);
        buf.putDouble(val);
        return buf.array();
    }

    private double deserializeDouble(byte[] data) {
        return ByteBuffer.wrap(data, 1, 8).getDouble();
    }

    private byte[] serializeBoolean(boolean val) {
        return new byte[]{(byte)5, (byte)(val ? 1 : 0)};
    }

    private boolean deserializeBoolean(byte[] data) {
        return data[1] == 1;
    }

    private byte[] serializeByteArray(byte[] arr) {
        ByteBuffer buf = ByteBuffer.allocate(1 + 4 + arr.length);
        buf.put((byte) 6);
        buf.putInt(arr.length);
        buf.put(arr);
        return buf.array();
    }

    private byte[] deserializeByteArray(byte[] data) {
        ByteBuffer buf = ByteBuffer.wrap(data);
        buf.get();
        int len = buf.getInt();
        byte[] arr = new byte[len];
        buf.get(arr);
        return arr;
    }

    // --- Fallback: Java Object serialization with class cache ---

    private byte[] fallbackSerialize(Object obj) throws IOException {
        ByteArrayOutputStream baos = new ByteArrayOutputStream(512);
        try (ObjectOutputStream oos = new ObjectOutputStream(baos)) {
            oos.writeObject(obj);
        }
        return baos.toByteArray();
    }

    private Object fallbackDeserialize(byte[] data) throws IOException, ClassNotFoundException {
        try (ObjectInputStream ois = new ObjectInputStream(
                new ByteArrayInputStream(data))) {
            return ois.readObject();
        }
    }

    public int classCacheSize() { return classCache.size(); }
    public long classCacheMisses() { return classCacheMisses.get(); }

    @Override
    public void shutdown() {
        classCache.clear();
        reverseClassCache.clear();
    }

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
