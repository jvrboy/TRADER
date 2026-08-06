package com.sandbox.filesystem;

import com.sandbox.core.SandboxConfig;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.nio.file.attribute.*;
import java.util.*;
import java.util.stream.Collectors;
import java.util.stream.Stream;

/**
 * Manages file system operations within the sandbox.
 * Supports create, read, write, delete, list, and permissions.
 */
public class FileSystemManager {

    private final SandboxConfig config;
    private final File baseDir;

    public FileSystemManager(SandboxConfig config) {
        this.config = config;
        this.baseDir = config.getWorkingDirectory();
        this.baseDir.mkdirs();
    }

    public String readFile(String path) throws IOException {
        if (!config.isAllowFileRead()) throw new SecurityException("File read is disabled");
        File file = resolvePath(path);
        checkExists(file);
        return new String(Files.readAllBytes(file.toPath()), StandardCharsets.UTF_8);
    }

    public void writeFile(String path, String content) throws IOException {
        if (!config.isAllowFileWrite()) throw new SecurityException("File write is disabled");
        File file = resolvePath(path);
        file.getParentFile().mkdirs();
        Files.write(file.toPath(), content.getBytes(StandardCharsets.UTF_8));
    }

    public void appendFile(String path, String content) throws IOException {
        if (!config.isAllowFileWrite()) throw new SecurityException("File write is disabled");
        File file = resolvePath(path);
        file.getParentFile().mkdirs();
        Files.write(file.toPath(), content.getBytes(StandardCharsets.UTF_8),
                StandardOpenOption.CREATE, StandardOpenOption.APPEND);
    }

    public void createFile(String path, String content) throws IOException {
        if (!config.isAllowFileWrite()) throw new SecurityException("File write is disabled");
        File file = resolvePath(path);
        if (file.exists()) throw new IOException("File already exists: " + path);
        file.getParentFile().mkdirs();
        Files.write(file.toPath(), content.getBytes(StandardCharsets.UTF_8));
    }

    public void deleteFile(String path) throws IOException {
        if (!config.isAllowFileWrite()) throw new SecurityException("File write is disabled");
        File file = resolvePath(path);
        checkExists(file);
        Files.delete(file.toPath());
    }

    public boolean fileExists(String path) {
        return resolvePath(path).exists();
    }

    public List<String> listFiles(String dirPath) throws IOException {
        File dir = resolvePath(dirPath);
        checkDirectory(dir);
        File[] files = dir.listFiles();
        if (files == null) return Collections.emptyList();
        return Arrays.stream(files)
                .map(f -> f.isDirectory() ? f.getName() + "/" : f.getName())
                .sorted()
                .collect(Collectors.toList());
    }

    public void createDirectory(String path) throws IOException {
        if (!config.isAllowFileWrite()) throw new SecurityException("File write is disabled");
        File dir = resolvePath(path);
        Files.createDirectories(dir.toPath());
    }

    public long getFileSize(String path) throws IOException {
        File file = resolvePath(path);
        checkExists(file);
        return file.length();
    }

    public void setPermissions(String path, String perms) throws IOException {
        File file = resolvePath(path);
        checkExists(file);
        Set<PosixFilePermission> permissions = PosixFilePermissions.fromString(perms);
        Files.setPosixFilePermissions(file.toPath(), permissions);
    }

    public String getPermissions(String path) throws IOException {
        File file = resolvePath(path);
        checkExists(file);
        Set<PosixFilePermission> perms = Files.getPosixFilePermissions(file.toPath());
        return PosixFilePermissions.toString(perms);
    }

    public void copyFile(String source, String dest) throws IOException {
        File src = resolvePath(source);
        File dst = resolvePath(dest);
        checkExists(src);
        Files.copy(src.toPath(), dst.toPath(), StandardCopyOption.REPLACE_EXISTING);
    }

    public void moveFile(String source, String dest) throws IOException {
        File src = resolvePath(source);
        File dst = resolvePath(dest);
        checkExists(src);
        Files.move(src.toPath(), dst.toPath(), StandardCopyOption.REPLACE_EXISTING);
    }

    public Map<String, Object> getFileInfo(String path) throws IOException {
        File file = resolvePath(path);
        checkExists(file);
        Map<String, Object> info = new LinkedHashMap<>();
        info.put("path", file.getAbsolutePath());
        info.put("name", file.getName());
        info.put("size", file.length());
        info.put("isDirectory", file.isDirectory());
        info.put("isFile", file.isFile());
        info.put("lastModified", new Date(file.lastModified()));
        info.put("canRead", file.canRead());
        info.put("canWrite", file.canWrite());
        info.put("canExecute", file.canExecute());
        return info;
    }

    public String getTree(String dirPath, int maxDepth) throws IOException {
        File dir = resolvePath(dirPath);
        checkDirectory(dir);
        StringBuilder sb = new StringBuilder();
        buildTree(dir, "", sb, 0, maxDepth);
        return sb.toString();
    }

    private void buildTree(File dir, String prefix, StringBuilder sb, int depth, int maxDepth) {
        if (depth >= maxDepth) return;
        File[] files = dir.listFiles();
        if (files == null) return;
        Arrays.sort(files);
        for (int i = 0; i < files.length; i++) {
            boolean isLast = i == files.length - 1;
            String connector = isLast ? "└── " : "├── ";
            sb.append(prefix).append(connector).append(files[i].getName());
            if (files[i].isDirectory()) sb.append("/");
            sb.append("\n");
            if (files[i].isDirectory()) {
                buildTree(files[i], prefix + (isLast ? "    " : "│   "), sb, depth + 1, maxDepth);
            }
        }
    }

    private File resolvePath(String path) {
        File file = new File(path);
        if (!file.isAbsolute()) {
            file = new File(baseDir, path);
        }
        return file;
    }

    private void checkExists(File file) throws IOException {
        if (!file.exists()) throw new FileNotFoundException("File not found: " + file.getAbsolutePath());
    }

    private void checkDirectory(File file) throws IOException {
        checkExists(file);
        if (!file.isDirectory()) throw new IOException("Not a directory: " + file.getAbsolutePath());
    }
}
