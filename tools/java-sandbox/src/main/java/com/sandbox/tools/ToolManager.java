package com.sandbox.tools;

import com.sandbox.core.ExecutionResult;
import com.sandbox.core.SandboxEngine;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Manages built-in and custom tools available in the sandbox.
 * Tools are named commands with predefined behavior.
 */
public class ToolManager {

    private final SandboxEngine engine;
    private final Map<String, Tool> tools = new ConcurrentHashMap<>();

    public ToolManager(SandboxEngine engine) {
        this.engine = engine;
        registerBuiltinTools();
    }

    private void registerBuiltinTools() {
        // System info tools
        register(new Tool("sysinfo", "Show system information", this::sysinfo));
        register(new Tool("env", "Show environment variables", this::env));
        register(new Tool("whoami", "Show current user", this::whoami));
        register(new Tool("pwd", "Print working directory", this::pwd));
        register(new Tool("date", "Show current date and time", this::date));
        register(new Tool("uname", "Show system name", this::uname));
        register(new Tool("hostname", "Show hostname", this::hostname));
        register(new Tool("df", "Show disk usage", this::df));
        register(new Tool("free", "Show memory usage", this::free));
        register(new Tool("uptime", "Show system uptime", this::uptime));

        // File tools
        register(new Tool("cat", "Read file contents (usage: cat <path>)", this::cat));
        register(new Tool("ls", "List files (usage: ls [path])", this::ls));
        register(new Tool("mkdir", "Create directory (usage: mkdir <path>)", this::mkdir));
        register(new Tool("touch", "Create empty file (usage: touch <path>)", this::touch));
        register(new Tool("rm", "Delete file (usage: rm <path>)", this::rm));
        register(new Tool("tree", "Show directory tree (usage: tree [path] [depth])", this::tree));
        register(new Tool("stat", "Show file info (usage: stat <path>)", this::stat));
        register(new Tool("wc", "Count lines/words/chars (usage: wc <path>)", this::wc));
        register(new Tool("head", "Show first N lines (usage: head <path> [n])", this::head));
        register(new Tool("tail", "Show last N lines (usage: tail <path> [n])", this::tail));

        // Network tools
        register(new Tool("ping", "Ping a host (usage: ping <host>)", this::ping));
        register(new Tool("curl", "HTTP GET request (usage: curl <url>)", this::curl));
        register(new Tool("nslookup", "DNS lookup (usage: nslookup <host>)", this::nslookup));
        register(new Tool("download", "Download file (usage: download <url> <savepath>)", this::download));

        // Dev tools
        register(new Tool("echo", "Print text (usage: echo <text>)", this::echo));
        register(new Tool("calc", "Evaluate math expression (usage: calc <expr>)", this::calc));
        register(new Tool("base64", "Base64 encode/decode (usage: base64 encode|decode <text>)", this::base64Tool));
        register(new Tool("md5", "Calculate MD5 hash (usage: md5 <text>)", this::md5));
        register(new Tool("json", "Pretty-print JSON (usage: json <json-string>)", this::jsonFormat));
        register(new Tool("hexdump", "Hex dump of string (usage: hexdump <text>)", this::hexdump));
        register(new Tool("uuid", "Generate UUID", this::uuidGen));
        register(new Tool("random", "Generate random number (usage: random [min] [max])", this::random));
        register(new Tool("sleep", "Sleep for N seconds (usage: sleep <seconds>)", this::sleep));

        // Process tools
        register(new Tool("ps", "List running processes", this::ps));
        register(new Tool("which", "Find command location (usage: which <cmd>)", this::which));

        // Sandbox management
        register(new Tool("status", "Show sandbox status", this::status));
        register(new Tool("tools", "List all available tools", this::toolList));
        register(new Tool("help", "Show help for a tool (usage: help [tool])", this::help));
    }

    public void register(Tool tool) {
        tools.put(tool.getName().toLowerCase(), tool);
    }

    public void unregister(String name) {
        tools.remove(name.toLowerCase());
    }

    public ExecutionResult executeTool(String name, String... args) {
        Tool tool = tools.get(name.toLowerCase());
        if (tool == null) {
            return new ExecutionResult(-1, "",
                    "Unknown tool: " + name + ". Type 'tools' to see available tools.",
                    0, name, false, null, -1);
        }
        try {
            return tool.execute(engine, args);
        } catch (Exception e) {
            return new ExecutionResult(-1, "", "Tool error: " + e.getMessage(), 0, name, false, e, -1);
        }
    }

    public List<String> listTools() {
        List<String> names = new ArrayList<>(tools.keySet());
        Collections.sort(names);
        return names;
    }

    public String getToolHelp(String toolName) {
        Tool tool = tools.get(toolName.toLowerCase());
        if (tool == null) return "Tool not found: " + toolName;
        return tool.getName() + " - " + tool.getDescription();
    }

    // === Tool Implementations ===

    private ExecutionResult sysinfo(SandboxEngine e, String[] args) {
        return e.run("uname -a");
    }

    private ExecutionResult env(SandboxEngine e, String[] args) {
        StringBuilder sb = new StringBuilder();
        for (Map.Entry<String, String> entry : e.getConfig().getEnvironmentVariables().entrySet()) {
            sb.append(entry.getKey()).append("=").append(entry.getValue()).append("\n");
        }
        return new ExecutionResult(0, sb.toString(), "", 0, "env", false, null, -1);
    }

    private ExecutionResult whoami(SandboxEngine e, String[] args) { return e.run("whoami"); }
    private ExecutionResult pwd(SandboxEngine e, String[] args) {
        return new ExecutionResult(0, e.getConfig().getWorkingDirectory().getAbsolutePath() + "\n", "", 0, "pwd", false, null, -1);
    }
    private ExecutionResult date(SandboxEngine e, String[] args) { return e.run("date"); }
    private ExecutionResult uname(SandboxEngine e, String[] args) { return e.run("uname -a"); }
    private ExecutionResult hostname(SandboxEngine e, String[] args) { return e.run("hostname"); }
    private ExecutionResult df(SandboxEngine e, String[] args) { return e.run("df -h"); }
    private ExecutionResult free(SandboxEngine e, String[] args) { return e.run("free -h"); }
    private ExecutionResult uptime(SandboxEngine e, String[] args) { return e.run("uptime"); }

    private ExecutionResult cat(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: cat <path>", 0, "cat", false, null, -1);
        try { return new ExecutionResult(0, e.readFile(args[0]) + "\n", "", 0, "cat", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "cat", false, ex, -1); }
    }

    private ExecutionResult ls(SandboxEngine e, String[] args) {
        String path = args.length > 0 ? args[0] : ".";
        try { return new ExecutionResult(0, String.join("\n", e.listFiles(path)) + "\n", "", 0, "ls", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "ls", false, ex, -1); }
    }

    private ExecutionResult mkdir(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: mkdir <path>", 0, "mkdir", false, null, -1);
        try { e.createDirectory(args[0]); return new ExecutionResult(0, "Created: " + args[0] + "\n", "", 0, "mkdir", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "mkdir", false, ex, -1); }
    }

    private ExecutionResult touch(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: touch <path>", 0, "touch", false, null, -1);
        try { e.createFile(args[0], ""); return new ExecutionResult(0, "Created: " + args[0] + "\n", "", 0, "touch", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "touch", false, ex, -1); }
    }

    private ExecutionResult rm(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: rm <path>", 0, "rm", false, null, -1);
        try { e.deleteFile(args[0]); return new ExecutionResult(0, "Deleted: " + args[0] + "\n", "", 0, "rm", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "rm", false, ex, -1); }
    }

    private ExecutionResult tree(SandboxEngine e, String[] args) {
        String path = args.length > 0 ? args[0] : ".";
        int depth = args.length > 1 ? Integer.parseInt(args[1]) : 5;
        try { return new ExecutionResult(0, e.getFileSystemManager().getTree(path, depth), "", 0, "tree", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "tree", false, ex, -1); }
    }

    private ExecutionResult stat(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: stat <path>", 0, "stat", false, null, -1);
        try {
            Map<String, Object> info = e.getFileSystemManager().getFileInfo(args[0]);
            StringBuilder sb = new StringBuilder();
            for (Map.Entry<String, Object> entry : info.entrySet()) sb.append(entry.getKey()).append(": ").append(entry.getValue()).append("\n");
            return new ExecutionResult(0, sb.toString(), "", 0, "stat", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "stat", false, ex, -1); }
    }

    private ExecutionResult wc(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: wc <path>", 0, "wc", false, null, -1);
        try {
            String content = e.readFile(args[0]);
            String[] lines = content.split("\n");
            String[] words = content.split("\\s+");
            return new ExecutionResult(0, lines.length + " lines, " + words.length + " words, " + content.length() + " chars\n", "", 0, "wc", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "wc", false, ex, -1); }
    }

    private ExecutionResult head(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: head <path> [n]", 0, "head", false, null, -1);
        try {
            String content = e.readFile(args[0]);
            int n = args.length > 1 ? Integer.parseInt(args[1]) : 10;
            String[] lines = content.split("\n");
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Math.min(n, lines.length); i++) sb.append(lines[i]).append("\n");
            return new ExecutionResult(0, sb.toString(), "", 0, "head", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "head", false, ex, -1); }
    }

    private ExecutionResult tail(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: tail <path> [n]", 0, "tail", false, null, -1);
        try {
            String content = e.readFile(args[0]);
            int n = args.length > 1 ? Integer.parseInt(args[1]) : 10;
            String[] lines = content.split("\n");
            StringBuilder sb = new StringBuilder();
            int start = Math.max(0, lines.length - n);
            for (int i = start; i < lines.length; i++) sb.append(lines[i]).append("\n");
            return new ExecutionResult(0, sb.toString(), "", 0, "tail", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "tail", false, ex, -1); }
    }

    private ExecutionResult ping(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: ping <host>", 0, "ping", false, null, -1);
        return e.run("ping -c 4 " + args[0]);
    }

    private ExecutionResult curl(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: curl <url>", 0, "curl", false, null, -1);
        try { return new ExecutionResult(0, e.httpGet(args[0]), "", 0, "curl", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "curl", false, ex, -1); }
    }

    private ExecutionResult nslookup(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: nslookup <host>", 0, "nslookup", false, null, -1);
        try { return new ExecutionResult(0, e.resolveHost(args[0]) + "\n", "", 0, "nslookup", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "nslookup", false, ex, -1); }
    }

    private ExecutionResult download(SandboxEngine e, String[] args) {
        if (args.length < 2) return new ExecutionResult(-1, "", "Usage: download <url> <savepath>", 0, "download", false, null, -1);
        try { e.downloadFile(args[0], args[1]); return new ExecutionResult(0, "Downloaded: " + args[1] + "\n", "", 0, "download", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", ex.getMessage(), 0, "download", false, ex, -1); }
    }

    private ExecutionResult echo(SandboxEngine e, String[] args) {
        return new ExecutionResult(0, String.join(" ", args) + "\n", "", 0, "echo", false, null, -1);
    }

    private ExecutionResult calc(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: calc <expression> (e.g. calc 2+3*4)", 0, "calc", false, null, -1);
        String expr = String.join(" ", args);
        // Simple safe evaluation for basic math
        try {
            String sanitized = expr.replaceAll("[^0-9+*/().%\\- ]", "");
            Object result = new javax.script.ScriptEngineManager()
                    .getEngineByName("js").eval(sanitized);
            return new ExecutionResult(0, expr + " = " + result + "\n", "", 0, "calc", false, null, -1);
        } catch (Exception ex) {
            return new ExecutionResult(-1, "", "Math error: " + ex.getMessage(), 0, "calc", false, ex, -1);
        }
    }

    private ExecutionResult base64Tool(SandboxEngine e, String[] args) {
        if (args.length < 2) return new ExecutionResult(-1, "", "Usage: base64 encode|decode <text>", 0, "base64", false, null, -1);
        String mode = args[0];
        String text = String.join(" ", java.util.Arrays.copyOfRange(args, 1, args.length));
        try {
            if (mode.equals("encode")) return new ExecutionResult(0, java.util.Base64.getEncoder().encodeToString(text.getBytes()) + "\n", "", 0, "base64", false, null, -1);
            else if (mode.equals("decode")) return new ExecutionResult(0, new String(java.util.Base64.getDecoder().decode(text)) + "\n", "", 0, "base64", false, null, -1);
            else return new ExecutionResult(-1, "", "Usage: base64 encode|decode <text>", 0, "base64", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", "Error: " + ex.getMessage(), 0, "base64", false, ex, -1); }
    }

    private ExecutionResult md5(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: md5 <text>", 0, "md5", false, null, -1);
        try {
            java.security.MessageDigest md = java.security.MessageDigest.getInstance("MD5");
            byte[] hash = md.digest(String.join(" ", args).getBytes());
            StringBuilder sb = new StringBuilder();
            for (byte b : hash) sb.append(String.format("%02x", b));
            return new ExecutionResult(0, sb.toString() + "\n", "", 0, "md5", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", "Error: " + ex.getMessage(), 0, "md5", false, ex, -1); }
    }

    private ExecutionResult jsonFormat(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: json <json-string>", 0, "json", false, null, -1);
        try {
            String json = String.join(" ", args);
            com.google.gson.Gson gson = new com.google.gson.GsonBuilder().setPrettyPrinting().create();
            Object obj = gson.fromJson(json, Object.class);
            return new ExecutionResult(0, gson.toJson(obj) + "\n", "", 0, "json", false, null, -1);
        } catch (Exception ex) { return new ExecutionResult(-1, "", "JSON parse error: " + ex.getMessage(), 0, "json", false, ex, -1); }
    }

    private ExecutionResult hexdump(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: hexdump <text>", 0, "hexdump", false, null, -1);
        String text = String.join(" ", args);
        byte[] bytes = text.getBytes();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.length; i += 16) {
            sb.append(String.format("%08x: ", i));
            for (int j = 0; j < 16; j++) {
                if (i + j < bytes.length) sb.append(String.format("%02x ", bytes[i + j]));
                else sb.append("   ");
                if (j == 7) sb.append(" ");
            }
            sb.append(" |" );
            for (int j = 0; j < 16 && i + j < bytes.length; j++) {
                char c = (char) bytes[i + j];
                sb.append(c >= 32 && c < 127 ? c : '.');
            }
            sb.append("|\n");
        }
        return new ExecutionResult(0, sb.toString(), "", 0, "hexdump", false, null, -1);
    }

    private ExecutionResult uuidGen(SandboxEngine e, String[] args) {
        return new ExecutionResult(0, java.util.UUID.randomUUID().toString() + "\n", "", 0, "uuid", false, null, -1);
    }

    private ExecutionResult random(SandboxEngine e, String[] args) {
        int min = args.length > 0 ? Integer.parseInt(args[0]) : 1;
        int max = args.length > 1 ? Integer.parseInt(args[1]) : 100;
        return new ExecutionResult(0, (min + new java.util.Random().nextInt(max - min + 1)) + "\n", "", 0, "random", false, null, -1);
    }

    private ExecutionResult sleep(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: sleep <seconds>", 0, "sleep", false, null, -1);
        try { Thread.sleep(Long.parseLong(args[0]) * 1000); return new ExecutionResult(0, "Slept " + args[0] + "s\n", "", 0, "sleep", false, null, -1); }
        catch (Exception ex) { return new ExecutionResult(-1, "", "Error: " + ex.getMessage(), 0, "sleep", false, ex, -1); }
    }

    private ExecutionResult ps(SandboxEngine e, String[] args) { return e.run("ps aux"); }
    private ExecutionResult which(SandboxEngine e, String[] args) {
        if (args.length < 1) return new ExecutionResult(-1, "", "Usage: which <cmd>", 0, "which", false, null, -1);
        return e.run("which " + args[0]);
    }

    private ExecutionResult status(SandboxEngine e, String[] args) {
        Map<String, Object> s = e.getStatus();
        StringBuilder sb = new StringBuilder("=== Sandbox Status ===\n");
        for (Map.Entry<String, Object> entry : s.entrySet()) sb.append(entry.getKey()).append(": ").append(entry.getValue()).append("\n");
        return new ExecutionResult(0, sb.toString(), "", 0, "status", false, null, -1);
    }

    private ExecutionResult toolList(SandboxEngine e, String[] args) {
        StringBuilder sb = new StringBuilder("=== Available Tools (" + tools.size() + ") ===\n");
        for (String name : listTools()) sb.append(String.format("  %-12s %s%n", name, tools.get(name).getDescription()));
        return new ExecutionResult(0, sb.toString(), "", 0, "tools", false, null, -1);
    }

    private ExecutionResult help(SandboxEngine e, String[] args) {
        if (args.length > 0) {
            return new ExecutionResult(0, getToolHelp(args[0]) + "\n", "", 0, "help", false, null, -1);
        }
        return toolList(e, args);
    }
}
