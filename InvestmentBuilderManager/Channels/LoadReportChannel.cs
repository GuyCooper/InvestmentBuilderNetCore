using InvestmentBuilderCore;
using InvestmentBuilderLib;
using System;
using System.IO;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// LoadReport request dto.
    /// </summary>
    internal class LoadReportRequestDto : Dto
    {
        public string ValuationDate { get; set; }
    }

    /// <summary>
    /// handler class for loading valuation report. returns a location url to the report file
    /// </summary>
    internal class LoadReportChannel : AuthorisationEndpointChannel<LoadReportRequestDto, ChannelUpdater>
    {
        public LoadReportChannel(ServiceAggregator aggregator) :
            base("LOAD_REPORT_REQUEST", "LOAD_REPORT_RESPONSE", aggregator.AccountService)
        {
            _builder = aggregator.Builder;
            _settings = aggregator.Settings;
        }

        protected override Dto HandleEndpointRequest(UserSession userSession, LoadReportRequestDto payload, ChannelUpdater update)
        {
            var token = GetCurrentUserToken(userSession);
            var dtValuation = payload.ValuationDate != null ? DateTime.Parse(payload.ValuationDate) : userSession.ValuationDate;
            var reportFile = Path.Combine(_settings.OutputFolder, token.Account.GetPathName(),  _builder.GetInvestmentReport(token, dtValuation));

            return new BinaryDto
            {
                Payload = readBytesFromFile(reportFile)
            };
        }

        /// <summary>
        /// Method loads the report file and encodes it into a byte array using an ASCII encoding.
        /// </summary>
        private byte[] readBytesFromFile(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                using (var reader = new StreamReader(stream,true))
                {
                    using (var outStream = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(outStream, reader.CurrentEncoding))
                        {
                            int charsRead;
                            char[] buffer = new char[128 * 1024];
                            while ((charsRead = reader.ReadBlock(buffer, 0, buffer.Length)) > 0)
                            {
                                writer.Write(buffer, 0, charsRead);
                            }
                        }
                        return outStream.ToArray();
                    }
                }
            }
        }

        #region Private Data

        private readonly InvestmentBuilder _builder;

        private readonly IConfigurationSettings _settings;

        #endregion

    }
}
