## Overview
The MQTT Publisher Application is a .NET console application that connects to an MQTT broker and publishes messages loaded from a JSON file. It is designed to handle multiple client connections and send messages in multiple rounds based on configuration settings.

## Configuration
The application is configurable through a JSON file named `config.json`. Below is a description of each configuration parameter:

- `Topic`: MQTT topic under which the messages will be published.
- `MessageCount`: Number of message sending rounds.
- `IntervalSeconds`: Interval in seconds between each round.
- `Username`: Username for MQTT broker authentication.
- `Password`: Password for MQTT broker authentication.
- `MessagePath`: JSON path to select specific data from the JSON file or "all" to send the entire file.
- `Protocol`: Connection protocol ("TCP" or "WS" for WebSocket).
- `MQTTbrokerAddress`: Address of the MQTT broker.
- `UseUrls`: URLs to bind the integrated web server.
- `ClientCount`: Number of MQTT clients to connect to the broker.

Example configuration:
```json
{
  "Topic": "yourtopic",
  "MessageCount": 10,
  "IntervalSeconds": 10,
  "Username": "yourusername",
  "Password": "yourpassword",
  "MessagePath": "all",
  "Protocol": "WS",
  "MQTTbrokerAddress": "yourbrokeraddress",
  "UseUrls": "http://localhost:5000",
  "ClientCount": 1
}

```

## Usage

To run the application, follow these steps:

1. Ensure you have .NET SDK installed on your machine.
2. Place the `config.json` and `data.json` (which should contain the JSON data to be sent as messages) in the root directory of the application.
3. From the command line, navigate to the directory containing the application and run:
   
```bash
dotnet run
```

4. The application will start and begin connecting to the MQTT broker, sending messages according to the configuration.

## Dependencies

This application uses several external libraries:

- MQTTnet: For handling MQTT client functionality.
- Newtonsoft.Json: For JSON parsing.
- Microsoft.AspNetCore: For hosting a web server alongside the MQTT client.
Make sure these dependencies are included in your project file (.csproj) and are restored using the dotnet restore command before running the application.