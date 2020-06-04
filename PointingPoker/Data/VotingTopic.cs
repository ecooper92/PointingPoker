using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PointingPoker.Data
{
    public class VotingTopic : IModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VotingTopic(Topic topic, bool isComplete, IEnumerable<Vote> votes)
        {
            Topic = topic;
            IsComplete = isComplete;
            Votes = votes == null ? new Vote[0] : votes.ToArray();
        }

        /// <summary>
        /// Constructs a new voting topic from a topic with all defaults
        /// </summary>
        public VotingTopic(Topic topic)
            : this(topic, false, null) { }

        /// <summary>
        /// Copies a voting topic with a different underlying topic.
        /// </summary>
        public VotingTopic(VotingTopic copy, Topic topic)
            : this(topic, copy.IsComplete, copy.Votes) { }

        /// <summary>
        /// Copies a voting topic with different votes.
        /// </summary>
        public VotingTopic(VotingTopic copy, IEnumerable<Vote> votes)
            : this(copy.Topic, copy.IsComplete, votes) { }

        public string Id => Topic.Id;

        public Topic Topic { get; }

        public bool IsComplete { get; }

        public IEnumerable<Vote> Votes { get; }
    }
}
