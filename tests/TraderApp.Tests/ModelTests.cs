using TraderUI.Models;
using Xunit;

namespace TraderApp.Tests;

public class ModelTests
{
    [Fact]
    public void Quote_DefaultProperties_AreCorrect()
    {
        var quote = new Quote();
        Assert.Equal("", quote.Symbol);
        Assert.Equal(0m, quote.Price);
        Assert.Equal(0m, quote.Change);
        Assert.Equal(0m, quote.ChangePercent);
        // Change=0 means IsPositive is true (>= 0)
    }

    [Fact]
    public void Quote_IsPositive_ReturnsTrue_WhenChangePositive()
    {
        var quote = new Quote { Change = 1.5m };
        Assert.True(quote.IsPositive);
    }

    [Fact]
    public void Quote_IsPositive_ReturnsTrue_WhenChangeZero()
    {
        var quote = new Quote { Change = 0m };
        Assert.True(quote.IsPositive);
    }

    [Fact]
    public void Quote_IsPositive_ReturnsFalse_WhenChangeNegative()
    {
        var quote = new Quote { Change = -0.5m };
        Assert.False(quote.IsPositive);
    }

    [Fact]
    public void Quote_ChangeDisplay_FormatsCorrectly()
    {
        var positive = new Quote { Change = 1.2345m };
        Assert.StartsWith("+", positive.ChangeDisplay);

        var negative = new Quote { Change = -0.5m };
        Assert.StartsWith("-", negative.ChangeDisplay);
    }

    [Fact]
    public void Quote_ChangePercentDisplay_FormatsCorrectly()
    {
        var quote = new Quote { ChangePercent = 2.567m };
        var display = quote.ChangePercentDisplay;
        Assert.StartsWith("+", display, StringComparison.Ordinal);
        Assert.Contains("%", display);
    }

    [Fact]
    public void Quote_PriceDisplay_FormatsToFiveDecimals()
    {
        var quote = new Quote { Price = 1.08542m };
        Assert.Equal("1.08542", quote.PriceDisplay);
    }

    [Fact]
    public void Quote_PropertyChanged_FiresOnSet()
    {
        var quote = new Quote();
        bool fired = false;
        quote.PropertyChanged += (s, e) => fired = true;
        quote.Symbol = "EURUSD";
        Assert.True(fired);
    }

    [Fact]
    public void Quote_PropertyChanged_DoesNotFireOnSameValue()
    {
        var quote = new Quote { Symbol = "EURUSD" };
        bool fired = false;
        quote.PropertyChanged += (s, e) => fired = true;
        quote.Symbol = "EURUSD";
        Assert.False(fired);
    }

    [Fact]
    public void OhlcBar_IsUp_TrueWhenCloseAboveOpen()
    {
        var bar = new OhlcBar { Open = 100m, Close = 105m };
        Assert.True(bar.IsUp);
    }

    [Fact]
    public void OhlcBar_IsUp_FalseWhenCloseBelowOpen()
    {
        var bar = new OhlcBar { Open = 105m, Close = 100m };
        Assert.False(bar.IsUp);
    }

    [Fact]
    public void OhlcBar_IsUp_TrueWhenCloseEqualsOpen()
    {
        var bar = new OhlcBar { Open = 100m, Close = 100m };
        Assert.True(bar.IsUp);
    }

    [Fact]
    public void Timeframe_All_ReturnsExpectedCount()
    {
        var all = Timeframe.All;
        Assert.Equal(7, all.Count);
    }

    [Fact]
    public void Timeframe_All_ContainsExpectedLabels()
    {
        var all = Timeframe.All;
        var labels = all.Select(t => t.Label).ToList();
        Assert.Contains("1m", labels);
        Assert.Contains("5m", labels);
        Assert.Contains("15m", labels);
        Assert.Contains("1H", labels);
        Assert.Contains("4H", labels);
        Assert.Contains("1D", labels);
        Assert.Contains("1W", labels);
    }

    [Fact]
    public void ChatMessage_DefaultProperties()
    {
        var msg = new ChatMessage();
        Assert.Equal(MessageRole.User, msg.Role);
        Assert.Equal("", msg.Content);
        Assert.NotNull(msg.QuickActions);
        Assert.False(msg.IsLoading);
    }

    [Fact]
    public void Signal_DefaultProperties()
    {
        var signal = new Signal();
        Assert.NotEqual(default, signal.Id);
        Assert.NotEmpty(signal.Id);
        Assert.Equal(SignalDirection.Buy, signal.Direction);
        Assert.Equal(SignalStatus.Live, signal.Status);
        Assert.Equal(ConfidenceLevel.Low, signal.Confidence);
    }

    [Fact]
    public void AppSettings_DefaultProperties()
    {
        var settings = new AppSettings();
        Assert.Equal("OpenAI", settings.SelectedAiProvider);
        Assert.Equal("gpt-4o", settings.SelectedAiModel);
    }
}
