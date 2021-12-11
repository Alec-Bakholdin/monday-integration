using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIMS360.Monday
{
    public class MondayColumn
    {
        [JsonProperty("id")]
        public string Id {get; set;}
        [JsonProperty("title")]
        public string Name {get; set;}
    }

    public class MondayColumnCollection
    {
        [JsonProperty("columns")]
        public List<MondayColumn> Columns {get; set;}
    }
}