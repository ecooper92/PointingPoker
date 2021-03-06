﻿using System;

namespace PointingPoker.Data
{
    public class Topic
    {
        public Topic(string name, string value)
            : this(Guid.NewGuid().ToString(), name, value) { }

        public Topic(string id, string name, string discussion)
        {
            Id = id;
            Name = name;
            Discussion = discussion;
        }

        public string Id { get; } = string.Empty;

        public string Name { get; } = string.Empty;

        public string Discussion { get; } = string.Empty;

        /// <summary>
        /// Returns true if the option has the same id, but modified fields.
        /// </summary>
        public bool IsModified(Topic other)
        {
            return this.Id == other.Id
                && (this.Name != other.Name || this.Discussion != other.Discussion);
        }
    }
}
