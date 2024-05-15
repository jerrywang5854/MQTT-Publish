using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MqttPublisher;
using System.Threading;

[ApiController]
[Route("[controller]")]

public class ApiController : ControllerBase
{
    [HttpGet]
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        try
        {
            // Read the contents of the config.json file
            string configJson = System.IO.File.ReadAllText("config.json");
            
            // Convert a JSON string to an object for return in a structured format
            var configData = JsonSerializer.Deserialize<MqttConfig>(configJson);

            return Ok(configData);
        }
        catch (Exception ex)
        {
            // If an error occurs while reading the file, an error message is returned
            return StatusCode(500, $"Error reading config file: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateConfig(MqttConfig newConfig)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true // Set to true to format JSON
        };
         
        // Serialize new configuration to JSON
        string newJson = JsonSerializer.Serialize(newConfig, options);

        // Write to config.json file
        System.IO.File.WriteAllText("config.json", newJson);

        string mqttConfigJson = System.IO.File.ReadAllText("config.json"); 
        MqttConfig mqttConfig = System.Text.Json.JsonSerializer.Deserialize<MqttConfig>(mqttConfigJson);
        
        // Increment count when thread starts
        ThreadCounter.Increment();
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started.");

        var mqttTask = Task.Run(async () => await Program.RunMqttClient(mqttConfig));
        mqttTask.ContinueWith(t =>
        {
            // Decrement count when thread is about to end
            ThreadCounter.Decrement();
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} stopped.");
        });
        
        return Ok("Config updated and message sent in a new task.");
    }
}

public static class ThreadCounter
{
    private static int activeThreads = 0;

    // Increase active thread count
    public static void Increment()
    {
        Interlocked.Increment(ref activeThreads);
    }

    // Reduce active thread count
    public static void Decrement()
    {
        Interlocked.Decrement(ref activeThreads);
    }

    // Get the number of currently active threads
    public static int GetActiveThreadCount()
    {
        return activeThreads;
    }
}

