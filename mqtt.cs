using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace MqttPublisher
{
    class Program
    {
        private static MqttConfig mqttConfig;

        public static async Task Main(string[] args)
        {
            // Load MQTT configuration from file
            mqttConfig = LoadMqttConfig("config.json");

            // Setup and run the ASP.NET Core web host
            var host = CreateHostBuilder(args).Build();
            var mqttTask = RunMqttClient(mqttConfig);
            await Task.WhenAll(host.RunAsync(), mqttTask);
        }

        private static MqttConfig LoadMqttConfig(string filePath)
        {
            string configJson = File.ReadAllText(filePath);
            return System.Text.Json.JsonSerializer.Deserialize<MqttConfig>(configJson);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Configure the web server using the MQTT configuration
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(mqttConfig.UseUrls);
                });

        public static async Task RunMqttClient(MqttConfig config)
        {
            var factory = new MqttFactory();
            var mqttClients = InitializeMqttClients(factory, config);

            // Read and parse the JSON content
            JObject jsonData = LoadJsonData("data.json");

            await SendMessagesInRounds(mqttClients, config, jsonData);
            await DisconnectClients(mqttClients);
        }

        private static JObject LoadJsonData(string filePath)
        {
            string jsonContent = File.ReadAllText(filePath);
            return JObject.Parse(jsonContent);
        }

        private static List<IMqttClient> InitializeMqttClients(MqttFactory factory, MqttConfig config)
        {
            var mqttClients = new List<IMqttClient>();
            for (int i = 0; i < config.ClientCount; i++)
            {
                var mqttClient = factory.CreateMqttClient();
                IMqttClientOptions options = BuildMqttClientOptions(config);

                try
                {
                    mqttClient.ConnectAsync(options, CancellationToken.None).Wait();
                    Console.WriteLine($"Client {i} connected to the broker.");
                    mqttClients.Add(mqttClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client {i} connection failed: {ex.Message}");
                }
            }
            return mqttClients;
        }

        private static async Task SendMessagesInRounds(List<IMqttClient> mqttClients, MqttConfig config, JObject jsonData)
        {
            for (int round = 0; round < config.MessageCount; round++)
            {
                foreach (var client in mqttClients)
                {
                    string messagePayload = ComposeMessagePayload(jsonData, config);
                    await SendMqttMessage(messagePayload, client, config);
                }
                Console.WriteLine($"Waiting {config.IntervalSeconds} seconds before the next round...");
                await Task.Delay(TimeSpan.FromSeconds(config.IntervalSeconds));
            }
        }

        private static string ComposeMessagePayload(JObject jsonData, MqttConfig config)
        {
            if (config.MessagePath == "all")
            {
                return jsonData.ToString();
            }
            else
            {
                JToken value = jsonData.SelectToken(config.MessagePath);
                return value?.ToString() ?? throw new InvalidOperationException("No data found at the specified MessagePath.");
            }
        }

        private static async Task SendMqttMessage(string messagePayload, IMqttClient mqttClient, MqttConfig config)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(config.Topic)
                .WithPayload(messagePayload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            try
            {
                await mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine("Message sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Publishing failed: {ex.Message}");
            }
        }

        private static async Task DisconnectClients(List<IMqttClient> mqttClients)
        {
            foreach (var client in mqttClients)
            {
                try
                {
                    await client.DisconnectAsync();
                    Console.WriteLine("Client disconnected.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client disconnection failed: {ex.Message}");
                }
            }
        }

        private static IMqttClientOptions BuildMqttClientOptions(MqttConfig config)
        {
            if (config.Protocol == "TCP")
            {
                return new MqttClientOptionsBuilder()
                    .WithTcpServer(config.MQTTbrokerAddress, 1883)
                    .WithCredentials(config.Username, config.Password)
                    .WithCleanSession(false)
                    .WithClientId("mqtt-PUBLISH-1")
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                    .Build();
            }
            else if (config.Protocol == "WS")
            {
                string WSAddress = "ws://" + config.MQTTbrokerAddress + "/mqtt";
                return new MqttClientOptionsBuilder()
                    .WithWebSocketServer(WSAddress)
                    .WithCredentials(config.Username, config.Password)
                    .WithCleanSession()
                    .WithClientId("mqtt-PUBLISH-1")
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                    .Build();
            }
            else
            {
                throw new ArgumentException("Protocol configuration error.");
            }
        }
    }

    public class MqttConfig
    {
        public string Topic { get; set; }
        public int MessageCount { get; set; }
        public int IntervalSeconds { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string MessagePath { get; set; }
        public string Protocol { get; set; }
        public string MQTTbrokerAddress { get; set; }
        public string UseUrls { get; set; }
        public int ClientCount { get; set; }
    }
}
