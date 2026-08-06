using System.Text.Json;
using DsiAgentic.Agents;
using DsiAgentic.Core;
using DsiAgentic.Divergence;
using DsiAgentic.Orchestrator;

namespace DsiAgentic.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return 0; }
        var root = FindRoot();
        var cfgPath = Path.Combine(root, "config", "instruments.json");
        var dataDir = Path.Combine(root, "data");
        var cfg = Configuration.Load(cfgPath);
        await using var orch = new MasterOrchestrator(cfg, dataDir);

        var cmd = args[0].ToLowerInvariant();
        var pretty = new JsonSerializerOptions { WriteIndented = true };
        try
        {
            switch (cmd)
            {
                case "ping":
                {
                    var ok = await orch.Client.PingAsync();
                    Console.WriteLine($"ping: {ok}");
                    break;
                }
                case "price":
                {
                    var symbol = ResolveSymbol(cfg, args[1]);
                    var q = await orch.Client.GetQuoteAsync(symbol);
                    Console.WriteLine($"{symbol}: {q}");
                    break;
                }
                case "scan":
                {
                    var list = await orch.ScanAllAsync();
                    Console.WriteLine(JsonSerializer.Serialize(list, pretty));
                    break;
                }
                case "signal":
                {
                    var inst = cfg.Instruments.First(i => i.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase));
                    var s = await orch.ScanInstrumentAsync(inst);
                    Console.WriteLine(JsonSerializer.Serialize(s, pretty));
                    break;
                }
                case "agents":
                {
                    var inst = cfg.Instruments.First(i => i.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase));
                    var mtf = await orch.LoadMtfAsync(inst);
                    var conf = orch.Colony.Aggregate(mtf);
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        instrument = inst.Name,
                        direction = conf.Direction.ToString(),
                        net = conf.NetScore,
                        bull = conf.BullScore,
                        bear = conf.BearScore,
                        agents = conf.AgentsFired,
                        families = conf.Families.ToDictionary(k => k.Key, v => new { bull = v.Value.bull, bear = v.Value.bear }),
                        top_votes = conf.Votes.OrderByDescending(v => v.Weight * v.Confidence).Take(15)
                    }, pretty));
                    break;
                }
                case "divergence":
                {
                    var inst = cfg.Instruments.First(i => i.Name.Equals(args[1], StringComparison.OrdinalIgnoreCase));
                    var mtf = await orch.LoadMtfAsync(inst);
                    var events = new List<DivergenceEvent>();
                    foreach (var kv in mtf) events.AddRange(DivergenceEngine.Detect(kv.Value));
                    Console.WriteLine(JsonSerializer.Serialize(events.OrderByDescending(e => e.Score).Take(20), pretty));
                    break;
                }
                case "brain":
                {
                    var name = args[1];
                    if (!orch.Brains.TryGetValue(name, out var b)) { Console.WriteLine("brain not found"); return 1; }
                    var inst = cfg.Instruments.First(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    var mtf = await orch.LoadMtfAsync(inst);
                    var primary = mtf[inst.Timeframes[0]];
                    var feats = DsiAgentic.Brains.FeatureExtractor.Extract(primary);
                    var d = b.DetailedPredict(feats);
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        instrument = name,
                        updates = b.TotalUpdates,
                        outcome = d.outcome,
                        trend = d.trend,
                        reversal = d.reversal,
                        volatility = d.vol,
                        regime = d.regime,
                        meta = d.meta
                    }, pretty));
                    break;
                }
                case "poll":
                {
                    var closed = await orch.PollOpenAsync();
                    Console.WriteLine($"closed: {closed.Count}");
                    Console.WriteLine(JsonSerializer.Serialize(closed, pretty));
                    break;
                }
                case "open":
                {
                    Console.WriteLine(JsonSerializer.Serialize(orch.Store.LoadOpen(), pretty));
                    break;
                }
                case "history":
                {
                    var n = args.Length > 1 && int.TryParse(args[1], out var v) ? v : 20;
                    Console.WriteLine(JsonSerializer.Serialize(orch.Store.ReadHistory(n).ToList(), pretty));
                    break;
                }
                case "perf":
                {
                    Console.WriteLine(JsonSerializer.Serialize(orch.Performance.Data, pretty));
                    break;
                }
                case "watch":
                {
                    var intervalMs = args.Length > 1 && int.TryParse(args[1], out var v) ? v : 30_000;
                    Console.WriteLine($"watch loop every {intervalMs}ms  (ctrl-c to stop)");
                    while (true)
                    {
                        var newSignals = await orch.ScanAllAsync();
                        var closed = await orch.PollOpenAsync();
                        Console.WriteLine($"[{DateTime.UtcNow:O}] new={newSignals.Count} closed={closed.Count}");
                        await Task.Delay(intervalMs);
                    }
                }
                case "knowledge":
                {
                    var inst = args[1];
                    var n = args.Length > 2 && int.TryParse(args[2], out var v) ? v : 5;
                    foreach (var line in orch.Knowledge.Read(inst, n)) Console.WriteLine(line);
                    break;
                }
                default:
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
        return 0;
    }

    private static string ResolveSymbol(RootConfig cfg, string name)
    {
        var inst = cfg.Instruments.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return inst?.Symbol ?? name;
    }

    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "DsiAgentic.sln"))) return dir;
            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }
        return Directory.GetCurrentDirectory();
    }

    private static void PrintHelp() => Console.WriteLine(@"DsiAgentic CLI

  dsi ping                      Test connection to Deriv WebSocket
  dsi price <NAME>              Fetch latest quote (DSI10 / XAUUSD / ...)
  dsi scan                      Scan every instrument and emit signals
  dsi signal <NAME>              Generate a signal for one instrument
  dsi agents <NAME>             Show agent colony breakdown
  dsi divergence <NAME>         Show top divergence events across MTF
  dsi brain <NAME>              Show 6-sub-brain ensemble prediction
  dsi poll                      Poll live quotes and close TP/SL
  dsi open                      Show open signals
  dsi history [N]               Show last N closed signals (default 20)
  dsi perf                      Show performance per instrument
  dsi knowledge <NAME> [N]      Show last N knowledge rows
  dsi watch [intervalMs]        Continuous scan+poll loop");
}
