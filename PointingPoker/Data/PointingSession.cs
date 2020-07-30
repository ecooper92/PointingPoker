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
        private ConcurrentDictionary<string, ICollection<PointingOption>> _presetOptions;

        public event Action OnVotesChanged;
        public event Action OnTopicsChanged;
        public event Action OnOptionsChanged;
        public event Action OnParticipantsChanged;

        private static int _counter;

        public PointingSession() : this(Guid.NewGuid().ToString()) { }

        public PointingSession(string id)
        {
            Id = id;
            //_votes = new ConcurrentDictionary<string, Vote>();
            _participants = new ConcurrentDictionary<string, Participant>();
            _topics = new ConcurrentDictionary<string, CountingItem<VotingTopic>>();
            _options = new ConcurrentDictionary<string, CountingItem<PointingOption>>();

            // Pre-poplate topics
            AddTopic(new Topic("Default Story 1", "some discussion"));

            // Pre-populate options
            var storyOptions = new List<PointingOption>()
            {
                new PointingOption("0 points", "0"),
                new PointingOption("1 point", "1"),
                new PointingOption("2 points", "2"),
                new PointingOption("3 points", "3"),
                new PointingOption("5 points", "5"),
                new PointingOption("8 points", "8"),
                new PointingOption("13 points", "13"),
                new PointingOption("20 points", "20"),
                new PointingOption("40 points", "40"),
                new PointingOption("100 points", "100"),
                new PointingOption("?", "?")
            };

            var taskOptions = new List<PointingOption>()
            {
                new PointingOption("0.5 days", "0.5"),
                new PointingOption("1 day", "1"),
                new PointingOption("1.5 days", "1.5"),
                new PointingOption("2 days", "2"),
                new PointingOption("2.5 days", "2.5"),
                new PointingOption("3 days", "3"),
                new PointingOption("3.5 days", "3.5"),
                new PointingOption("4 days", "4"),
                new PointingOption("4.5 days", "4.5"),
                new PointingOption("5 days", "5"),
                new PointingOption("?", "?")
            };

            _presetOptions = new ConcurrentDictionary<string, ICollection<PointingOption>>();
            _presetOptions.TryAdd("Story", storyOptions);
            _presetOptions.TryAdd("Task", taskOptions);
            SetOptionPreset("Story");
        }

        public string Id { get; } = string.Empty;

        public IEnumerable<string> OptionPresets => _presetOptions.Select(p => p.Key).OrderBy(p => p).ToArray();

        public IEnumerable<VotingTopic> Topics => _topics.OrderBy(o => o.Value.Count).Select(o => o.Value.Item).ToArray();

        public IEnumerable<PointingOption> Options => _options.OrderBy(o => o.Value.Count).Select(o => o.Value.Item).ToArray();

        public IEnumerable<Participant> Participants => _participants.Select(o => o.Value).ToArray();

        public string SelectedOptionsPreset { get; private set; }

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

            while (_topics.TryGetValue(topicId, out var item))
            {
                var votes = item.Item.Votes.ToList();
                votes.RemoveAll(v => v.UserId == userId && v.TopicId == topicId);
                votes.Add(vote);

                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, item.Item.IsShowing, votes)), item))
                {
                    SafeRunAction(OnVotesChanged);
                    return true;
                }
            }

            return false;
        }

        public void ResetVotes(string topicId)
        {
            while (_topics.TryGetValue(topicId, out var item))
            {
                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, false, null)), item))
                {
                    SafeRunAction(OnTopicsChanged);
                    SafeRunAction(OnVotesChanged);
                    break;
                }
            }
        }

        public void SetVotesShowing(string topicId, bool isShowing)
        {
            while (_topics.TryGetValue(topicId, out var item))
            {
                if (_topics.TryUpdate(topicId, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, isShowing, item.Item.Votes)), item))
                {
                    SafeRunAction(OnTopicsChanged);
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

        public void RemoveParticipant(string userId)
        {
            if (_participants.TryRemove(userId, out var participant))
            {
                var voteRemoved = false;
                foreach (var topic in Topics)
                {
                    while (_topics.TryGetValue(topic.Topic.Id, out var item))
                    {
                        var votes = item.Item.Votes.ToList();
                        votes.RemoveAll(v => v.UserId == userId);

                        if (_topics.TryUpdate(topic.Topic.Id, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, item.Item.IsShowing, votes)), item))
                        {
                            voteRemoved = true;
                            break;
                        }
                    }
                }

                if (voteRemoved)
                {
                    SafeRunAction(OnTopicsChanged);
                    SafeRunAction(OnVotesChanged);
                }

                SafeRunAction(OnParticipantsChanged);
            }
        }

        public bool IsShowingTopic(string topicId)
        {
            var topic = FindTopic(topicId);
            return topic != null && topic.IsShowing;
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

        public void SetOptionPreset(string preset)
        {
            // Return if this already is the preset.
            if (SelectedOptionsPreset == preset)
            {
                return;
            }

            if (_presetOptions.TryGetValue(preset, out var options))
            {
                var voteRemoved = false;
                foreach (var optionToRemove in _options)
                {
                    if (_options.TryRemove(optionToRemove.Key, out var option))
                    {
                        foreach (var topic in Topics)
                        {
                            while (_topics.TryGetValue(topic.Topic.Id, out var item))
                            {
                                var votes = item.Item.Votes.ToList();
                                votes.RemoveAll(v => v.OptionId == option.Item.Id);

                                if (_topics.TryUpdate(topic.Topic.Id, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, item.Item.IsShowing, votes)), item))
                                {
                                    voteRemoved = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                foreach (var option in options)
                {
                    _options.TryAdd(option.Id, new CountingItem<PointingOption>(option));
                }

                SelectedOptionsPreset = preset;
                if (voteRemoved)
                {
                    SafeRunAction(OnTopicsChanged);
                    SafeRunAction(OnVotesChanged);
                }
                SafeRunAction(OnOptionsChanged);
            }
        }

        public void AddOption(string name, string value)
        {
            // Sanity check
            if (_options.Count > MAX_OPTIONS)
            {
                throw new InvalidOperationException($"Only up to {MAX_OPTIONS} options are supported per session.");
            }

            var option = new PointingOption(name, value);
            while (!_options.TryAdd(option.Id, new CountingItem<PointingOption>(option)))
            {
                option = new PointingOption(option.Name, option.Value);
            }

            SelectedOptionsPreset = null;
            SafeRunAction(OnOptionsChanged);
        }

        public void UpdateOption(string id, string name, string value)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            var option = new PointingOption(id, name, value);
            while (_options.TryGetValue(option.Id, out var item) && item.Item.IsModified(option))
            {
                if (_options.TryUpdate(option.Id, new CountingItem<PointingOption>(item.Count, option), item))
                {
                    SelectedOptionsPreset = null;
                    SafeRunAction(OnOptionsChanged);
                    return;
                }
            }

        }

        public void RemoveOption(string optionId)
        {
            if (_options.TryRemove(optionId, out var option))
            {
                var voteRemoved = false;
                foreach (var topic in Topics)
                {
                    while (_topics.TryGetValue(topic.Topic.Id, out var item))
                    {
                        var votes = item.Item.Votes.ToList();
                        votes.RemoveAll(v => v.OptionId == optionId);

                        if (_topics.TryUpdate(topic.Topic.Id, new CountingItem<VotingTopic>(item.Count, new VotingTopic(item.Item.Topic, item.Item.IsShowing, votes)), item))
                        {
                            voteRemoved = true;
                            break;
                        }
                    }
                }

                SelectedOptionsPreset = null;
                if (voteRemoved)
                {
                    SafeRunAction(OnTopicsChanged);
                    SafeRunAction(OnVotesChanged);
                }
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
