using System;

namespace PointingPoker.Data
{
    public class Vote
    {
        public Vote(string userId, string topicId, string optionId)
        {
            UserId = userId;
            TopicId = topicId;
            OptionId = optionId;
        }

        public string UserId { get; } = string.Empty;

        public string TopicId { get; } = string.Empty;

        public string OptionId { get; } = string.Empty;
    }
}
