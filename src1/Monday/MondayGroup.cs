using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIMS360.Monday
{
    public class MondayGroup
    {
        [JsonProperty("id")]
        public string Id {get; set;}
        [JsonProperty("title")]
        public string Name {get; set;}
        [JsonProperty("items")]
        public List<MondayItem> Items {get; set;}
    }

    public class MondayGroupCollection
    {
        [JsonProperty("groups")]
        public List<MondayGroup> Groups {get; set;}
    }
}