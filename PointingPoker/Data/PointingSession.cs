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

        public PointingSession() : this(Interlocked.Increment(ref _counter).ToString()/*Guid.NewGuid().ToString()*/) { }

        public PointingSession(string id)
        {
            Id = id;
            //_votes = new ConcurrentDictionary<string, Vote>();
            _participants = new ConcurrentDictionary<string, Participant>();
            _topics = new ConcurrentDictionary<string, CountingItem<VotingTopic>>();
            _options = new ConcurrentDictionary<string, CountingItem<PointingOption>>();

            AddParticipant(new Participant("1", "JoeBob1"));
            AddParticipant(new Participant("2", "JoeBob2"));
            AddParticipant(new Participant("3", "JoeBob3"));
            AddParticipant(new Participant("4", "JoeBob4"));
            AddParticipant(new Participant("5", "JoeBob5"));
            AddParticipant(new Participant("6", "JoeBob6"));
            AddParticipant(new Participant("7", "JoeBob7"));
            AddParticipant(new Participant("8", "JoeBob8"));
            AddParticipant(new Participant("9", "JoeBob9"));
            AddParticipant(new Participant("10", "JoeBob10"));
            AddParticipant(new Participant("11", "JoeBob11"));
            AddParticipant(new Participant("12", "JoeBob12"));
            AddParticipant(new Participant("13", "JoeBob13"));

            // Pre-poplate topics
            AddTopic(new Topic("Default Story 1", "some discussion"));
            AddTopic(new Topic("Default Story 2", "other discussion"));
            AddTopic(new Topic("Default Story 3", "other discussion"));
            AddTopic(new Topic("Default Story 4", "other discussion"));
            AddTopic(new Topic("Default Story 5", "other discussion"));

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
            if (_topics.TryGetValue(topicId, out var topic))
            {
                return topic.Item.Votes.FirstOrDefault(v => v.UserId == userId);
            }

            return null;
        }

        public void Vote(string userId, string topicId, string optionId)
        {
            var vote = new Vote(userId, topicId, optionId);

            while (_topics.TryGetValue(topicId, out var item))
            {
                var votes = item.Item.Votes.ToList();
                votes.RemoveAll(v => v.UserId == userId && v.TopicId == topicId);
                votes.Add(vote);

                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, item.Item.IsShowing, votes)), item))
                {
                    SafeRunAction(OnVotesChanged);
                    break;
                }
            }
        }

        public void ResetVotes(string topicId)
        {
            while (_topics.TryGetValue(topicId, out var item))
            {
                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, false, null)), item))
                {
                    SafeRunAction(OnVotesChanged);
                    break;
                }
            }
        }

        public void ShowVotes(string topicId)
        {
            while (_topics.TryGetValue(topicId, out var item))
            {
                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, true, item.Item.Votes)), item))
                {
                    SafeRunAction(OnVotesChanged);
                    break;
                }
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
            SafeRunAction(OnParticipantsChanged);
        }

        public void RemoveParticipant(string participantId)
        {
            if (_participants.TryRemove(participantId, out var participant))
            {
                SafeRunAction(OnParticipantsChanged);
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

            while (!_topics.TryAdd(topic.Id, new CountingItem<VotingTopic>(new VotingTopic(topic, false, null))))
            {
                topic = new Topic(topic.Name, topic.Discussion);
            }

            SafeRunAction(OnTopicsChanged);
        }

        public void UpdateTopic(Topic topic)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            while (_topics.TryGetValue(topic.Id, out var item) && item.Item.Topic.IsModified(topic))
            {
                if (_topics.TryUpdate(topic.Id, new CountingItem<VotingTopic>(item.Count, new VotingTopic(topic, item.Item.IsShowing, item.Item.Votes)), item))
                {
                    SafeRunAction(OnTopicsChanged);
                    return;
                }
            }
        }

        public void RemoveTopic(string id)
        {
            if (_topics.TryRemove(id, out var topic))
            {
                SafeRunAction(OnTopicsChanged);
            }
        }

        public void AddOption(PointingOption option)
        {
            // Sanity check
            if (_options.Count > MAX_OPTIONS)
            {
                throw new InvalidOperationException($"Only up to {MAX_OPTIONS} options are supported per session.");
            }

            while (!_options.TryAdd(option.Id, new CountingItem<PointingOption>(option)))
            {
                option = new PointingOption(option.Name, option.Value);
            }

            SafeRunAction(OnOptionsChanged);
        }

        public void UpdateOption(PointingOption option)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            while (_options.TryGetValue(option.Id, out var item) && item.Item.IsModified(option))
            {
                if (_options.TryUpdate(option.Id, new CountingItem<PointingOption>(item.Count, option), item))
                {
                    SafeRunAction(OnOptionsChanged);
                    return;
                }
            }

        }

        public void RemoveOption(string id)
        {
            if (_options.TryRemove(id, out var option))
            {
                SafeRunAction(OnOptionsChanged);
            }
        }

        private void SafeRunAction(Action action)
        {
            if (action != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch
                    {

                    }
                });
            }
        }
    }
}
