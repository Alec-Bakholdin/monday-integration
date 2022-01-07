using System;
using System.Collections.Generic;
using System.Linq;

namespace monday_integration.src.monday.model
{
    public class MondayItemBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = true;
        public bool name {get; set;} = false;
        public bool board_id {get; set;} = false;
    }

    public class MondayItem
    {
        public string id {get; set;}
        public string name {get; set;}
        public string board_id {get; set;}

        public List<MondayColumnValue> column_values {get; set;} = new List<MondayColumnValue>();

        public string GetCreateItemParameters()
        {
            if (board_id == null || name == null)
            {
                throw new InvalidOperationException("Board ID and item name must not be null");
            }
            Dictionary<string, string> paramDict = GetParamDictionary();
            var paramStrList = paramDict.Select(pair => $"{pair.Key}: {pair.Value}");
            return string.Join(", ", paramStrList);
        }

        private Dictionary<string, string> GetParamDictionary()
        {
            var paramDict = new Dictionary<string, string>();
            paramDict["item_name"] = $"\"{name}\"";
            paramDict["board_id"] = board_id;
            paramDict["create_labels_if_missing"] = "true";
            if (column_values.Count > 0)
            {
                var colValStr = MondayColumnValue.GetColumnValuesStr(column_values);
                paramDict["column_values"] = colValStr;
            }

            return paramDict;
        }
    }

    public class MondayCreateItemResponse {
        public MondayItem create_item {get; set;}
    }
}