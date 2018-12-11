using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BittrexOrderBookWatch.ConsoleApp
{
    class Program
    {
        private static string urlTemplate = "https://bittrex.com/api/v1.1/public/getorderbook?market={0}&type={1}";
        private static string marketUSDXRP = "USD-XRP";
        private static string orderBookTypeSell = "sell";

        static void Main(string[] args)
        {
            new Program().Run().Wait();

        }

        public async Task Run()
        {
            Console.WriteLine("Hello World!");

            var json = await RequestOrderBookJsonAsync(marketUSDXRP, orderBookTypeSell);
            Console.WriteLine($"json: {json}");

            var jo = JObject.Parse(json);
            var success = jo.GetValue("success").Value<bool>();
            var message = jo.GetValue("message").Value<string>();
            Console.WriteLine($"success={success}, message=[{message}]");

            var results = jo.GetValue("result").Value<JArray>();
            var resultItems = JsonConvert.DeserializeObject<ResultItem[]>(results.ToString());

            var total = 0m;
            var sum = 0m;


            foreach (var resultItem in resultItems)
            {
                total = resultItem.Rate * resultItem.Quantity;
                sum += total;

                Console.WriteLine($"askUSD={resultItem.Rate}, sizeXRP={resultItem.Quantity}, totalUSD={total}, sumUSD={sum}");
            }


            Console.WriteLine("BYE");
        }

        public async Task<string> RequestOrderBookJsonAsync (string market, string orderBookType)
        {
            var uri = new Uri(string.Format(urlTemplate, market, orderBookType));

            using (var client = new WebClient())
            {
                return await client.DownloadStringTaskAsync(uri);
            }
        }
    }

    public class Response
    {
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName ="message")]
        public string Message { get; set; }

        public ResultItem[] ResultItems { get; set; }
    }

    public class ResultItem
    {
        [JsonProperty(PropertyName = "quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty(PropertyName = "rate")]
        public decimal Rate { get; set; }
    }
}
