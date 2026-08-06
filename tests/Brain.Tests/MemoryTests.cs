using Brain.Memory;
using Xunit;

namespace Brain.Tests;

public class MemoryTests
{
    [Fact]
    public void ShortTermMemory_Add_StoresEntry()
    {
        var stm = new ShortTermMemory(100);
        var entry = new MemoryEntry { Content = "test entry", RelevanceScore = 0.8f };

        stm.Add(entry);

        Assert.Equal(1, stm.Count);
        Assert.Equal("test entry", stm.GetAll()[0].Content);
    }

    [Fact]
    public void ShortTermMemory_Prune_RemovesOldestWhenFull()
    {
        var stm = new ShortTermMemory(5);
        for (int i = 0; i < 10; i++)
            stm.Add(new MemoryEntry { Content = "entry " + i });

        Assert.Equal(5, stm.Count);
        Assert.Equal("entry 5", stm.GetAll()[0].Content);
    }

    [Fact]
    public void ShortTermMemory_GetHighRelevance_FiltersByThreshold()
    {
        var stm = new ShortTermMemory(100);
        stm.Add(new MemoryEntry { Content = "low", RelevanceScore = 0.3f });
        stm.Add(new MemoryEntry { Content = "high", RelevanceScore = 0.9f });

        var result = stm.GetHighRelevance(0.7f);

        Assert.Single(result);
        Assert.Equal("high", result.First().Content);
    }

    [Fact]
    public void LongTermMemory_StoreAndQuery_ReturnsSimilarEntries()
    {
        var ltm = new LongTermMemory(100);
        var embedding1 = new float[] { 1, 0, 0, 0 };
        var embedding2 = new float[] { 0, 1, 0, 0 };
        var embedding3 = new float[] { 0.9f, 0.1f, 0, 0 };

        ltm.Store(new MemoryEntry { Content = "entry1", Embedding = embedding1 });
        ltm.Store(new MemoryEntry { Content = "entry2", Embedding = embedding2 });

        var results = ltm.Query(embedding3, topK: 1);

        Assert.Single(results);
        Assert.Equal("entry1", results[0].Content);
    }

    [Fact]
    public void LongTermMemory_QueryByText_FindsMatches()
    {
        var ltm = new LongTermMemory(100);
        ltm.Store(new MemoryEntry { Content = "drift prediction for index 10", Embedding = new float[] { 1 } });
        ltm.Store(new MemoryEntry { Content = "market analysis report", Embedding = new float[] { 1 } });

        var results = ltm.QueryByText("drift");

        Assert.Single(results);
        Assert.Contains("drift", results[0].Content);
    }
}
