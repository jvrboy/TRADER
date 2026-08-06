package com.jcharts;

import java.util.*;
import java.lang.reflect.*;
import org.junit.jupiter.api.Test;

public class TestRunner {
    static int passed = 0, failed = 0, errors = 0;
    static final List<String> failures = new ArrayList<>();

    public static void main(String[] args) throws Exception {
        String testPkg = "com.jcharts";
        ClassLoader cl = TestRunner.class.getClassLoader();
        List<Class<?>> testClasses = new ArrayList<>();
        String classpath = System.getProperty("java.class.path");
        for (String path : classpath.split(":")) {
            java.io.File dir = new java.io.File(path);
            if (!dir.isDirectory()) continue;
            findTestClasses(dir, testPkg, testClasses);
        }
        System.out.println("Found " + testClasses.size() + " test classes");
        for (Class<?> cls : testClasses) {
            System.out.println("\n--- " + cls.getSimpleName() + " ---");
            for (Method m : cls.getDeclaredMethods()) {
                if (m.isAnnotationPresent(Test.class)) {
                    runTest(cls, m);
                }
            }
        }
        System.out.println("\n========================================");
        System.out.println("Total: " + (passed + failed + errors) + " | Passed: " + passed + " | Failed: " + failed + " | Errors: " + errors);
        System.out.println("========================================");
        if (!failures.isEmpty()) {
            System.out.println("\nFAILURES:");
            for (String f : failures) System.out.println("  " + f);
        }
        System.exit(failed + errors > 0 ? 1 : 0);
    }

    static void findTestClasses(java.io.File dir, String pkg, List<Class<?>> list) {
        String pkgPath = pkg.replace('.', '/');
        java.io.File pkgDir = new java.io.File(dir, pkgPath);
        if (!pkgDir.exists()) return;
        for (java.io.File f : pkgDir.listFiles()) {
            if (f.isDirectory()) {
                findTestClasses(dir, pkg + "." + f.getName(), list);
            } else if (f.getName().endsWith("Test.class")) {
                String className = pkg + "." + f.getName().replace(".class", "");
                if (!className.equals("com.jcharts.TestRunner")) {
                    try { list.add(Class.forName(className)); } catch (Exception ignored) {}
                }
            }
        }
    }

    static void runTest(Class<?> cls, Method m) {
        String name = cls.getSimpleName() + "." + m.getName();
        try {
            Constructor<?> ctor = cls.getDeclaredConstructor();
            ctor.setAccessible(true);
            Object instance = ctor.newInstance();
            m.setAccessible(true);
            m.invoke(instance);
            passed++;
            System.out.println("  PASS: " + name);
        } catch (InvocationTargetException e) {
            Throwable cause = e.getCause();
            if (cause instanceof AssertionError) {
                failed++;
                String msg = cause.getMessage() != null ? cause.getMessage() : cause.toString();
                failures.add(name + ": " + msg);
                System.out.println("  FAIL: " + name + " - " + msg);
            } else {
                errors++;
                failures.add(name + ": ERROR - " + cause);
                System.out.println("  ERROR: " + name + " - " + cause);
            }
        } catch (Exception e) {
            errors++;
            failures.add(name + ": SETUP ERROR - " + e);
            System.out.println("  ERROR: " + name + " (setup) - " + e);
        }
    }
}