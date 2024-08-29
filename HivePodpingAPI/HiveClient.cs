using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace HivePodpingAPI
{
    public class HiveClient : HiveAPI
    {
        private const string DATABASE_API = "database_api.";
        private List<string> _urls;
        private int _currentUrlIndex;
        public HiveClient(HttpClient oHttpClient, List<string> urls) : base(oHttpClient, urls[0])
        {
            _urls = urls;
            _currentUrlIndex = 0;
        }

        private void SwitchToNextUrl()
        {
            //this resets to 0 when we reach the end of the _urls collection
            _currentUrlIndex = (_currentUrlIndex + 1) % _urls.Count;
            base.SetUrl(_urls[_currentUrlIndex]);
        }

        private JObject CallApiWithFailover(string method, ArrayList parameters = null)
        {
            while (true)
            {
                try
                {
                    return call_api(method, parameters);
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine($"Failed to call API at {_urls[_currentUrlIndex]}, switching to next URL.");
                    SwitchToNextUrl();
                }
            }
        }

        #region Private Methods

        private List<JObject> HandleBlock(JObject block)
        {
            var podpingBlocks = new List<JObject>();

            try
            {
                string timestamp = block["timestamp"].ToString();
                JArray transactions = (JArray)block["transactions"];

                foreach (JObject transaction in transactions)
                {
                    var podpingTransactions = HandleTransaction(transaction, timestamp);
                    podpingBlocks.AddRange(podpingTransactions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling block");
                Console.WriteLine(ex);
            }

            return podpingBlocks;
        }

        private List<JObject> HandleTransaction(JObject transaction, string timestamp)
        {
            var podpingTransactions = new List<JObject>();

            long blockNumber = transaction["block_num"].Value<long>();
            string transactionId = transaction["transaction_id"].ToString();

            JArray operations = (JArray)transaction["operations"];
            foreach (JArray operation in operations)
            {
                var podpingOperation = HandleOperation(operation, timestamp, blockNumber, transactionId);
                if (podpingOperation != null)
                {
                    podpingTransactions.Add(podpingOperation);
                }
            }

            return podpingTransactions;
        }

        private JObject HandleOperation(JArray operation, string timestamp, long blockNumber, string transactionId)
        {
            string operationType = operation[0].ToString();
            if (operationType == "custom_json")
            {
                JObject post = (JObject)operation[1];
                return HandleCustomJsonPost(post, timestamp, blockNumber, transactionId);
            }

            return null;
        }

        private JObject HandleCustomJsonPost(JObject post, string timestamp, long blockNumber, string transactionId)
        {
            if (post["id"].ToString() == "podping" || post["id"].ToString().StartsWith("pp_"))
            {
                var postJson = JObject.Parse(post["json"].ToString());

                var version = postJson["version"]?.ToString() ?? postJson["v"]?.ToString();
                var updateReason = postJson["reason"]?.ToString() ?? postJson["r"]?.ToString() ?? postJson["type"]?.ToString();
                var medium = postJson["medium"]?.ToString();

                if (!float.TryParse(version, out float versionValue))
                {
                    if (updateReason != null && updateReason != "feed_update" && updateReason != "1")
                        return null;
                }
                else
                {
                    var podpingReasons = new List<string> { "live", "liveEnd", "update" };
                    var podpingMediums = new List<string> { "podcast", "audiobook", "blog", "film", "music", "newsletter", "video" };

                    if (!podpingReasons.Contains(updateReason) || !podpingMediums.Contains(medium))
                        return null;
                }

                return postJson;
            }

            return null;
        }

        #endregion

        #region Public Methods

        public async Task StreamPodpingBlocksAsync(long lastBlockNumber = 1)
        {
            while (true)
            {
                JObject dynamicGlobalProperties = call_api("condenser_api.get_dynamic_global_properties");
                long headBlockNumber = dynamicGlobalProperties["head_block_number"].Value<long>();

                for (long blockNumber = lastBlockNumber + 1; blockNumber <= headBlockNumber; blockNumber++)
                {
                    try
                    {
                        JObject block = CallApiWithFailover("condenser_api.get_block", new ArrayList { blockNumber });
                        var podpingBlocks = HandleBlock(block);

                        foreach (var podpingBlock in podpingBlocks)
                        {
                            // Process each podping block as needed
                            Console.WriteLine($"Processed podping block: {podpingBlock}");

                            var iris = podpingBlock["iris"]?.ToObject<List<string>>() ?? new List<string>();
                            var urls = podpingBlock["urls"]?.ToObject<List<string>>() ?? new List<string>();

                            if (urls.Count > 0)
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

                        await Task.Delay(350); //wait to give the API some room and not to overload it
                        Console.WriteLine($"Processed block {blockNumber}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception processing {blockNumber}, " + ex.Message);
                    }
                }

                lastBlockNumber = headBlockNumber;
                await Task.Delay(3000); // Wait for 3 seconds before checking for new blocks
            }
        }


        public async IAsyncEnumerable<JObject> StreamPodpingBlocksAsStreamAsync(long lastBlockNumber = 1)
        {
            while (true)
            {
                JObject dynamicGlobalProperties = call_api("condenser_api.get_dynamic_global_properties");
                long headBlockNumber = dynamicGlobalProperties["head_block_number"].Value<long>();

                for (long blockNumber = lastBlockNumber + 1; blockNumber <= headBlockNumber; blockNumber++)
                {
                    List<JObject> podpingBlocks = null;
                    try
                    {
                        JObject block = CallApiWithFailover("condenser_api.get_block", new ArrayList { blockNumber });
                        podpingBlocks = HandleBlock(block);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception processing {blockNumber}, " + ex.Message);
                    }

                    if (podpingBlocks != null)
                    {
                        foreach (var podpingBlock in podpingBlocks)
                        {
                            // Add blockNumber to the podpingBlock
                            podpingBlock["blockNumber"] = blockNumber;
                            yield return podpingBlock;
                        }
                    }

                    await Task.Delay(350); //wait to give the API some room and not to overload it
                }

                lastBlockNumber = headBlockNumber;
                await Task.Delay(300); // Wait for 3 seconds before checking for new blocks
            }
        }

        public JObject get_dynamic_global_properties()
        {
            return call_api(DATABASE_API + MethodBase.GetCurrentMethod().Name);
        }

        #endregion
    }
}
