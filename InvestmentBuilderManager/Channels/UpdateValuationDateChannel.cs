using System;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    internal class UpdateValuationDateDto : Dto
    {
        public DateTime ValautionDate { get; set; }
    }

    /// <summary>
    /// handler class for updating valuation date command
    /// </summary>
    class UpdateValuationDateChannel : EndpointChannel<UpdateValuationDateDto, ChannelUpdater>
    {
        public UpdateValuationDateChannel() 
            : base("UPDATE_VALUTION_DATE_REQUEST", "UPDATE_VALUATION_DATE_RESPONSE")
        {
        }

        protected override Dto HandleEndpointRequest(UserSession userSession, UpdateValuationDateDto payload, ChannelUpdater update)
        {
            userSession.ValuationDate = payload.ValautionDate;
            return new ResponseDto { Status = true };
        }
    }
}
