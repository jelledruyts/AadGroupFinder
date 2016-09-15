using GroupFinder.Common.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GroupFinder.Common.Aad
{
    [DebuggerDisplay("Group: {DisplayName}")]
    public class AadGroup : AadEntity, IPartialGroup
    {
        public const string ObjectTypeName = "Group";

        // Use a placeholder value to detect if a JSON value was "null" (in which case the null will overwrite the placeholder)
        // or if it was missing entirely (in which case the placeholder remains and gets fixed up later).
        // The value is unimportant as long as it's never going to be returned in JSON.
        private const string Placeholder = "__PLACEHOLDER__{56F381F7-AF3D-49B1-B204-0E603DA59C48}";

        [JsonProperty(nameof(DisplayName))]
        public string __DisplayName { get; set; }
        [JsonIgnore]
        public Optional<string> DisplayName { get; set; }

        [JsonProperty(nameof(Description))]
        public string __Description { get; set; }
        [JsonIgnore]
        public Optional<string> Description { get; set; }

        [JsonProperty(nameof(Mail))]
        public string __Mail { get; set; }
        [JsonIgnore]
        public Optional<string> Mail { get; set; }

        public bool? MailEnabled { get; set; }

        [JsonProperty(nameof(MailNickname))]
        public string __MailNickname { get; set; }
        [JsonIgnore]
        public Optional<string> MailNickname { get; set; }

        public bool? SecurityEnabled { get; set; }

        public AadGroup()
        {
            // Set up the placeholders to be detected after deserialization.
            this.__DisplayName = Placeholder;
            this.__Description = Placeholder;
            this.__Mail = Placeholder;
            this.__MailNickname = Placeholder;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            this.DisplayName = this.__DisplayName == Placeholder ? Optional<string>.Empty : new Optional<string>(this.__DisplayName);
            this.Description = this.__Description == Placeholder ? Optional<string>.Empty : new Optional<string>(this.__Description);
            this.Mail = this.__Mail == Placeholder ? Optional<string>.Empty : new Optional<string>(this.__Mail);
            this.MailNickname = this.__MailNickname == Placeholder ? Optional<string>.Empty : new Optional<string>(this.__MailNickname);
        }
    }
}