using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class PointingSession
    {
        const int MAX_TOPICS = 200;
        const int MAX_OPTIONS = 100;
        const int MAX_PARTICIPANTS = 200;

        //private ConcurrentDictionary<string, Vote> _votes;
        private ConcurrentDictionary<string, Participant> _participants;
        private ConcurrentDictionary<string, CountingItem<VotingTopic>> _topics;
        private ConcurrentDictionary<string, CountingItem<PointingOption>> _options;

        public event Action OnVotesChanged;
        public event Action OnTopicsChanged;
        public event Action OnOptionsChanged;
        public event Action OnParticipantsChanged;

        private static int _counter;

        public PointingSession() : this(Interlocked.Increment(ref _counter).ToString()) { }//Guid.NewGuid().ToString()) { }

        public PointingSession(string id)
        {
            Id = id;
            //_votes = new ConcurrentDictionary<string, Vote>();
            _participants = new ConcurrentDictionary<string, Participant>();
            _topics = new ConcurrentDictionary<string, CountingItem<VotingTopic>>();
            _options = new ConcurrentDictionary<string, CountingItem<PointingOption>>();

            // Pre-poplate topics
            AddTopic(new Topic("Default Story 1", "This is some discussion about the topic 1"));
            AddTopic(new Topic("Default Story 2", "This is some discussion about the topic 2"));
            AddTopic(new Topic("Default Story 3", "This is some discussion about the topic 3"));
            AddTopic(new Topic("Default Story 4", "This is some discussion about the topic 4"));
            AddTopic(new Topic("Default Story 5", "This is some discussion about the topic 5"));
            AddTopic(new Topic("Default Story 6", "This is some discussion about the topic 6"));
            AddTopic(new Topic("Default Story 7", "This is some discussion about the topic 7"));
            AddTopic(new Topic("Default Story 8", "This is some discussion about the topic 8"));
            AddTopic(new Topic("Default Story 9", "This is some discussion about the topic 9"));
            AddTopic(new Topic("Default Story 10", "This is some discussion about the topic 10"));
            AddTopic(new Topic("Default Story 11", "This is some discussion about the topic 11"));

            // Pre-populate options
            AddOption(new PointingOption("0 points", "0"));
            AddOption(new PointingOption("1 point", "1"));
            AddOption(new PointingOption("2 points", "2"));
            AddOption(new PointingOption("3 points", "3"));
            AddOption(new PointingOption("5 points", "5"));
            AddOption(new PointingOption("8 points", "8"));
            AddOption(new PointingOption("13 points", "13"));
            AddOption(new PointingOption("20 points", "20"));
            AddOption(new PointingOption("40 points", "40"));
            AddOption(new PointingOption("100 points", "100"));
        }

        public string Id { get; } = string.Empty;

        public IEnumerable<VotingTopic> Topics => _topics.OrderBy(o => o.Value.Count).Select(o => o.Value.Item).ToArray();

        public IEnumerable<PointingOption> Options => _options.OrderBy(o => o.Value.Count).Select(o => o.Value.Item).ToArray();

        public IEnumerable<Participant> Participants => _participants.Select(o => o.Value).ToArray();

        public Vote GetVoteByUserAndTopic(string userId, string topicId)
        {
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(topicId) && _topics.TryGetValue(topicId, out var topic))
            {
                return topic.Item.Votes.FirstOrDefault(v => v.UserId == userId);
            }

            return null;
        }

        public bool Vote(string userId, string topicId, string optionId)
        {
            if (string.IsNullOrEmpty(userId)
                || string.IsNullOrEmpty(topicId)
                || string.IsNullOrEmpty(optionId))
            {
                return false;
            }

            var vote = new Vote(userId, topicId, optionId);
            UpdateTopicVotes(topicId, votes => votes.RemoveAllFluent(v => v.UserId == userId && v.TopicId == topicId).AddFluent(vote));

            return false;
        }

        public void RestartVoting(string topicId)
        {
            if (_topics.TryUpdate(topicId, v => new VotingTopic(v.Topic, VotingTopicState.Current, DateTime.Now, DateTime.MinValue, null)))
            {
                OnTopicsChanged?.SafeInvoke();
                OnVotesChanged?.SafeInvoke();
            }
        }

        public void CompleteVoting(string topicId)
        {
            if (_topics.TryUpdate(topicId, v => new VotingTopic(v.Topic, VotingTopicState.Voted, v.StartTime, DateTime.Now, v.Votes)))
            {
                OnTopicsChanged?.SafeInvoke();
                OnVotesChanged?.SafeInvoke();
            }
        }

        public Participant FindParticipant(string userId)
        {
            if (_participants.TryGetValue(userId, out var participant))
            {
                return participant;
            }

            return null;
        }

        public void AddParticipant(Participant participant)
        {
            if (_participants.Count > MAX_PARTICIPANTS)
            {
                throw new InvalidOperationException($"Only up to {MAX_PARTICIPANTS} participants are supported per session.");
            }

            _participants.AddOrUpdate(participant.UserId, participant, (key, oldValue) => participant);
            OnParticipantsChanged?.SafeInvoke();
        }

        public void RemoveParticipant(string userId)
        {
            if (_participants.TryRemove(userId, out var participant))
            {
                if (Topics.Any(votingTopic => UpdateTopicVotes(votingTopic.Topic.Id, votes => votes.RemoveAllFluent(v => v.UserId == userId), false)))
                {
                    OnTopicsChanged?.SafeInvoke();
                    OnVotesChanged?.SafeInvoke();
                }

                OnParticipantsChanged?.SafeInvoke();
            }
        }

        public VotingTopic FindTopic(string topicId)
        {
            if (!string.IsNullOrEmpty(topicId) && _topics.TryGetValue(topicId, out var topic))
            {
                return topic.Item;
            }

            return null;
        }

        public void AddTopic(Topic topic)
        {
            // Sanity check
            if (_topics.Count > MAX_TOPICS)
            {
                throw new InvalidOperationException($"Only up to {MAX_TOPICS} topics are supported per session.");
            }

            var votingTopic = new VotingTopic(topic);
            var item = new CountingItem<VotingTopic>(votingTopic);
            if (_topics.TryAdd(topic.Id, item))
            {
                OnTopicsChanged?.SafeInvoke();
            }
        }

        public void UpdateTopic(Topic topic)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            while (_topics.TryGetValue(topic.Id, out var item) && item.Item.Topic.IsModified(topic))
            {
                var votingTopic = new VotingTopic(item.Item, topic);
                var newItem = new CountingItem<VotingTopic>(item.Count, votingTopic);
                if (_topics.TryUpdate(topic.Id, newItem, item))
                {
                    OnTopicsChanged?.SafeInvoke();
                    return;
                }
            }
        }

        public void RemoveTopic(string id)
        {
            if (_topics.TryRemove(id, out var topic))
            {
                OnTopicsChanged?.SafeInvoke();
            }
        }

        public void AddOption(PointingOption option)
        {
            // Sanity check
            if (_options.Count > MAX_OPTIONS)
            {
                throw new InvalidOperationException($"Only up to {MAX_OPTIONS} options are supported per session.");
            }

            if (_options.TryAdd(option.Id, new CountingItem<PointingOption>(option)))
            {
                OnOptionsChanged?.SafeInvoke();
            }
        }

        public void UpdateOption(PointingOption option)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            while (_options.TryGetValue(option.Id, out var item) && item.Item.IsModified(option))
            {
                if (_options.TryUpdate(option.Id, new CountingItem<PointingOption>(item.Count, option), item))
                {
                    OnOptionsChanged?.SafeInvoke();
                    return;
                }
            }

        }

        public void RemoveOption(string optionId)
        {
            if (_options.TryRemove(optionId, out var option))
            {
                if (Topics.Any(votingTopic => UpdateTopicVotes(votingTopic.Topic.Id, votes => votes.RemoveAllFluent(v => v.OptionId == optionId), false)))
                {
                    OnVotesChanged?.SafeInvoke();
                    OnTopicsChanged?.SafeInvoke();
                }
                OnOptionsChanged?.SafeInvoke();
            }
        }

        private bool UpdateTopicVotes(string topicId, Func<List<Vote>, IEnumerable<Vote>> updateAction, bool fireUpdate = true)
        {
            if (_topics.TryUpdate(topicId, v => new VotingTopic(v, updateAction(v.Votes.ToList()))) && fireUpdate)
            {
                OnVotesChanged?.SafeInvoke();
                return true;
            }

            return false;
        }
    }
}