using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PointingPoker.Data
{
    public class VotingTopic
    {
        public VotingTopic(Topic topic, bool isShowing, VotingTopicState state, IEnumerable<Vote> votes)
        {
            Topic = topic;
            IsShowing = isShowing;
            State = state;
            Votes = votes == null ? new Vote[0] : votes.ToArray();
        }

        /// <summary>
        /// Constructs a new voting topic from a topic with all defaults
        /// </summary>
        public VotingTopic(Topic topic)
            : this(topic, false, VotingTopicState.Upcoming, null) { }

        /// <summary>
        /// Copies a voting topic with a different underlying topic.
        /// </summary>
        public VotingTopic(VotingTopic copy, Topic topic)
            : this(topic, copy.IsShowing, copy.State, copy.Votes) { }

        /// <summary>
        /// Copies a voting topic with different votes.
        /// </summary>
        public VotingTopic(VotingTopic copy, IEnumerable<Vote> votes)
            : this(copy.Topic, copy.IsShowing, copy.State, votes) { }

        public Topic Topic { get; }

        public bool IsShowing { get; } = false;

        public VotingTopicState State { get; }

        public IEnumerable<Vote> Votes { get; }
    }
}
