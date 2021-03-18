using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PointingPoker.Data
{
    public class PointingSessionManager
    {
        private readonly ConcurrentDictionary<string, PointingSession> _sessions;

        public PointingSessionManager()
        {
            _sessions = new ConcurrentDictionary<string, PointingSession>();
        }

        public IEnumerable<PointingSession> FindByUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new PointingSession[0];
            }

            return _sessions
                .Where(s => s.Value.FindParticipant(userId) != null)
                .Select(s => s.Value)
                .ToArray();                
        }

        public PointingSession Create()
        {
            var session = new PointingSession();
            while (!_sessions.TryAdd(session.Id, session))
            {
                session = new PointingSession();
            }

            return session;
        }

        public PointingSession Get(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var result))
            {
                result = null;
            }

            return result;
        }

        public IEnumerable<PointingSession> Get()
        {
            return _sessions
                .Select(s => s.Value)
                .ToArray();
        }
    }
}
