using System;
using System.Collections.Generic;
using System.Text;

namespace TestClient
{
    class SessionObserver
    {
        public event Action<string> SessionUpdatedEvent;

        public void UpdateSession(string sessionid)
        {
            SessionUpdatedEvent?.Invoke(sessionid);
        }
    }
}
