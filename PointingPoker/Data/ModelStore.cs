using PointingPoker.Shared;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class ModelStore<T> : ICollection<T>
        where T : IModel
    {
        private readonly int _maxModels = 50;
        private readonly ConcurrentDictionary<string, CountingItem<T>> _models;

        public event Action<T> OnAdd;
        public event Action<T> OnUpdate;
        public event Action<T> OnDelete;

        public ModelStore()
        {
            _models = new ConcurrentDictionary<string, CountingItem<T>>();
        }

        public int Count => _models.Count;

        public bool IsReadOnly => false;

        public T Find(string modelId)
        {
            if (!string.IsNullOrEmpty(modelId) && _models.TryGetValue(modelId, out var model))
            {
                return model.Item;
            }

            return default(T);
        }

        public bool TryGet(string modelId, out T model)
        {
            model = Find(modelId);
            return model != null;
        }

        public void Add(T model) => TryAdd(model);

        public bool TryAdd(T model)
        {
            // Sanity check
            if (_models.Count > _maxModels)
            {
                throw new InvalidOperationException($"Only up to {_maxModels} are supported per session.");
            }

            if (_models.TryAdd(model.Id, new CountingItem<T>(model)))
            {
                OnAdd?.Invoke(model);
                return true;
            }

            return false;
        }

        public T AddOrUpdate(T model)
        {
            var isUpdate = false;
            var result = _models.AddOrUpdate(model.Id, k => new CountingItem<T>(model), (k, v) =>
            {
                isUpdate = true;
                return new CountingItem<T>(v.Count, model);
            });

            if (isUpdate)
            {
                OnUpdate?.Invoke(model);
            }
            else
            {
                OnAdd?.Invoke(model);
            }

            return result.Item;
        }

        public bool Update(T model) => Update(model?.Id, existingModel => model);

        public bool Update(string modelId, Func<T, T> updateFunction)
        {
            if (!string.IsNullOrEmpty(modelId) && _models.TryUpdate(modelId, existingModel => new CountingItem<T>(existingModel.Count, updateFunction(existingModel.Item)), out var updatedValue))
            {
                OnUpdate?.Invoke(updatedValue.Item);
                return true;
            }

            return false;
        }

        public void Clear() => _models.Clear();

        public bool Contains(T model) => !string.IsNullOrEmpty(model?.Id) && _models.ContainsKey(model.Id);

        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(T model) => Remove(model?.Id);

        public bool Remove(string modelId)
        {
            if (_models.TryRemove(modelId, out var removedModel))
            {
                OnDelete?.Invoke(removedModel.Item);
                return true;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator() => _models
            .OrderBy(c => c.Value.Count)
            .Select(c => c.Value.Item)
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _models.GetEnumerator();
    }
}
