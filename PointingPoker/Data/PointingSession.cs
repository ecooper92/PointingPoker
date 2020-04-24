using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class PointingSession
    {
        const int MAX_OPTIONS = 100;
        const int MAX_PARTICIPANTS = 200;

        private ConcurrentDictionary<string, CountingItem<PointingOption>> _options;
        private ConcurrentDictionary<string, Participant> _participants;

        public event Action OnOptionsChanged;
        public event Action OnParticipantsChanged;

        public PointingSession() : this(Guid.NewGuid().ToString()) { }

        public PointingSession(string id)
        {
            Id = id;
            _options = new ConcurrentDictionary<string, CountingItem<PointingOption>>();
            _participants = new ConcurrentDictionary<string, Participant>();

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

        public IEnumerable<PointingOption> Options => _options.OrderBy(o => o.Value.Count).Select(o => o.Value.Item).ToArray();

        public IEnumerable<Participant> Participants => _participants.Select(o => o.Value).ToArray();

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
                if (_options.TryUpdate(option.Id, new CountingItem<PointingOption>(option), item))
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
