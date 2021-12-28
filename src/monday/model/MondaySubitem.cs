using System;
using System.Collections.Generic;
using System.Linq;

namespace monday_integration.src.monday.model
{
    public class MondaySubitemBodyOptions : MondayBodyOptions {
        public bool parent_item_id {get; set;} = false;
        public bool name {get; set;} = true;
    }

    public class MondaySubitem
    {
        public string item_name {get; set;}
        public List<MondayColumnValue> column_values {get; set;} = new List<MondayColumnValue>();

        private MondayItem parent_item {get; set;}

        public MondaySubitem(MondayItem parent_item, string item_name) {
            this.parent_item = parent_item;
            this.item_name = item_name;
        }

        public string GetCreateSubitemParameters()
        {
            if (parent_item.id == null || item_name == null)
            {
                throw new InvalidOperationException("parent_item_id and item_name must not be null");
            }
            Dictionary<string, string> paramDict = GetParamDictionary();
            var paramStrList = paramDict.Select(pair => $"{pair.Key}: {pair.Value}");
            return string.Join(", ", paramStrList);
        }

        private Dictionary<string, string> GetParamDictionary()
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["item_name"] = $"\"{item_name}\"";
            paramDict["parent_item_id"] = parent_item.id;
            if (column_values.Count > 0)
            {
                var colValStr = MondayColumnValue.GetColumnValuesStr(column_values);
                paramDict["column_values"] = $"\"{colValStr}\"";
            }

            return paramDict;
        }
    }
}