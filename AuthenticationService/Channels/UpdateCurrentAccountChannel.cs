using InvestmentBuilderCore;
using Transports;

namespace AuthenticationService.Channels
{
    /// <summary>
    /// UpdateCurrentAccount request dto.
    /// </summary>
    internal class UpdateCurrentAccountRequestDto : Dto
    {
        public AccountIdentifier AccountName { get; set; }
    }

    /// <summary>
    /// Class handles Updatecurrentaccount request.
    /// </summary>
    class UpdateCurrentAccountChannel : EndpointChannel<UpdateCurrentAccountRequestDto, ChannelUpdater>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public UpdateCurrentAccountChannel() :
            base("UPDATE_CURRENT_ACCOUNT_REQUEST", "UPDATE_CURRENT_ACCOUNT_RESPONSE")
        {
        }

        /// <summary>
        /// Handle update current account request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, UpdateCurrentAccountRequestDto payload, ChannelUpdater update)
        {
            userSession.AccountName = payload.AccountName;
            return new ResponseDto { Status = true };
        }
    }
}
