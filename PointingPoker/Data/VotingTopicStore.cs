using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class VotingTopicStore : ModelStore<VotingTopic>
    {
        public void Add(Topic topic) => Add(new VotingTopic(topic));

        public bool TryAdd(Topic topic) => TryAdd(new VotingTopic(topic));

        public void Update(Topic topic) => Update(topic.Id, existingTopic => new VotingTopic(existingTopic, topic));

        public void ClearVoting(string topicId) => Update(topicId, t => new VotingTopic(t.Topic, false, null));

        public void CompleteVoting(string topicId) => Update(topicId, t => new VotingTopic(t.Topic, true, t.Votes));

        public bool Vote(string userId, string topicId, string optionId)
        {
            if (string.IsNullOrEmpty(userId)
                || string.IsNullOrEmpty(topicId)
                || string.IsNullOrEmpty(optionId))
            {
                return false;
            }

            var vote = new Vote(userId, topicId, optionId);
            UpdateVotes(topicId, votes => votes.RemoveAllFluent(v => v.UserId == userId && v.TopicId == topicId).AddFluent(vote));

            return false;
        }

        public Vote GetVote(string userId, string topicId)
        {
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(topicId) && TryGet(topicId, out var topic))
            {
                return topic.Votes.FirstOrDefault(v => v.UserId == userId);
            }

            return null;
        }

        public void RemoveVotesByUser(string userId)
        {
            foreach (var votingTopic in this)
            {
                UpdateVotes(votingTopic.Id, votes => votes.RemoveAllFluent(v => v.UserId == userId));
            }
        }

        public void RemoveVotesByOption(string optionId)
        {
            foreach (var votingTopic in this)
            {
                UpdateVotes(votingTopic.Id, votes => votes.RemoveAllFluent(v => v.OptionId == optionId));
            }
        }

        private bool UpdateVotes(string topicId, Func<List<Vote>, IEnumerable<Vote>> updateAction)
        {
            return Update(topicId, t => new VotingTopic(t, updateAction(t.Votes.ToList())));
        }
    }
}
