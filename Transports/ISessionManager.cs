using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Transports
{
    /// <summary>
    /// Defines a class that retrieves a users sessions details
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Returns the session details for a user.
        /// </summary>
        Task<UserSession> GetUserSession(string sessionId);
        /// <summary>
        ///  removes a users session details.
        /// </summary>
        void RemoveUserSession(string sessionId);
    }

}
