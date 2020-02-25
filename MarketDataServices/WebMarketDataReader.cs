using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using NLog;

namespace MarketDataServices
{

    internal class WebMarketDataReader : IMarketDataReader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<string> GetData(string url, SourceDataFormat format)
        {
            HttpWebRequest request = null;
            var result = new List<string>();
            request = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
            request.Timeout = 30000;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (StreamReader input = new StreamReader(
                    response.GetResponseStream()))
                {
                    if (format == SourceDataFormat.CSV)
                    {
                        while (input.EndOfStream == false)
                        {
                            result.Add(input.ReadLine());
                        }
                    }
                    else
                    {
                        result.Add(input.ReadToEnd());
                    }
                }
            }
            catch(WebException e)
            {
                logger.Log(LogLevel.Error, "error caling url: {0}. {1}",url, e.Message);
            }
            return result;
        }
    }
}
