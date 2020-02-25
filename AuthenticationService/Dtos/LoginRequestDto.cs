using System;
using System.Collections.Generic;
using System.Text;
using Transports;

namespace AuthenticationService.Dtos
{
    /// <summary>
    /// Login request Dto
    /// </summary>
    public class LoginRequestDto : Dto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Version { get; set; }
        public string AppName { get; set; }
        public string Source { get; set; }
    }

}
