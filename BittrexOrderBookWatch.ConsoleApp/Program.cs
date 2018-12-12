using System;
using System.Collections.Generic;
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
            var buckets = new List<Bucket>();

            Bucket currBucket = null;
            var nextRate = 0m;
            var rateIncrement = 0.1m;
            var maxRate = 2m;

            foreach (var resultItem in resultItems)
            {
                total = resultItem.Rate * resultItem.Quantity;
                sum += total;

                if (currBucket == null)
                {
                    currBucket = new Bucket() { Rate = resultItem.Rate, SizeXrp = resultItem.Quantity, Total = total, Sum = sum };
                    nextRate += resultItem.Rate + rateIncrement;
                    buckets.Add(currBucket);
                }

                if (resultItem.Rate >= nextRate)
                {
                    currBucket = new Bucket() { Rate = nextRate, SizeXrp = 0m, Total = 0m, Sum = 0m };
                    nextRate += rateIncrement;
                    buckets.Add(currBucket);
                    Console.WriteLine($"new bucket @ {currBucket.Rate}");
                }

                currBucket.SizeXrp += resultItem.Quantity;
                currBucket.Total += total;
                currBucket.Sum += sum;

                //Console.WriteLine($"askUSD={resultItem.Rate}, sizeXRP={resultItem.Quantity}, totalUSD={total}, sumUSD={sum}");

                if (resultItem.Rate >= maxRate)
                {
                    break;
                }
            }



            Console.WriteLine("-------------------------------------------");

            foreach(var b in buckets)
            {
                Console.WriteLine($"askUSD={b.Rate}, sizeXRP={b.SizeXrp}, totalUSD={b.Total}, sumUSD={b.Sum}");
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

    public class Bucket
    {
        public decimal Rate { get; set; }
        public decimal SizeXrp { get; set; }
        public decimal Total { get; set; }
        public decimal Sum { get; set; }
    }
}
