using System;
using Transports;
using Transports.Session;

namespace TestClient
{
    class LoginRequestDto : Dto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Version { get; set; }
        public string AppName { get; set; }
        public string Source { get; set; }
    }

    class LoginResponseDto : Dto
    {
        public bool Success { get; set; }
        public string SessionID { get; set; }
    }

    sealed class LoginOperation : Operation<LoginRequestDto, LoginResponseDto>
    {
        public LoginOperation(IConnectionSession session) : base(session, "Login User", "AUTHENTICATE_USER_REQUEST", "AUTHENTICATE_USER_RESPONSE")
        { }

        protected override LoginRequestDto GetRequest()
        {
            return new LoginRequestDto
            {
                UserName = "guy@guycooper.plus.com",
                Password = "N@omi13James10"
            };
        }

        protected override bool HandleResponse(LoginResponseDto response)
        {
            var result = response.Success;
            if(!result)
            {
                Console.WriteLine($"Login request failed: {response.Error}");
            }
            return result;
        }
    }
}
