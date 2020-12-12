using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DT = System.Data;
using QC = Microsoft.Data.SqlClient;

namespace Centric.TechDays.AzureFunctions
{
    public static class GiftAggregationFunctions
    {
        [FunctionName("giftCountByProductionLine")]
        public static async Task<IActionResult> GiftCountByProductionLine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting gift counts by production line");

            var query = "SELECT production_line, COUNT(*) AS gift_count" +
                        " FROM gifts" +
                        " GROUP BY production_line" +
                        " ORDER BY gift_count DESC";

            var result = await GetRows(query, reader => new {
                productionLine = reader.GetString(0),
                giftCount = reader.GetInt32(1)
            });

            return new OkObjectResult(result);
        }

        [FunctionName("firstGiftByProductionLine")]
        public static async Task<IActionResult> FirstGiftByProductionLine(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting first gift per production line");

            var query = "SELECT production_line, MIN(creation_date) AS first_gift_timestamp" +
                        " FROM gifts" +
                        " GROUP BY production_line" +
                        " ORDER BY first_gift_timestamp ASC";

            var result = await GetRows(query, reader => new {
                productionLine = reader.GetString(0),
                firstGiftTimestamp = reader.GetDateTime(1)
            });

            return new OkObjectResult(result);
        }

        private static async Task<List<dynamic>> GetRows(
            string query,
            Func<QC.SqlDataReader, dynamic> rowConverter)
        {
            var result = new List<dynamic>();
            var connectionString = Environment.GetEnvironmentVariable("GIFTS_DB_ADO_NET_CONNECTION_STRING");
            using (var connection = new QC.SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new QC.SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = DT.CommandType.Text;
                    command.CommandText = query;
                    QC.SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        result.Add(rowConverter(reader));
                    }
                }
            }

            return result;
        }
    }
}
