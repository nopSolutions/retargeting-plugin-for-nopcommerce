using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Nop.Services.Logging;

namespace Nop.Plugin.Widgets.Retargeting
{
    public class RetargetingRestApiHelper
    {
        public string GetJson(ILogger logger, string url, HttpMethod method, string data = null)
        {
            var result = string.Empty;

            try
            {
                if (method == HttpMethod.Get)
                    url = string.IsNullOrEmpty(data) ? url : string.Format("{0}?{1}", url, data);

                var httpWebRequest = WebRequest.Create(url);
                httpWebRequest.Method = method.ToString();
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";

                if ((method == HttpMethod.Post || method == HttpMethod.Put) && !string.IsNullOrEmpty(data))
                {
                    using var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream());
                    streamWriter.Write(data);
                    streamWriter.Close();
                }

                var httpWebResponse = httpWebRequest.GetResponse();

                using var responseStream = httpWebResponse.GetResponseStream();
                if (responseStream != null)
                {
                    var streamReader = new StreamReader(responseStream);
                    result = streamReader.ReadToEnd();
                    streamReader.Close();
                }
            }
            catch (WebException ex)
            {
                try
                {
                    using var responseStream = ex.Response.GetResponseStream();
                    if (responseStream != null)
                    {
                        var streamReader = new StreamReader(responseStream);
                        result = streamReader.ReadToEnd();
                        streamReader.Close();
                        logger.Error(string.Format("Retargeting REST API. Saving the order data error: {0}", result));
                    }
                }
                catch (Exception)
                {
                    //ignored
                }
            }
            catch (Exception ex)
            {
                var errorType = ex.GetType().ToString();
                var errorMessage = errorType + ": " + ex.Message;
                logger.Error(string.Format("Retargeting REST API. Saving the order data error: {0}", errorMessage));
            }

            return result;
        }
    }
}
