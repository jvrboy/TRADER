using Brain.API.Services;

namespace Brain.Launcher;

/// <summary>
/// Console host entry point for the BrainSystem.
/// Can run as a console application or be installed as a Windows Service.
/// </summary>
public sealed class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  BrainSystem v1.0 - Intelligent Agent");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var orchestrator = new OrchestrationService();

        Console.WriteLine("System initialized:");
        Console.WriteLine("  Neural networks: " + orchestrator.Ensemble.Count);
        Console.WriteLine("  Tools registered: " + orchestrator.ToolRegistry.GetToolNames().Count);
        Console.WriteLine("  LLM loaded: " + orchestrator.LlmRunner.IsLoaded);
        Console.WriteLine();

        if (args.Length > 0 && args[0] == "--api")
        {
            Console.WriteLine("Starting API server...");
            await StartApiServer();
            return;
        }

        // Interactive mode
        Console.WriteLine("Interactive mode. Type 'exit' to quit, 'help' for commands.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;
            if (input == "exit" || input == "quit") break;

            if (input == "help")
            {
                PrintHelp();
                continue;
            }

            if (input == "status")
            {
                var status = orchestrator.GetStatus();
                Console.WriteLine("Status: " + status.Status);
                Console.WriteLine("Networks: " + status.NetworkCount);
                Console.WriteLine("LLM loaded: " + status.ModelLoaded);
                continue;
            }

            if (input.StartsWith("predict "))
            {
                var indexStr = input.Substring(8).Trim();
                if (int.TryParse(indexStr, out var index))
                {
                    var prediction = orchestrator.Predict(index);
                    Console.WriteLine("Drift Index " + prediction.DriftIndex + ": " + prediction.Direction +
                        " (confidence: " + prediction.Confidence.ToString("P") + ")");
                }
                else
                {
                    Console.WriteLine("Invalid index. Use 10, 20, or 30.");
                }
                continue;
            }

            if (input.StartsWith("train"))
            {
                Console.WriteLine("Starting training on indices 10, 20, 30...");
                var result = await orchestrator.Train(null, 5, 0.001f);
                Console.WriteLine("Training " + (result.Success ? "completed" : "failed") + " in " + result.TrainingTimeMs + "ms");
                foreach (var r in result.Results)
                {
                    Console.WriteLine("  Index " + r.DriftIndex + ": val=" + r.ValidationAccuracy.ToString("P") +
                        " test=" + r.TestAccuracy.ToString("P"));
                }
                continue;
            }

            // Default: chat
            var response = await orchestrator.Chat(input);
            Console.WriteLine("Assistant: " + response.Reply);
            if (response.ToolCalls.Count > 0)
            {
                Console.WriteLine("  Tools used: " + string.Join(", ", response.ToolCalls.Select(t => t.Tool)));
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  status       - Show system status");
        Console.WriteLine("  predict <n>  - Predict drift for index 10, 20, or 30");
        Console.WriteLine("  train        - Train the ensemble on drift indices");
        Console.WriteLine("  help         - Show this help");
        Console.WriteLine("  exit         - Quit the application");
        Console.WriteLine("  <message>    - Chat with the agent");
    }

    private static async Task StartApiServer()
    {
        var apiProjectPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Brain.API");
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project " + apiProjectPath,
            UseShellExecute = false
        };
        var process = System.Diagnostics.Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }
}
