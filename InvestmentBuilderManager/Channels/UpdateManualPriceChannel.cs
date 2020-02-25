using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// Investment price  update dto.
    /// </summary>
    internal class InvestmentPriceUpdateDto : Dto
    {
        public string Investment { get; set; }
        public double Price { get; set; }
    }

    /// <summary>
    /// Class handles request to manuallly update an investment price.
    /// </summary>
    internal class UpdateManualPriceChannel : EndpointChannel<InvestmentPriceUpdateDto, ChannelUpdater>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateManualPriceChannel() : 
            base("UPDATE_INVESTMENT_PRICE_REQUEST", "UPDATE_INVESTMENT_PRICE_RESPONSE")
        {
        }

        /// <summary>
        /// Handle update manual price request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, InvestmentPriceUpdateDto payload, ChannelUpdater update)
        {
            if(userSession.UserPrices.ContainsKey(payload.Investment) == false)
            {
                userSession.UserPrices.Add(payload.Investment, payload.Price);
            }
            else
            {
                userSession.UserPrices[payload.Investment] = payload.Price;
            }
            return new ResponseDto { Status = true };
        }
    }
}
