using InvestmentBuilderCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Transports;
using Transports.Utils;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// Defines the recent report request Dto.
    /// </summary>
    internal class RecentReportRequestDto : Dto
    {
        public string DateFrom { get; set; }
    }

    /// <summary>
    /// RecentReportFile class. Contains File information about a report
    /// </summary>
    internal class RecentReportFile
    {
        public string ValuationDate { get; set; }
        public string Link { get; set; }
    }

    /// <summary>
    /// Dto returned from channel handler. Contains a list of RecentReportFile objects.
    /// </summary>
    internal class RecentReportListDto : Dto
    {
        public List<RecentReportFile> RecentReports { get; set; } 
    }

    /// <summary>
    /// Channel Handler for returning a list of recent report file locations
    /// </summary>
    internal class GetRecentReportFiles : AuthorisationEndpointChannel<RecentReportRequestDto, ChannelUpdater>
    {
        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public GetRecentReportFiles(ServiceAggregator aggregator)
            : base("GET_RECENT_REPORTS_REQUEST", "GET_RECENT_REPORTS_RESPONSE", aggregator.AccountService)
        {
            m_clientData = aggregator.DataLayer.ClientData;
            m_connectionSettings = aggregator.ConnectionSettings;
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Handle request for GetRecentReportFiles.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, RecentReportRequestDto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);
            var dtFrom = payload.DateFrom != null ?  DateTime.Parse(payload.DateFrom) : DateTime.Today;
            return new RecentReportListDto
            {
                RecentReports = m_clientData.GetRecentValuationDates(userToken, dtFrom).Select(x =>
                    new RecentReportFile
                    {
                        Link = CreateReportLink(m_connectionSettings, userToken.Account, x),
                        ValuationDate = x.ToShortDateString()
                    }).ToList()
            };
        }

        #endregion

        #region Private Data Members

        private readonly IClientDataInterface m_clientData;
        private readonly IConnectionSettings m_connectionSettings;

        #endregion
    }
}
