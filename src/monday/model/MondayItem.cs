using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class MondayItemBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = true;
        public bool name {get; set;} = true;
        public bool board_id {get; set;} = false;
    }

    public class MondayItem
    {
        public string id {get; set;}
        public string name {get; set;}
        public string board_id {get; set;}

        public List<MondayColumnValue> column_values {get; set;}
    }
}