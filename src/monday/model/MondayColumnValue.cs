using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monday_integration.src.monday.model
{
    public class MondayColumnValue
    {
        public string id {get {return columnAttribute.columnId;}}
        public string value {get {return columnAttribute.GetStringValue(obj);}}
        public string title {get; set;}

        private MondayItemColumnAttribute columnAttribute {get; set;}
        private object obj {get; set;}
        
        public MondayColumnValue() {

        }

        public MondayColumnValue(MondayItemColumnAttribute columnAttribute, object obj) {
            this.columnAttribute = columnAttribute;
            this.obj = obj;
        }

        public override string ToString()
        {
            return $"\\\"{id}\\\": {value}";
        }

        public static string GetColumnValuesStr(List<MondayColumnValue> column_values) {
            var colValStrList = column_values.Select(colval => colval.ToString());
            var joinedColVals = string.Join(", ", colValStrList);
            return "\"{" + joinedColVals + "}\"";
        }
    }
}