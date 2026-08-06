package com.sandbox.scripting;

import com.sandbox.core.ExecutionResult;
import com.sandbox.core.ProcessExecutor;
import com.sandbox.core.SandboxEngine;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * Executes scripts in multiple languages: Python, Node.js, Ruby, Perl, PHP, Bash, etc.
 * Detects language from file extension or explicit specification.
 */
public class ScriptEngine {

    private final SandboxEngine engine;

    private static final Map<String, String[]> LANGUAGE_COMMANDS = new LinkedHashMap<>();
    static {
        LANGUAGE_COMMANDS.put("python", new String[]{"python3", "python"});
        LANGUAGE_COMMANDS.put("python3", new String[]{"python3", "python"});
        LANGUAGE_COMMANDS.put("node", new String[]{"node", "nodejs"});
        LANGUAGE_COMMANDS.put("javascript", new String[]{"node", "nodejs"});
        LANGUAGE_COMMANDS.put("ruby", new String[]{"ruby"});
        LANGUAGE_COMMANDS.put("perl", new String[]{"perl"});
        LANGUAGE_COMMANDS.put("php", new String[]{"php"});
        LANGUAGE_COMMANDS.put("bash", new String[]{"/bin/bash"});
        LANGUAGE_COMMANDS.put("sh", new String[]{"/bin/sh"});
        LANGUAGE_COMMANDS.put("shell", new String[]{"/bin/bash"});
        LANGUAGE_COMMANDS.put("lua", new String[]{"lua"});
        LANGUAGE_COMMANDS.put("gcc", new String[]{"gcc"});
        LANGUAGE_COMMANDS.put("java", new String[]{"java"});
        LANGUAGE_COMMANDS.put("groovy", new String[]{"groovy"});
        LANGUAGE_COMMANDS.put("awk", new String[]{"awk"});
        LANGUAGE_COMMANDS.put("sed", new String[]{"sed"});
    }

    private static final Map<String, String> EXTENSION_MAP = new HashMap<>();
    static {
        EXTENSION_MAP.put(".py", "python");
        EXTENSION_MAP.put(".js", "node");
        EXTENSION_MAP.put(".rb", "ruby");
        EXTENSION_MAP.put(".pl", "perl");
        EXTENSION_MAP.put(".php", "php");
        EXTENSION_MAP.put(".sh", "bash");
        EXTENSION_MAP.put(".bash", "bash");
        EXTENSION_MAP.put(".lua", "lua");
        EXTENSION_MAP.put(".c", "gcc");
        EXTENSION_MAP.put(".java", "java");
        EXTENSION_MAP.put(".groovy", "groovy");
        EXTENSION_MAP.put(".awk", "awk");
    }

    public ScriptEngine(SandboxEngine engine) {
        this.engine = engine;
    }

    public ExecutionResult execute(String language, String script) {
        language = language.toLowerCase().trim();

        // Handle compiled languages specially
        if (language.equals("c") || language.equals("gcc")) {
            return executeCScript(script);
        }
        if (language.equals("java")) {
            return executeJavaScript(script);
        }

        String[] commands = LANGUAGE_COMMANDS.get(language);
        if (commands == null) {
            return new ExecutionResult(-1, "",
                    "Unsupported language: " + language + ". Supported: " + LANGUAGE_COMMANDS.keySet(),
                    0, language, false, null, -1);
        }

        // Find available interpreter
        String interpreter = findInterpreter(commands);
        if (interpreter == null) {
            return new ExecutionResult(-1, "",
                    "No interpreter found for: " + language + " (tried: " + Arrays.toString(commands) + ")",
                    0, language, false, null, -1);
        }

        try {
            // Write script to temp file
            File tempScript = File.createTempFile("sandbox_" + language + "_", getExtension(language));
            tempScript.setExecutable(true);
            Files.write(tempScript.toPath(), script.getBytes());
            tempScript.deleteOnExit();

            String command = interpreter + " " + tempScript.getAbsolutePath();
 return engine.run(command);

        } catch (IOException e) {
            return new ExecutionResult(-1, "", "Error creating temp script: " + e.getMessage(),
                    0, language, false, e, -1);
        }
    }

    public ExecutionResult executeFile(String filePath) {
        File file = new File(filePath);
        if (!file.exists()) {
            return new ExecutionResult(-1, "", "File not found: " + filePath, 0, filePath, false,
                    new FileNotFoundException(filePath), -1);
        }

        String ext = getExtensionFromFile(file.getName());
        String language = EXTENSION_MAP.getOrDefault(ext, "bash");

        try {
            String content = new String(Files.readAllBytes(file.toPath()));
            return execute(language, content);
        } catch (IOException e) {
            return new ExecutionResult(-1, "", "Error reading file: " + e.getMessage(),
                    0, filePath, false, e, -1);
        }
    }

    public Set<String> getSupportedLanguages() {
        return LANGUAGE_COMMANDS.keySet();
    }

    public Set<String> getAvailableInterpreters() {
        Set<String> available = new LinkedHashSet<>();
        for (Map.Entry<String, String[]> entry : LANGUAGE_COMMANDS.entrySet()) {
            if (findInterpreter(entry.getValue()) != null) {
                available.add(entry.getKey());
            }
        }
        return available;
    }

    private String findInterpreter(String[] candidates) {
        for (String candidate : candidates) {
            try {
                Process p = new ProcessBuilder("which", candidate).start();
                p.waitFor(5, TimeUnit.SECONDS);
                if (p.exitValue() == 0) return candidate;
            } catch (Exception ignored) {}
        }
        return null;
    }

    private ExecutionResult executeCScript(String source) {
        try {
            File srcFile = File.createTempFile("sandbox_c_", ".c");
            File outFile = File.createTempFile("sandbox_c_", "");
            srcFile.deleteOnExit();
            outFile.deleteOnExit();

            Files.write(srcFile.toPath(), source.getBytes());

            ExecutionResult compile = engine.run("gcc -o " + outFile.getAbsolutePath() +
                    " " + srcFile.getAbsolutePath());
            if (!compile.isSuccess()) return compile;

            return engine.run(outFile.getAbsolutePath());
        } catch (IOException e) {
            return new ExecutionResult(-1, "", "C compilation error: " + e.getMessage(),
                    0, "c", false, e, -1);
        }
    }

    private ExecutionResult executeJavaScript(String source) {
        try {
            File srcDir = Files.createTempDirectory("sandbox_java_").toFile();
            srcDir.deleteOnExit();
            File srcFile = new File(srcDir, "Main.java");
            Files.write(srcFile.toPath(), source.getBytes());

            ExecutionResult compile = engine.run("javac " + srcFile.getAbsolutePath());
            if (!compile.isSuccess()) return compile;

            return engine.run("java -cp " + srcDir.getAbsolutePath() + " Main");
        } catch (IOException e) {
            return new ExecutionResult(-1, "", "Java compilation error: " + e.getMessage(),
                    0, "java", false, e, -1);
        }
    }

    private String getExtension(String language) {
        for (Map.Entry<String, String> entry : EXTENSION_MAP.entrySet()) {
            if (entry.getValue().equals(language)) return entry.getKey();
        }
        return ".sh";
    }

    private String getExtensionFromFile(String filename) {
        int idx = filename.lastIndexOf('.');
        return idx >= 0 ? filename.substring(idx).toLowerCase() : "";
    }
}
