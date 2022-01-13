using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monday_integration.src.monday.model
{
    public class MondayColumnValueBodyOptions : MondayBodyOptions {
        public bool title {get; set;} = false;
        public bool id {get; set;} = true;
        public bool value {get; set;} = true;
        public bool text {get; set;} = true;
    }

    public class MondayColumnValue
    {
        public string id {get; set;}
        public string value {get; set;}
        public string title {get; set;}
        public string text {get; set;}

        private MondayItemColumnAttribute columnAttribute;

        public MondayColumnValue() {

        }

        public MondayColumnValue(MondayItemColumnAttribute columnAttribute, object obj) {
            this.id = columnAttribute.columnId;
            this.value = columnAttribute.GetStringValue(obj);
            this.text = columnAttribute.GetStringText(obj);
            this.columnAttribute = columnAttribute;
        }

        public bool needsUpdating(MondayColumnValue colVal) {
            // dropdown values are unique in that they're case-insensitive in the Monday.com api, so
            // Consequence == CONSEQUENCE.
            if(colVal.id.StartsWith("dropdown") && this.text.Trim().ToUpper() == colVal.text.Trim().ToUpper())
                return false;
            if(columnAttribute != null && !columnAttribute.update) {
                return false;
            }
            return colVal.text.Trim() != this.text.Trim();
        }

        public override string ToString()
        {
            return $"\"{id}\": {value}";
        }

        public static string GetColumnValuesStr(HashSet<MondayColumnValue> column_values) {
            var colValStrList = column_values.Select(colval => colval.ToString());
            var joinedColVals = string.Join(", ", colValStrList);
            return "\"{" + joinedColVals + "}\"";
        }

        public override bool Equals(object obj)
        {
            return obj is MondayColumnValue value &&
                   id == value.id &&
                   this.value == value.value &&
                   title == value.title;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(id, value, title);
        }
    }
}