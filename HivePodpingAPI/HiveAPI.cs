using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HivePodpingAPI
{
	public class HiveAPI
	{
		const string STR_RESULT = "result";
		const string STR_ERROR = "error";
		const string STR_FORMAT = "Unexpected result format";

		private HttpClient m_oHttpClient;
		private string m_strURL;

		public HiveAPI(HttpClient oHttpClient, string strURL)
		{
			m_oHttpClient = oHttpClient;
			m_strURL = strURL;
		}


		#region Private methods
		private async Task<string> Post(string strMethod, ArrayList arrParams = null)
		{
            string strResult = string.Empty;
            Hashtable arrRequest = new Hashtable();

            arrRequest.Add("id", 1);
            arrRequest.Add("jsonrpc", "2.0");
            arrRequest.Add("method", strMethod);
            arrRequest.Add("params", arrParams ?? new ArrayList()); // Ensure params is always included

            string strJson = JsonConvert.SerializeObject(arrRequest);
            using (var oResponse = await m_oHttpClient.PostAsync(m_strURL, new StringContent(strJson, System.Text.Encoding.UTF8, "application/json")))
            {
                oResponse.EnsureSuccessStatusCode();
                strResult = await oResponse.Content.ReadAsStringAsync();
            }
            return strResult;
        }
		private string SendRequest(string strMethod, ArrayList aParams = null)
		{
			using(Task<string> t = Post(strMethod, aParams)) {
				t.Wait();
				return t.Result;
			}
		}
		private object ProcessResult(JObject obj)
		{
			if (obj[STR_RESULT] != null)
			{
				return obj[STR_RESULT];
			}
			if (obj[STR_ERROR] != null)
			{
				throw new Exception(obj[STR_ERROR].ToString());
			}
			throw new Exception(STR_FORMAT);
		}
        #endregion

        #region Protected methods

        protected void call_api_sub(string strMethod, ArrayList arrParams)
        {
            SendRequest(strMethod, arrParams);
        }
        protected JObject call_api(string strMethod)
        {
			return (JObject)ProcessResult(JsonConvert.DeserializeObject<JObject>(SendRequest(strMethod)));
        }
        protected JObject call_api(string strMethod, ArrayList arrParams)
        {
            return (JObject)ProcessResult(JsonConvert.DeserializeObject<JObject>(SendRequest(strMethod, arrParams)));
        }

        #endregion

		public void SetUrl(string strURL)
		{
            m_strURL = strURL;
        }
    }
}
