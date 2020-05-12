using System;
using System.Collections.Generic;
using System.Text;
using Transports;
using Transports.Session;

namespace TestClient
{
    internal class UpdateCurrentAccountRequestDto : Dto
    {
        public int AccountId { get; set; }
    }

    public class ResponseDto : Dto
    {
        public bool Status { get; set; }
    }

    internal sealed class ChangeAccountOperation : Operation<UpdateCurrentAccountRequestDto, ResponseDto>
    {
        public ChangeAccountOperation(IConnectionSession session) : base(session, "change account request", "UPDATE_CURRENT_ACCOUNT_REQUEST", "UPDATE_CURRENT_ACCOUNT_RESPONSE")
        { }

        protected override UpdateCurrentAccountRequestDto GetRequest()
        {
            return new UpdateCurrentAccountRequestDto
            {
                AccountId = 2
            };
        }

        protected override bool HandleResponse(ResponseDto response)
        {
            return response.Status;
        }
    }
}
