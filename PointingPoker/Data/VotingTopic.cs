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

        public VotingTopic(VotingTopic copy, IEnumerable<Vote> votes)
            : this(copy.Topic, copy.IsShowing, copy.State, votes) { }

        public Topic Topic { get; }

        public bool IsShowing { get; } = false;

        public VotingTopicState State { get; }

        public IEnumerable<Vote> Votes { get; }
    }
}
