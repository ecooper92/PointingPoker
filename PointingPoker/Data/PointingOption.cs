using System;

namespace PointingPoker.Data
{
    public class PointingOption : IModel
    {
        public PointingOption(string name, string value)
            : this(Guid.NewGuid().ToString(), name, value) { }

        public PointingOption(string id, string name, string value)
        {
            Id = id;
            Name = name;
            Value = value;
        }

        public string Id { get; } = string.Empty;

        public string Name { get; } = string.Empty;

        public string Value { get; } = string.Empty;

        /// <summary>
        /// Returns true if the option has the same id, but modified fields.
        /// </summary>
        public bool IsModified(PointingOption other)
        {
            return this.Id == other.Id
                && (this.Name != other.Name || this.Value != other.Value);
        }
    }
}
