using System;
using System.Text.RegularExpressions;
using monday_integration.src.api;

namespace monday_integration.src.monday.model
{
    public enum MondayItemColumnType{
        text,
        date,
        dropdown,
        numbers,
        connect_boards
    }

    public class MondayItemColumnAttribute : Attribute
    {
        public string columnId {get; private set;}
        public MondayItemColumnType columnType {get; private set;}

        public MondayItemColumnAttribute(string columnId) {
            this.columnId = columnId;
            if(columnId == "")
                return;

            var match = Regex.Match(this.columnId, "^([a-z_]+)[0-9]*");
            this.columnType = Enum.Parse<MondayItemColumnType>(match.Groups[1].Value);
        }

/*
use this for connect_boards, where the item id is from the other board
mutation{
  create_item(item_name:"test", board_id:2080913842, column_values:"{\"connect_boards\": {\"item_ids\" : [2075494263]}}"){
    id, name
  }
}
*/
        public string GetStringValue(object obj) {
            return GetValue(obj).Replace("\"", @"\""");
        }

        private String GetValue(object obj) {
            if(obj == null) {
                return "\"\"";
            }
            if(obj.GetType() == typeof(AimsApiLookup)) {
                obj = obj.ToString();
            }
            switch(columnType) {
                case MondayItemColumnType.numbers:
                case MondayItemColumnType.text:
                    return $"\"{obj.ToString().Replace("\"", "")}\"";
                case MondayItemColumnType.dropdown:
                    return $"{{\"labels\": [\"{obj.ToString().Replace("\"", "")}\"]}}";
                case MondayItemColumnType.date:
                    if(obj.GetType() == typeof(string))
                        return $"\"{obj.ToString().Replace("\"", "")}\"";
                    var dateTimeObj = obj.GetType() == typeof(DateTime?) ? ((DateTime?)obj).Value : (DateTime)obj;
                    return $"\"{dateTimeObj.ToString("yyyy-MM-dd")}\"";
                default:
                    throw new NotImplementedException($"Haven't implemented column type {columnType}");
            }   
        }
    }
}