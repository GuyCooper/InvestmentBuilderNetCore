using System;
using InvestmentBuilderCore;

namespace Transports
{
    public class UserSession
    {
        public string UserName { get; private set; }
        public string SessionId { get; private set; }
        public DateTime ValuationDate { get; set; }
        public AccountIdentifier AccountName { get; set; }
        public ManualPrices UserPrices { get; private set; }

        public UserSession(string username, string usersessionid)
        {
            UserName = username;
            SessionId = usersessionid;
            ValuationDate = DateTime.Now;
            UserPrices = new ManualPrices();
        }             
    }

    public class UserSessionRequestDto : Dto
    {
        public string SessionId { get; set; }
    }

    public class UserSessionResponseDto : Dto
    {
        public bool Success { get; set; }
        public UserSession Session { get; set; }
    }
}

