using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class MondayCreateItemParameters : MondayParameters<MondayItem> {
        public Func<MondayItem, string> item_name {get; set;} = (item) => item.name;
        public Func<MondayItem, long?> board_id {get; set;} = (item) => item.board_id;
        public Func<MondayItem, bool> create_labels_if_missing {get; set;} = (item) => true;
        public Func<MondayItem, Dictionary<string, MondayColumnValue>> column_values {get; set;} = (item) => item.columnValueDict;

        public MondayCreateItemParameters(MondayItem item) : base(item) {}
    }
    
    public class MondayUpdateItemParameters : MondayParameters<MondayItem> {
        public Func<MondayItem, long?> item_id {get; set;} = (item) => item.id;
        public Func<MondayItem, long?> board_id {get; set;} = (item) => item.board_id;
        public Func<MondayItem, bool> create_labels_if_missing {get; set;} = (item) => true;
        public Func<MondayItem, Dictionary<string, string>> column_values {get; private set;}
        
        public MondayUpdateItemParameters(MondayItem oldItem, MondayItem newItem) : base(newItem) {
            this.item_id = (item) => oldItem.id; //TODO: resolve this somehow, idk
            this.column_values = (item) => item.getChangedValues(oldItem);
        }
    }

    public class MondayItemBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = true;
        public bool name {get; set;} = false;
        public MondayColumnValueBodyOptions column_values {get; set;} = null;
    }

    public class MondayItem
    {
        public long id {get; set;}
        public string name {get; set;}
        public long? board_id {get; set;}


        public IReadOnlyCollection<MondayColumnValue> columnValues {
            get {
                return _column_values;
            }
        }
        [JsonProperty("column_values")]
        private HashSet<MondayColumnValue> _column_values = new HashSet<MondayColumnValue>();
        public Dictionary<string, MondayColumnValue> columnValueDict {
            get {
                if(_columnValueDict == null)
                    _columnValueDict = columnValues.ToDictionary(colVal => colVal.id);
                return _columnValueDict;
            } 
            private set {
                _columnValueDict = value; 
            }
        }
        private Dictionary<string, MondayColumnValue> _columnValueDict;
        
        public void AddColumnValue(MondayColumnValue columnValue) {
            if(columnValue.id == null) {
                throw new InvalidOperationException("Column value id must not be null");
            }
            if(columnValueDict.ContainsKey(columnValue.id)) {
                _column_values.RemoveWhere(colVal => colVal.id == columnValue.id);
            }
            _column_values.Add(columnValue);
            columnValueDict[columnValue.id] = columnValue;
        }

        public void AddAllColumnValues(IEnumerable<MondayColumnValue> colValueList) {
            foreach(var colVal in colValueList){ 
                AddColumnValue(colVal);
            }
        }

        public bool isDifferentFromOldItem(MondayItem oldItem) {
            foreach(var colVal in oldItem.columnValues) {
                if(colVal.needsUpdating(oldItem.columnValueDict[colVal.id])) {
                    return true;
                }
            }
            return false;
        }
        
        public Dictionary<string, string> getChangedValues(MondayItem oldItem) {
            var changedValsDict = new Dictionary<string, string>();
            foreach(var colVal in oldItem.columnValues) {
                if(colVal.needsUpdating(oldItem.columnValueDict[colVal.id])) {
                    changedValsDict.Add(colVal.id, colVal.value);
                }
            }
            return changedValsDict;
        }
    }

    public class MondayCreateItemResponse {
        public MondayItem create_item {get; set;}
    }

    public class MondayUpdateItemResponse {
        public MondayItem change_multiple_column_values {get; set;}
    }
}