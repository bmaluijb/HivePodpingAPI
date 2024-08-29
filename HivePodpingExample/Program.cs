using HivePodpingAPI;

namespace HivePodpingExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Start reading those Podpings!");

            long lastBlockId = 88430996; //for instance

            //use your own HttpClient and list of addresses for flexibility
            var httpClient = new HttpClient();
            var apiUrls = new List<string> { "https://api.hive.blog", "https://any.other.url" };
            var hiveClient = new HiveClient(httpClient, apiUrls);

            await foreach (var podpingBlock in hiveClient.StreamPodpingBlocksAsStreamAsync(lastBlockId))
            {
                // Process each podping block as needed
                Console.WriteLine($"{podpingBlock["blockNumber"]} Processed podping block: {podpingBlock}");
                
                var iris = podpingBlock["iris"]?.ToObject<List<string>>() ?? new List<string>();
                var urls = podpingBlock["urls"]?.ToObject<List<string>>() ?? new List<string>();

                if (urls.Count() > 0)
                {
                    iris.AddRange(urls);
                }

                if (podpingBlock["url"] != null)
                {
                    iris = new List<string> { podpingBlock["url"].ToString() };
                }

                foreach (var iri in iris)
                {
                    Console.WriteLine($"  - {iri}");
                }
            }


            Console.ReadLine();
        }
    }
}
