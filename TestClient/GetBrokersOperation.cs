using System;
using System.Collections.Generic;
using System.Text;
using Transports;
using Transports.Session;

namespace TestClient
{
    internal class BrokerManagerResponseDto : Dto
    {
        public IList<string> Brokers { get; set; }
    }

    internal sealed class GetBrokersOperation : Operation<Dto, BrokerManagerResponseDto>
    {
        public GetBrokersOperation(IConnectionSession session) : base(session, "GetBrokers request", "GET_BROKERS_REQUEST", "GET_BROKERS_RESPONSE")
        {

        }

        protected override Dto GetRequest()
        {
            return new Dto();
        }

        protected override bool HandleResponse(BrokerManagerResponseDto response)
        {
            return response.Brokers.Count == 3;
        }
    }
}
