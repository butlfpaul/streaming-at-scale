namespace EventHubToDigitalTwins
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core.Pipeline;
    using Azure.DigitalTwins.Core;
    using Azure.Identity;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;

    public class EventHubToDigitalTwins
    {
        //Your Digital Twin URL is stored in an application setting in Azure Functions
        private static readonly string AdtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL") ??
                                                        throw new InvalidOperationException(
                                                            "Application setting \"ADT_SERVICE_URL\" not set");

        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly DigitalTwinsClient Client;

        static EventHubToDigitalTwins()
        {
            //Authenticate with Digital Twins
            var cred = new DefaultAzureCredential();
            Client = new DigitalTwinsClient(new Uri(AdtInstanceUrl), cred,
                new DigitalTwinsClientOptions {Transport = new HttpClientTransport(HttpClient)});
        }

        [FunctionName("EventHubToDigitalTwins")]
        public async Task Run([EventHubTrigger("", Connection = "EVENT_HUB")]
            EventData[] events, ILogger log)
        {
            try
            {
                foreach (var eventData in events)
                {
                    if (eventData == null || eventData.Body == null)
                    {
                        continue;
                    }
                    await ProcessEvent(eventData, log);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
            }
        }

        private static async Task ProcessEvent(EventData eventData, ILogger log)
        {
            try
            {
                var body = JsonDocument.Parse(eventData.Body).RootElement;
                // Avoid this error: {"error":{"code":"InvalidArgument","message":"Invalid twin ID specified. Twin ID must be less than 128 characters, and only include the following characters: A-Z a-z 0-9 - . +% _ # * ? ! ( ) , = @ $ '"}}
                var deviceId = body.GetProperty("deviceId").GetString().Replace("://", "-");
                var deviceType = body.GetProperty("type").GetString();
                var twinId = ToTwinId(deviceId, deviceType);
                log.LogInformation($"DeviceId:{deviceId}. TwinId:{twinId}. DeviceType:{deviceType}");
                var updateTwinData = new JsonPatchDocument();
                switch (deviceType)
                {
                    case "TEMP":
                        updateTwinData.AppendAdd("/Temperature", body.GetProperty("value").GetDouble());
                        updateTwinData.AppendAdd("/TemperatureData", body.GetProperty("complexData"));
                        await Client.UpdateDigitalTwinAsync(twinId, updateTwinData);
                        break;
                    case "CO2":
                        updateTwinData.AppendAdd("/CO2", body.GetProperty("value").GetDouble());
                        updateTwinData.AppendAdd("/CO2Data", body.GetProperty("complexData"));
                        await Client.UpdateDigitalTwinAsync(twinId, updateTwinData);
                        break;
                }
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
            }
        }

        /*
         * Map deviceId to a valid twinId by distributing the incoming data among the 5 twins available
         */
        private static string ToTwinId(string deviceId, string deviceType)
        {
            var regex = new Regex(".*device-id-(\\d+)");
            var match = regex.Match(deviceId);
            if (!match.Success || match.Groups.Count <= 1)
            {
                throw new ArgumentException($"Invalid deviceId: {deviceId}");
            }

            var idNumber = int.Parse(match.Groups[1].Value);
            return $"{deviceType.ToLower()}-sensor-{idNumber % 5}";
        }
    }
}