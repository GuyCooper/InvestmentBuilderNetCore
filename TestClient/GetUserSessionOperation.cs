using System;
using System.Collections.Generic;
using System.Text;
using Transports;
using Transports.Session;

namespace TestClient
{
    class UserSessionRequestDto : Dto
    {
        public string SessionId { get; set; }
    }

    class UserSessionResponseDto : Dto
    {
        public string SessionID { get; set; }
        public bool Success { get; set; }
        public UserSession Session { get; set; }
    }

    internal sealed class GetUserSessionOperation : Operation<UserSessionRequestDto, UserSessionResponseDto>
    {
        public GetUserSessionOperation(IConnectionSession session, SessionObserver sessionObserver, string expectedAccountName) : base(session, "UserSession request", "GET_USER_SESSION_REQUEST", "GET_USER_SESSION_RESPONSE")
        {
            m_sessionObserver = sessionObserver;
            m_sessionObserver.SessionUpdatedEvent += onSessionIdUpdated;
            m_expectedAccountName = expectedAccountName;
        }

        private void onSessionIdUpdated(string sessionId)
        {
            m_sessionId = sessionId;
        }

        protected override UserSessionRequestDto GetRequest()
        {
            return new UserSessionRequestDto
            {
                SessionId = m_sessionId
            };
        }

        protected override bool HandleResponse(UserSessionResponseDto response)
        {
            var result = response.Success;
            if (!result)
            {
                Console.WriteLine($"GetUserSession request failed: {response.Error}");
            }

            return response.Session.AccountName.Name == m_expectedAccountName;

        }

        private readonly SessionObserver m_sessionObserver;
        private readonly string m_expectedAccountName;
        private string m_sessionId;
    }
}
