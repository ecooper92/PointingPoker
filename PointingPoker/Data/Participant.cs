using System;

namespace PointingPoker.Data
{
    public class Participant
    {
        public Participant(string userId, string name, ParticipantType type)
        {
            UserId = userId;
            Name = name;
            Type = type;
        }

        public string UserId { get; } = string.Empty;

        public string Name { get; } = string.Empty;

        public ParticipantType Type { get; } = ParticipantType.Observer;
    }
}
