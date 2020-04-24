using System;

namespace PointingPoker.Data
{
    public class Participant
    {
        public Participant(string userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public string UserId { get; } = string.Empty;

        public string Name { get; } = string.Empty;
    }
}
