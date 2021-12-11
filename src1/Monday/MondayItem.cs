using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIMS360.Monday
{
    public class MondayItem
    {
        [JsonProperty("id")]
        public string Id;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("board")]
        public MondayBoard Board;
        [JsonProperty("group")]
        public string GroupId;
        
    }

    public class MondayItemCollection
    {
        [JsonProperty("items")]
        public List<MondayItem> Items {get; set;}
    }

    public class CreateMondayItemResponse
    {
        [JsonProperty("create_item")]
        public MondayItem Item {get; set;}
    }

    public class CreateMondaySubitemResponse
    {
        [JsonProperty("create_subitem")]
        public MondayItem Item {get; set;}
    }

    public class ChangeMultipleColumnValuesResponse
    {
        [JsonProperty("change_multiple_column_values")]
        public MondayItem Item {get; set;}
    }
}