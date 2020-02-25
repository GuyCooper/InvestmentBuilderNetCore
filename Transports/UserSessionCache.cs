using System;
using System.Collections.Generic;
using System.Text;
using Transports.Session;

namespace Transports
{
    /// <summary>
    /// This class maintains a lazy cache of user sessions. Each worker will have a single instance
    /// of this cache. When a request is received it will obtain the usersession details from this cache
    /// If the usersesson details are not available for the specified session id, the usersession will be 
    /// authenticated on from the authentication server which will return a valid userssion if it is valid
    /// user
    /// </summary>
    internal class UserSessionCache
    {
        public UserSessionCache(IConnectionSession authenticationSession)
        {

        }
    }
}
