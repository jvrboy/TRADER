package com.sandbox.cli;

import com.sandbox.core.*;

import java.io.*;
import java.util.*;
import java.util.concurrent.*;

/**
 * Interactive command-line interface for the sandbox.
 * Provides a REPL with command history, tab completion hints, and colored output.
 */
public class CommandLineInterface {

    private final SandboxEngine engine;
    private final BufferedReader reader;
    private final List<String> history = new ArrayList<>();
    private boolean running = true;

    public CommandLineInterface(SandboxEngine engine) {
        this.engine = engine;
        this.reader = new BufferedReader(new InputStreamReader(System.in));
    }

    public void start() {
        printBanner();
        while (running) {
            try {
                String line = readLine();
                if (line == null) break;
                line = line.trim();
                if (line.isEmpty()) continue;
                history.add(line);
                processInput(line);
            } catch (Exception e) {
                System.out.println(ColorFormatter.error("Error: " + e.getMessage()));
            }
        }
        System.out.println(ColorFormatter.dim("\nGoodbye!"));
        engine.shutdown();
    }

    private void processInput(String line) {
        // Check for special commands
        if (line.equals("exit") || line.equals("quit")) {
            running = false;
            return;
        }
        if (line.equals("help") || line.equals("?")) {
            printHelp();
            return;
        }
        if (line.equals("tools")) {
            printTools();
            return;
        }
        if (line.equals("status")) {
            printStatus();
            return;
        }
        if (line.equals("history")) {
            printHistory();
            return;
        }
        if (line.equals("banner")) {
            printBanner();
            return;
        }
        if (line.equals("clear") || line.equals("cls")) {
            System.out.print("\033[H\033[2J");
            System.out.flush();
            return;
        }
        if (line.startsWith("run ")) {
            String cmd = line.substring(4).trim();
            executeAndPrint(cmd);
            return;
        }
        if (line.startsWith("async ")) {
            String cmd = line.substring(6).trim();
            executeAsyncAndPrint(cmd);
            return;
        }
        if (line.startsWith("script ")) {
            String[] parts = line.substring(7).trim().split("\\s+", 2);
            if (parts.length < 2) {
                System.out.println(ColorFormatter.error("Usage: script <language> <code>"));
                return;
            }
            ExecutionResult result = engine.runScript(parts[0], parts[1]);
            printResult(result);
            return;
        }
        if (line.startsWith("file ")) {
            handleFileCommand(line.substring(5).trim());
            return;
        }
        if (line.startsWith("get ")) {
            String url = line.substring(4).trim();
            try {
                String resp = engine.httpGet(url);
                System.out.println(resp);
            } catch (IOException e) {
                System.out.println(ColorFormatter.error("HTTP Error: " + e.getMessage()));
            }
            return;
        }
        if (line.startsWith("download ")) {
            String[] parts = line.substring(9).trim().split("\\s+", 2);
            if (parts.length < 2) {
                System.out.println(ColorFormatter.error("Usage: download <url> <savepath>"));
                return;
            }
            try {
                engine.downloadFile(parts[0], parts[1]);
                System.out.println(ColorFormatter.success("Downloaded: " + parts[1]));
            } catch (IOException e) {
                System.out.println(ColorFormatter.error("Download error: " + e.getMessage()));
            }
            return;
        }
        if (line.startsWith("run_all ")) {
            handleRunAll(line.substring(8).trim());
            return;
        }
        if (line.startsWith("concurrent ")) {
            handleConcurrent(line.substring(11).trim());
            return;
        }
        if (line.startsWith("cd ")) {
            String path = line.substring(3).trim();
            File newDir = new File(path);
            if (!newDir.isAbsolute()) newDir = new File(engine.getConfig().getWorkingDirectory(), path);
            if (newDir.exists() && newDir.isDirectory()) {
                engine.getConfig().workingDirectory(newDir);
                System.out.println(ColorFormatter.success("Changed to: " + newDir.getAbsolutePath()));
            } else {
                System.out.println(ColorFormatter.error("Directory not found: " + path));
            }
            return;
        }
        if (line.startsWith("timeout ")) {
            String[] parts = line.substring(8).trim().split("\\s+", 2);
            if (parts.length < 2) {
                System.out.println(ColorFormatter.error("Usage: timeout <seconds> <command>"));
                return;
            }
            try {
                int secs = Integer.parseInt(parts[0]);
                ExecutionResult result = engine.run(parts[1], secs, TimeUnit.SECONDS);
                printResult(result);
            } catch (NumberFormatException e) {
                System.out.println(ColorFormatter.error("Invalid timeout value"));
            }
            return;
        }
        // Try as a built-in tool
        String[] parts = line.split("\\s+", 2);
        String toolName = parts[0];
        String[] toolArgs = parts.length > 1 ? parts[1].split("\\s+") : new String[0];
        if (engine.listTools().contains(toolName)) {
            ExecutionResult result = engine.useTool(toolName, toolArgs);
            printResult(result);
            return;
        }
        // Default: execute as shell command
        executeAndPrint(line);
    }

    private void handleFileCommand(String cmd) {
        String[] parts = cmd.split("\\s+", 3);
        if (parts.length < 2) {
            System.out.println(ColorFormatter.error("Usage: file <write|read|create|delete|list|mkdir|tree> <path> [content]"));
            return;
        }
        String action = parts[0];
        String path = parts[1];
        try {
            switch (action) {
                case "write": case "w":
                    if (parts.length < 3) { System.out.println(ColorFormatter.error("Usage: file write <path> <content>")); return; }
                    engine.writeFile(path, parts[2]);
                    System.out.println(ColorFormatter.success("Written to: " + path));
                    break;
                case "append": case "a":
                    if (parts.length < 3) { System.out.println(ColorFormatter.error("Usage: file append <path> <content>")); return; }
                    engine.appendFile(path, parts[2]);
                    System.out.println(ColorFormatter.success("Appended to: " + path));
                    break;
                case "read": case "r":
                    System.out.println(engine.readFile(path));
                    break;
                case "create": case "c":
                    String content = parts.length > 2 ? parts[2] : "";
                    engine.createFile(path, content);
                    System.out.println(ColorFormatter.success("Created: " + path));
                    break;
                case "delete": case "del": case "rm":
                    engine.deleteFile(path);
                    System.out.println(ColorFormatter.success("Deleted: " + path));
                    break;
                case "list": case "ls":
                    System.out.println(String.join("\n", engine.listFiles(path)));
                    break;
                case "mkdir":
                    engine.createDirectory(path);
                    System.out.println(ColorFormatter.success("Created directory: " + path));
                    break;
                case "tree":
                    int depth = parts.length > 2 ? Integer.parseInt(parts[2]) : 5;
                    System.out.println(engine.getFileSystemManager().getTree(path, depth));
                    break;
                case "stat": case "info":
                    Map<String, Object> info = engine.getFileSystemManager().getFileInfo(path);
                    for (Map.Entry<String, Object> entry : info.entrySet()) {
                        System.out.println(entry.getKey() + ": " + entry.getValue());
                    }
                    break;
                default:
                    System.out.println(ColorFormatter.error("Unknown file action: " + action));
            }
        } catch (Exception e) {
            System.out.println(ColorFormatter.error("File error: " + e.getMessage()));
        }
    }

    private void handleRunAll(String input) {
        String[] commands = input.split(";");
        System.out.println(ColorFormatter.info("Running " + commands.length + " commands sequentially..."));
        long start = System.currentTimeMillis();
        for (String cmd : commands) {
            cmd = cmd.trim();
            if (cmd.isEmpty()) continue;
            System.out.println(ColorFormatter.cyan("$ " + cmd));
            ExecutionResult result = engine.run(cmd);
            if (result.getStdout() != null && !result.getStdout().isEmpty()) System.out.println(result.getStdout());
            if (!result.isSuccess()) {
                System.out.println(ColorFormatter.error("  FAILED (exit " + result.getExitCode() + ")"));
            }
        }
        long elapsed = System.currentTimeMillis() - start;
        System.out.println(ColorFormatter.info("Completed " + commands.length + " commands in " + elapsed + "ms"));
    }

    private void handleConcurrent(String input) {
        String[] parts = input.split(";");
        System.out.println(ColorFormatter.info("Running " + parts.length + " commands concurrently..."));
        long start = System.currentTimeMillis();
        List<Future<ExecutionResult>> futures = new ArrayList<>();
        for (String cmd : parts) {
            cmd = cmd.trim();
            if (cmd.isEmpty()) continue;
            futures.add(engine.runAsync(cmd));
        }
        int success = 0, failed = 0;
        for (int i = 0; i < futures.size(); i++) {
            try {
                ExecutionResult result = futures.get(i).get();
                String status = result.isSuccess() ? ColorFormatter.success("OK") : ColorFormatter.error("FAIL");
                System.out.println("  [" + status + ColorFormatter.RESET + "] " + parts[i].trim() +
                        ColorFormatter.dim(" (" + result.getExecutionTimeMs() + "ms)"));
                if (result.isSuccess()) success++; else failed++;
            } catch (Exception e) {
                System.out.println("  [" + ColorFormatter.error("FAIL") + "] " + parts[i].trim() +
                        ColorFormatter.dim(" (" + e.getMessage() + ")"));
                failed++;
            }
        }
        long elapsed = System.currentTimeMillis() - start;
        System.out.println(ColorFormatter.info("Results: " + success + " success, " + failed + " failed, " + elapsed + "ms total"));
    }

    private void executeAndPrint(String command) {
        System.out.println(ColorFormatter.cyan("$ " + command));
        long start = System.currentTimeMillis();
        ExecutionResult result = engine.run(command);
        long elapsed = System.currentTimeMillis() - start;
        if (result.getStdout() != null && !result.getStdout().isEmpty()) System.out.println(result.getStdout());
        if (result.isTimedOut()) {
            System.out.println(ColorFormatter.error("TIMED OUT"));
        } else if (result.getExitCode() != 0) {
            if (result.getStderr() != null && !result.getStderr().isEmpty()) System.out.println(ColorFormatter.red(result.getStderr()));
            System.out.println(ColorFormatter.dim("Exit code: " + result.getExitCode()));
        }
        System.out.println(ColorFormatter.dim("(" + elapsed + "ms)"));
    }

    private void executeAsyncAndPrint(String command) {
        System.out.println(ColorFormatter.info("Submitting async: " + command));
        Future<ExecutionResult> future = engine.runAsync(command);
        new Thread(() -> {
            try {
                ExecutionResult result = future.get();
                String status = result.isSuccess() ? ColorFormatter.success("OK") : ColorFormatter.error("FAIL");
                System.out.println(ColorFormatter.dim("[async done] ") + status +
                        ColorFormatter.dim(" " + command + " (" + result.getExecutionTimeMs() + "ms)"));
                if (result.getStdout() != null && !result.getStdout().isEmpty()) System.out.println(result.getStdout());
            } catch (Exception e) {
                System.out.println(ColorFormatter.error("[async error] " + command + ": " + e.getMessage()));
            }
        }).start();
    }

    private void printResult(ExecutionResult result) {
        if (result.getStdout() != null && !result.getStdout().isEmpty()) System.out.println(result.getStdout());
        if (!result.isSuccess()) {
            if (result.getStderr() != null && !result.getStderr().isEmpty()) System.out.println(ColorFormatter.red(result.getStderr()));
            System.out.println(ColorFormatter.dim("Exit code: " + result.getExitCode()));
        }
    }

    private String readLine() throws IOException {
        System.out.print(ColorFormatter.prompt("sandbox> "));
        System.out.flush();
        return reader.readLine();
    }

    private void printBanner() {
        String banner = ColorFormatter.brightBlue("""

   ██████╗ ██████╗ ██████╗ ███████╗
  ██╔════╝██╔═══██╗██╔══██╗██╔════╝
  ██║     ██║   ██║██║  ██║█████╗
  ██║     ██║   ██║██║  ██║██╔══╝
  ╚██████╗╚██████╔╝██████╔╝███████╗
   ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝

""") + ColorFormatter.dim("  Java Sandbox System v2.0 | Session: " + engine.getSessionId() + "\n") +
                ColorFormatter.dim("  Type 'help' for commands | 'tools' for available tools\n");
        System.out.println(banner);
    }

    private void printHelp() {
        String help = """
  """ + ColorFormatter.header("═══ COMMANDS ═══") + """

  """ + ColorFormatter.bold("Shell Commands:") + """
    <command>              Execute any shell command
    run <command>          Execute shell command explicitly
    async <command>        Execute command asynchronously
    timeout <sec> <cmd>    Execute with custom timeout
    run_all <cmd1>;<cmd2>  Run multiple commands sequentially
    concurrent <cmd1>;<cmd2> Run multiple commands concurrently

  """ + ColorFormatter.bold("Script Execution:") + """
    script <lang> <code>   Run code in any language
                          (python, node, ruby, perl, php, bash, lua, etc.)

  """ + ColorFormatter.bold("File Operations:") + """
    file write <p> <text>  Write text to file
    file read <path>       Read file contents
    file append <p> <text> Append text to file
    file create <path>     Create empty file
    file delete <path>     Delete file
    file list <dir>        List directory contents
    file mkdir <path>      Create directory
    file tree <dir> [d]    Show directory tree

  """ + ColorFormatter.bold("Network:") + """
    get <url>              HTTP GET request
    download <url> <path>  Download file from URL

  """ + ColorFormatter.bold("Navigation:") + """
    cd <path>              Change working directory

  """ + ColorFormatter.bold("Built-in Tools:") + """
    tools                  List all available tools
    <tool> [args]          Execute any built-in tool
    help <tool>            Show tool help

  """ + ColorFormatter.bold("Other:") + """
    status                 Show sandbox status
    history                Show command history
    clear                  Clear screen
    banner                 Show banner
    exit / quit            Exit sandbox
""";
        System.out.println(help);
    }

    private void printTools() {
        List<String> tools = engine.listTools();
        System.out.println(ColorFormatter.header("═══ AVAILABLE TOOLS (" + tools.size() + ") ═══"));
        for (String name : tools) {
            String help = engine.getToolHelp(name);
            System.out.println(String.format("  %-14s %s", ColorFormatter.green(name), help.substring(help.indexOf(" - ") + 3)));
        }
        System.out.println();
    }

    private void printStatus() {
        Map<String, Object> status = engine.getStatus();
        System.out.println(ColorFormatter.header("═══ SANDBOX STATUS ═══"));
        for (Map.Entry<String, Object> entry : status.entrySet()) {
            System.out.println(String.format("  %-22s %s", ColorFormatter.cyan(entry.getKey() + ":"), entry.getValue()));
        }
        System.out.println();
    }

    private void printHistory() {
        System.out.println(ColorFormatter.header("═══ COMMAND HISTORY (" + history.size() + ") ═══"));
        for (int i = Math.max(0, history.size() - 50); i < history.size(); i++) {
            System.out.println(String.format("  %4d  %s", i + 1, ColorFormatter.dim(history.get(i))));
        }
        System.out.println();
    }
}