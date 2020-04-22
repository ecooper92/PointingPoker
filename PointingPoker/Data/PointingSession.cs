using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class PointingSession
    {
        private ConcurrentDictionary<string, PointingOption> _options;

        public event Action OnOptionsChanged;

        public PointingSession()
        {
            _options = new ConcurrentDictionary<string, PointingOption>();
        }

        public string Id { get; set; } = string.Empty;

        public IEnumerable<PointingOption> Options => _options.Select(o => o.Value).ToArray();

        public void AddOption(PointingOption option)
        {
            while (!_options.TryAdd(option.Id, option))
            {
                option = new PointingOption(option.Name, option.Value);
            }

            SafeRunAction(OnOptionsChanged);
        }

        public void UpdateOption(PointingOption option)
        {
            // Attempt to update until successful or if the option is removed/doesn't exist.
            while (_options.TryGetValue(option.Id, out var currentOption) && currentOption.IsModified(option))
            {
                if (_options.TryUpdate(option.Id, option, currentOption))
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
