using System;
using System.Collections.Generic;

namespace BrainSystem.NeuralNetworks;

/// <summary>
/// Spawns the 1000+ specialised networks organised as a biologically-inspired brain.
/// Cortical regions: sensory, motor, language, memory, executive, emotion, prediction, etc.
/// </summary>
public static class BrainFactory
{
    public record RegionSpec(string Domain, int Count, int[] Layers, string Purpose);

    public static readonly RegionSpec[] DefaultBrain = new[]
    {
        // Sensory cortices
        new RegionSpec("visual_cortex",       120, new[]{ 64, 128, 64, 32 }, "Visual pattern encoding"),
        new RegionSpec("auditory_cortex",      80, new[]{ 64,  96, 48, 24 }, "Auditory features"),
        new RegionSpec("somatosensory",        60, new[]{ 32,  64, 32, 16 }, "Tactile / body signals"),
        new RegionSpec("olfactory",            20, new[]{ 16,  32, 16,  8 }, "Chemical / smell features"),
        // Language & symbolic
        new RegionSpec("wernicke_area",        80, new[]{128, 256,128, 64 }, "Language comprehension"),
        new RegionSpec("broca_area",           80, new[]{ 64, 128, 96, 32 }, "Language production"),
        new RegionSpec("semantic_net",        120, new[]{128, 192,128, 64 }, "Semantic embedding"),
        // Motor
        new RegionSpec("motor_cortex",         60, new[]{ 32,  64, 32, 16 }, "Motor planning"),
        new RegionSpec("cerebellum",           60, new[]{ 32,  64, 32, 16 }, "Fine motor coordination"),
        // Memory
        new RegionSpec("hippocampus",          80, new[]{128, 256,128, 64 }, "Episodic memory encode"),
        new RegionSpec("entorhinal",           40, new[]{ 64, 128, 64, 32 }, "Memory indexing"),
        // Executive
        new RegionSpec("prefrontal_cortex",    80, new[]{128, 256,128, 32 }, "Planning / reasoning"),
        new RegionSpec("anterior_cingulate",   40, new[]{ 64, 128, 32,  8 }, "Attention / error monitor"),
        // Affective
        new RegionSpec("amygdala",             30, new[]{ 32,  64, 16,  8 }, "Emotional salience"),
        new RegionSpec("nucleus_accumbens",    20, new[]{ 32,  64, 16,  4 }, "Reward prediction"),
        // Predictive / world model
        new RegionSpec("world_model",         100, new[]{128, 256,256,128 }, "Predictive coding"),
        // Meta
        new RegionSpec("meta_controller",      30, new[]{ 64, 128, 64, 16 }, "Which sub-net to trust"),
    };

    public static int BuildDefaultBrain(NetworkRegistry reg)
    {
        int total = 0;
        int seed = 1000;
        foreach (var r in DefaultBrain)
        {
            int c = r.Count;
            reg.Populate(r.Domain, c, i =>
                ($"{r.Domain}_{i:D3}", $"{r.Purpose} unit {i}", r.Layers), seed);
            total += c;
            seed += c;
        }
        return total;
    }
}
