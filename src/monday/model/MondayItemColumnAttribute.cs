using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using monday_integration.src.api;

namespace monday_integration.src.monday.model
{
    public enum MondayItemColumnType{
        text,
        date,
        dropdown,
        numbers,
    }

    public class MondayItemColumnAttribute : Attribute
    {
        public string columnId {get; private set;}
        public MondayItemColumnType columnType {get; private set;}
        public bool update {get; private set;}

        public MondayItemColumnAttribute(string columnId, bool update = true) {
            this.columnId = columnId;
            this.update = update;
            if(columnId == "")
                return;

            var match = Regex.Match(this.columnId, "^([a-z]+)[_0-9]*");
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
        public string GetStringText(object obj) {
            if(obj == null) {
                return "\"\"";
            }
            switch(columnType) {
                case MondayItemColumnType.text:
                case MondayItemColumnType.numbers:
                case MondayItemColumnType.dropdown:
                    return obj.ToString().Replace("\"", "");
                case MondayItemColumnType.date:                    
                    if(obj.GetType() == typeof(DateTime?)) {
                        return ((DateTime?)obj)?.ToString("yyyy-MM-dd");
                    } else if(obj.GetType() == typeof(DateTime)) {
                        return ((DateTime)obj).ToString("yyyy-MM-dd");
                    }
                    return obj.ToString();
                default:
                    throw new InvalidOperationException($"Column type {columnType} is not recognized");
            }
        }

        public string GetStringValue(object obj) {
            return GetValue(obj);
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

        public override bool Equals(object obj)
        {
            return obj is MondayItemColumnAttribute attribute &&
                   base.Equals(obj) &&
                   EqualityComparer<object>.Default.Equals(TypeId, attribute.TypeId) &&
                   columnId == attribute.columnId &&
                   columnType == attribute.columnType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), TypeId, columnId, columnType);
        }
    }
}