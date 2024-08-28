using HivePodpingAPI;

namespace HivePodpingExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Start reading those Podpings!");

            //use your own HttpClient and list of addresses for flexibility
            HttpClient httpClient = new HttpClient();
            List<string> addressList = new List<string>
            {
                "https://api.openhive.network",
                "https://api.hive.blog",
                "https://anyx.io",
                "https://api.deathwing.me"
            };

            HiveClient client = new HiveClient(httpClient, addressList);

            long lastBlockId = 88430996; //for instance

            await client.StreamPodpingBlocksAsync(lastBlockId);


            Console.ReadLine();
        }
    }
}
