using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Centric.TechDays.AzureFunctions
{
    public class GiftMessage
    {
        public Guid MessageId;
        public Volume GiftBoundingBox;
        public string Recipient;
    }

    public class Volume
    {
        public double Length;
        public double Height;
        public double Width;
    }

    public class GiftMessageFactory
    {
        private static readonly List<string> NAMES = new List<string>()
        {
            // A Christmas Carol
            "Tiny Tim",
            // Oliver Twist
            "Oliver Twist",
            "Noah Claypole",
            "Artful Dodger",
            "Charley Bates",
            // Great Expectations
            "Philip Pirrip", // Pip
            "Estella Havisham",
            "Bentley Drummle",
            "Clara Barley",
            // David Copperfield
            "David Copperfield",
            "James Steerforth",
            "Tommy Traddles",
            "Agnes Wickfield",
            "Uriah Heep",
            "Dora Spenlow",
            // Nicholas Nickleby
            "Nicholas Nickleby",
            "Kate Nickleby",
            "Tilda Price",
            "Madeline Bray"
        };

        private Random random = new Random();
        private JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public GiftMessage RandomGiftMessage()
        {
            return new GiftMessage
            {
                MessageId = Guid.NewGuid(),
                Recipient = RandomRecipient(),
                GiftBoundingBox = new Volume
                {
                    Length = RandomDimension(),
                    Width = RandomDimension(),
                    Height = RandomDimension()
                }
            };
        }

        public string RandomGiftMessageJson()
        {
            return JsonConvert.SerializeObject(RandomGiftMessage(), serializerSettings);
        }

        private double RandomDimension()
        {
            return Math.Round(5 + random.NextDouble() * 15);
        }

        private string RandomRecipient()
        {
            return NAMES[random.Next(0, NAMES.Count)];
        }
    }

    public static class QueuePostingFunctions
    {
        [FunctionName("postGiftMessages")]
        public static async void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var giftMessageFactory = new GiftMessageFactory();

            var serviceBusConnectionString =
                    System.Environment.GetEnvironmentVariable("PRODUCTION_LINES_SB_CONNECTION_SEND_ONLY");
            var queueNamesString =
                    System.Environment.GetEnvironmentVariable("PRODUCTION_LINES_QUEUE_NAMES");
            var queueNames = queueNamesString.Split(new char[1] {','});

            await using (ServiceBusClient client = new ServiceBusClient(serviceBusConnectionString))
            {
                foreach (var queueName in queueNames)
                {
                    ServiceBusSender sender = client.CreateSender(queueName);
                    ServiceBusMessage message = new ServiceBusMessage(giftMessageFactory.RandomGiftMessageJson());
                    
                    await sender.SendMessageAsync(message);
                    log.LogInformation($"Sent a gift message to queue: {queueName}");
                }
            }
        }
    }
}
