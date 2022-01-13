using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class MondayCreateItemParameters : MondayParameters<MondayItem> {
        public Func<MondayItem, string> item_name {get; set;} = (item) => item.name;
        public Func<MondayItem, int?> board_id {get; set;} = (item) => item.board_id;
        public Func<MondayItem, bool> create_labels_if_missing {get; set;} = (item) => true;
        public Func<MondayItem, Dictionary<string, string>> column_values {get; set;} = 
            (item) => item.column_values.ToDictionary(e => e.id, e => e.value);

        public MondayCreateItemParameters(MondayItem item) : base(item) {}
    }
    
    public class MondayUpdateItemParameters : MondayParameters<MondayItem> {
        public Func<MondayItem, int?> item_id {get; set;} = (item) => item.id;
        public Func<MondayItem, int?> board_id {get; set;} = (item) => item.board_id;
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
        public int id {get; set;}
        public string name {get; set;}
        public int? board_id {get; set;}

        public HashSet<MondayColumnValue> column_values {get; set;} = new HashSet<MondayColumnValue>();

        // TODO: manipulate permissions somehow to make this more efficient
        // make column_values read-only and make an add() function of sorts
        public bool isDifferentFromOldItem(MondayItem oldItem) {
            var oldColValDict = oldItem.column_values.ToDictionary(item => item.id);
            foreach(var colVal in column_values) {
                if(colVal.needsUpdating(oldColValDict[colVal.id])) {
                    return true;
                }
            }
            return false;
        }
        public Dictionary<string, string> getChangedValues(MondayItem oldItem) {
            var oldColValDict = oldItem.column_values.ToDictionary(item => item.id);
            var changedValsDict = new Dictionary<string, string>();
            foreach(var colVal in column_values) {
                if(colVal.needsUpdating(oldColValDict[colVal.id])) {
                    changedValsDict.Add(colVal.id, colVal.value);
                }
            }
            return changedValsDict;
        }

        public override bool Equals(object obj)
        {
            return obj is MondayItem item &&
                   id == item.id &&
                   name == item.name &&
                   board_id == item.board_id &&
                   EqualityComparer<HashSet<MondayColumnValue>>.Default.Equals(column_values, item.column_values);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, board_id, column_values);
        }
    }

    public class MondayCreateItemResponse {
        public MondayItem create_item {get; set;}
    }

    public class MondayUpdateItemResponse {
        public MondayItem change_multiple_column_values {get; set;}
    }
}