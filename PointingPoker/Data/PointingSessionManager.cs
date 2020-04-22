using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class PointingSessionManager
    {
        private ConcurrentDictionary<string, PointingSession> _sessions;

        public PointingSessionManager()
        {
            _sessions = new ConcurrentDictionary<string, PointingSession>();
        }

        public PointingSession Create()
        {
            var session = new PointingSession();
            session.Id = Guid.NewGuid().ToString();

            if (_sessions.TryAdd(session.Id, session))
            {
                return session;
            }

            return null;
        }

        public PointingSession Get(string sessionId)
        {
            PointingSession result = null;
            if (!_sessions.TryGetValue(sessionId, out result))
            {
                result = null;
            }

            return result;
        }
    }
}
