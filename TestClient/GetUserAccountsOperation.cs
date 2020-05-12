using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transports;
using Transports.Session;

namespace TestClient
{
    public class AccountIdentifier
    {
        public string Name { get; set; }
        public int AccountId { get; set; }

    }

    internal class AccountNamesDto : Dto
    {
        public IEnumerable<AccountIdentifier> AccountNames { get; set; }
    }

    class GetUserAccountsOperation : Operation<Dto, AccountNamesDto>
    {
        public GetUserAccountsOperation(IConnectionSession session) : base(session, "GetUser Accounts request", "GET_ACCOUNT_NAMES_REQUEST", "GET_ACCOUNT_NAMES_RESPONSE")
        {
        }

        protected override Dto GetRequest()
        {
            return new Dto();
        }

        protected override bool HandleResponse(AccountNamesDto response)
        {
            var accounts = response.AccountNames.ToList();
            return accounts.Count == 5;
        }
    }
}
