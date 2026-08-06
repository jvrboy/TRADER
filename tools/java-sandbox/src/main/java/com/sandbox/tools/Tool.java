package com.sandbox.tools;

import com.sandbox.core.ExecutionResult;
import com.sandbox.core.SandboxEngine;

/**
 * Represents a sandbox tool - a named command with description and execution logic.
 */
public class Tool {
    private final String name;
    private final String description;
    private final ToolExecutor executor;

    @FunctionalInterface
    public interface ToolExecutor {
        ExecutionResult execute(SandboxEngine engine, String[] args);
    }

    public Tool(String name, String description, ToolExecutor executor) {
        this.name = name;
        this.description = description;
        this.executor = executor;
    }

    public ExecutionResult execute(SandboxEngine engine, String[] args) {
        return executor.execute(engine, args);
    }

    public String getName() { return name; }
    public String getDescription() { return description; }
}
