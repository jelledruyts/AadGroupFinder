using Newtonsoft.Json;

namespace GroupFinder.Common.Aad
{
    public abstract class AadEntity
    {
        public string ObjectId { get; set; }
        public string ObjectType { get; set; }
        [JsonProperty("aad.isDeleted")]
        public bool IsDeleted { get; set; }

        public override bool Equals(object obj)
        {
            return obj != null && obj is AadEntity && ((AadEntity)obj).ObjectId == this.ObjectId;
        }

        public override int GetHashCode()
        {
            return this.ObjectId.GetHashCode();
        }
    }
}