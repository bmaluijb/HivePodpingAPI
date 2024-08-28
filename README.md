# Hive.NET

Interact with the Hive blockchain using the Hive JSON RPC API (https://developers.hive.io/apidefinitions/#block_api.get_block_range).

Reference the project (or NuGet package) in your project and use the `HiveClient` class to interact with the Hive blockchain.)

Here's an example:

`
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
`

Have a podcast? Host it on www.podhome.fm - the most modern podcast hosting company.