using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monday_integration.src.monday.model
{
    public class MondayColumnValue
    {
        public string id {get; set;}
        public string title {get; set;}
        public string value {get; set;}

        public static string GetColumnValuesStr(List<MondayColumnValue> column_values) {
            var jobj = new JObject();
            foreach(var col_val in column_values) {
                jobj[col_val.id] = col_val.value;
            }
            var serialized = JsonConvert.SerializeObject(jobj);
            return serialized.Replace(@"""", @"\""");
        }
    }
}