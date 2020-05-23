using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PointingPoker.Data
{
    public class VotingTopic
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public VotingTopic(Topic topic, VotingTopicState state, DateTime startTime, DateTime endTime, IEnumerable<Vote> votes)
        {
            Topic = topic;
            State = state;
            StartTime = startTime;
            EndTime = endTime;
            Votes = votes == null ? new Vote[0] : votes.ToArray();
        }

        /// <summary>
        /// Constructs a new voting topic from a topic with all defaults
        /// </summary>
        public VotingTopic(Topic topic)
            : this(topic, VotingTopicState.Upcoming, DateTime.MinValue, DateTime.MinValue, null) { }

        /// <summary>
        /// Copies a voting topic with a different underlying topic.
        /// </summary>
        public VotingTopic(VotingTopic copy, Topic topic)
            : this(topic, copy.State, copy.StartTime, copy.EndTime, copy.Votes) { }

        /// <summary>
        /// Copies a voting topic with different times.
        /// </summary>
        public VotingTopic(VotingTopic copy, DateTime startTime, DateTime endTime)
            : this(copy.Topic, copy.State, startTime, endTime, copy.Votes) { }

        /// <summary>
        /// Copies a voting topic with different votes.
        /// </summary>
        public VotingTopic(VotingTopic copy, IEnumerable<Vote> votes)
            : this(copy.Topic, copy.State, copy.StartTime, copy.EndTime, votes) { }

        public Topic Topic { get; }

        public VotingTopicState State { get; }

        public IEnumerable<Vote> Votes { get; }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }
    }
}
