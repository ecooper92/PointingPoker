using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PointingPoker.Data
{
    public class VotingTopic
    {
        public VotingTopic(Topic topic, bool isShowing, IEnumerable<Vote> votes)
        {
            Topic = topic;
            IsShowing = isShowing;
            Votes = votes == null ? new Vote[0] : votes.ToArray();
        }

        public Topic Topic { get; }

        public bool IsShowing { get; } = false;

        public IEnumerable<Vote> Votes { get; }
        /// <summary>
        /// Returns true if the option has the same id, but modified fields.
        /// </summary>
        //public bool IsModified(VotingTopic other)
        //{
        //    return this.Id == other.Id
        //        && (this.Name != other.Name || this.Discussion != other.Discussion);
        //}
    }
}
