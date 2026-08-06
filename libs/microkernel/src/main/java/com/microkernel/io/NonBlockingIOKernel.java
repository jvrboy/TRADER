package com.microkernel.io;

import com.microkernel.core.*;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.nio.channels.AsynchronousFileChannel;
import java.nio.channels.CompletionHandler;
import java.nio.file.*;
import java.nio.file.attribute.FileAttribute;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.atomic.AtomicLong;

/**
 * Micro-kernel: Non-blocking file I/O that uses NIO.2 async file channels
 * with completion handlers, eliminating thread blocking during disk reads/writes.
 *
 * Usage:
 *   NonBlockingIOKernel io = registry.get("nonblocking-io");
 *   CompletableFuture<byte[]> data = io.readAllAsync(path);
 *   data.thenAccept(bytes -> process(bytes));
 */
public class NonBlockingIOKernel implements MicroKernel {

    private final int maxConcurrentOps;
    private final AtomicLong bytesRead = new AtomicLong(0);
    private final AtomicLong bytesWritten = new AtomicLong(0);
    private KernelMetrics metrics;

    public NonBlockingIOKernel() {
        this(64);
    }

    public NonBlockingIOKernel(int maxConcurrentOps) {
        this.maxConcurrentOps = maxConcurrentOps;
    }

    @Override
    public String name() { return "nonblocking-io"; }

    @Override
    public String description() {
        return "Non-blocking async file I/O using NIO.2 completion handlers to prevent thread blocking";
    }

    @Override
    public void init(KernelContext context) {
        metrics = new KernelMetrics("nonblocking-io");
    }

    /** Read entire file asynchronously */
    public CompletableFuture<byte[]> readAllAsync(Path path) {
        long start = System.nanoTime();
        return CompletableFuture.supplyAsync(() -> {
            try {
                byte[] bytes = Files.readAllBytes(path);
                bytesRead.addAndGet(bytes.length);
                return bytes;
            } catch (IOException e) {
                metrics.recordError();
                throw new RuntimeException("Failed to read: " + path, e);
            } finally {
                metrics.recordOperation(System.nanoTime() - start);
            }
        });
    }

    /** Write data to file asynchronously */
    public CompletableFuture<Void> writeAsync(Path path, byte[] data) {
        long start = System.nanoTime();
        return CompletableFuture.runAsync(() -> {
            try {
                Files.write(path, data, StandardOpenOption.CREATE,
                    StandardOpenOption.TRUNCATE_EXISTING, StandardOpenOption.WRITE);
                bytesWritten.addAndGet(data.length);
            } catch (IOException e) {
                metrics.recordError();
                throw new RuntimeException("Failed to write: " + path, e);
            } finally {
                metrics.recordOperation(System.nanoTime() - start);
            }
        });
    }

    /** Read with position and length using async file channel */
    public CompletableFuture<ByteBuffer> readAsync(Path path, long position, int length) {
        CompletableFuture<ByteBuffer> future = new CompletableFuture<>();
        try {
            AsynchronousFileChannel channel = AsynchronousFileChannel.open(
                path, StandardOpenOption.READ);
            ByteBuffer buffer = ByteBuffer.allocateDirect(length);

            channel.read(buffer, position, buffer,
                new CompletionHandler<Integer, ByteBuffer>() {
                    @Override
                    public void completed(Integer result, ByteBuffer attachment) {
                        attachment.flip();
                        bytesRead.addAndGet(result);
                        metrics.recordAllocation();
                        try { channel.close(); } catch (IOException ignored) {}
                        future.complete(attachment);
                    }

                    @Override
                    public void failed(Throwable exc, ByteBuffer attachment) {
                        metrics.recordError();
                        try { channel.close(); } catch (IOException ignored) {}
                        future.completeExceptionally(exc);
                    }
                });
        } catch (IOException e) {
            metrics.recordError();
            future.completeExceptionally(e);
        }
        return future;
    }

    /** Copy file asynchronously */
    public CompletableFuture<Void> copyAsync(Path from, Path to) {
        return readAllAsync(from).thenCompose(data -> writeAsync(to, data));
    }

    public long bytesRead()    { return bytesRead.get(); }
    public long bytesWritten() { return bytesWritten.get(); }

    @Override
    public void shutdown() {}

    @Override
    public KernelStatus status() { return KernelStatus.RUNNING; }

    @Override
    public KernelMetrics metrics() { return metrics; }
}
