using System.Diagnostics;

namespace GroupFinder.Common.Models
{
    [DebuggerDisplay("{Value}")]
    public struct Optional<T>
    {
        public static readonly Optional<T> Empty = new Optional<T>();

        public T Value { get; }
        public bool HasValue { get; }

        public Optional(T value)
        {
            this.Value = value;
            this.HasValue = true;
        }

        public override string ToString()
        {
            return this.HasValue && this.Value != null ? this.Value.ToString() : "null";
        }
    }
}