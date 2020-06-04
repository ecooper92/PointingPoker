using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PointingPoker.Data
{
    public class PointingSession
    {
        private readonly VotingTopicStore _topicStore;
        private ModelStore<PointingOption> _optionStore;
        private ModelStore<Participant> _participantStore;

        public event Action OnUpdate;

        private static int _counter;
        private string _activeVotingTopicId;

        public PointingSession() : this(Interlocked.Increment(ref _counter).ToString()) { }//Guid.NewGuid().ToString()) { }

        public PointingSession(string id)
        {
            Id = id;
            _topicStore = new VotingTopicStore();
            _optionStore = new ModelStore<PointingOption>();
            _participantStore = new ModelStore<Participant>();

            _topicStore.OnAdd += t => OnUpdate?.SafeInvoke();
            _topicStore.OnUpdate += t => OnUpdate?.SafeInvoke();
            _topicStore.OnDelete += t => OnUpdate?.SafeInvoke();
            _optionStore.OnAdd += t => OnUpdate?.SafeInvoke();
            _optionStore.OnUpdate += t => OnUpdate?.SafeInvoke();
            _optionStore.OnDelete += t => OnUpdate?.SafeInvoke();
            _participantStore.OnAdd += t => OnUpdate?.SafeInvoke();
            _participantStore.OnUpdate += t => OnUpdate?.SafeInvoke();
            _participantStore.OnDelete += t => OnUpdate?.SafeInvoke();

            // Pre-poplate topics
            _topicStore.Add(new Topic("Default Story 1", "This is some discussion about the topic 1"));
            _topicStore.Add(new Topic("Default Story 2", "This is some discussion about the topic 2"));
            _topicStore.Add(new Topic("Default Story 3", "This is some discussion about the topic 3"));
            _topicStore.Add(new Topic("Default Story 4", "This is some discussion about the topic 4"));
            _topicStore.Add(new Topic("Default Story 5", "This is some discussion about the topic 5"));
            _topicStore.Add(new Topic("Default Story 6", "This is some discussion about the topic 6"));
            _topicStore.Add(new Topic("Default Story 7", "This is some discussion about the topic 7"));
            _topicStore.Add(new Topic("Default Story 8", "This is some discussion about the topic 8"));
            _topicStore.Add(new Topic("Default Story 9", "This is some discussion about the topic 9"));
            _topicStore.Add(new Topic("Default Story 10", "This is some discussion about the topic 10"));
            _topicStore.Add(new Topic("Default Story 11", "This is some discussion about the topic 11"));

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

        public IEnumerable<VotingTopic> Topics => _topicStore;

        public IEnumerable<PointingOption> Options => _optionStore;

        public IEnumerable<Participant> Participants => _participantStore;

        /// <summary>
        /// The topic currently set for voting.
        /// </summary>
        public string ActiveVotingTopicId
        {
            get => _activeVotingTopicId;
            set
            {
                if (_activeVotingTopicId != value)
                {
                    _activeVotingTopicId = value;
                    OnUpdate?.SafeInvoke();
                }
            }
        }

        public VotingTopic FindTopic(string topicId) => _topicStore.Find(topicId);

        public bool AddTopic(Topic topic) => _topicStore.TryAdd(topic);

        public bool Vote(string userId, string topicId, string optionId) => _topicStore.Vote(userId, topicId, optionId);

        public Vote GetVote(string userId, string topicId) => _topicStore.GetVote(userId, topicId);

        public void ClearVoting(string topicId) => _topicStore.ClearVoting(topicId);

        public void CompleteVoting(string topicId) => _topicStore.CompleteVoting(topicId);

        public Participant FindParticipant(string userId) => _participantStore.Find(userId);

        public void AddParticipant(Participant participant) => _participantStore.Add(participant);

        public void RemoveParticipant(string userId)
        {
            if (_participantStore.Remove(userId))
            {
                _topicStore.RemoveVotesByUser(userId);
            }
        }

        public void AddOption(PointingOption option) => _optionStore.Add(option);

        public void UpdateOption(PointingOption option) => _optionStore.Update(option);

        public void RemoveOption(string optionId)
        {
            if (_optionStore.Remove(optionId))
            {
                _topicStore.RemoveVotesByOption(optionId);
            }
        }
    }
}