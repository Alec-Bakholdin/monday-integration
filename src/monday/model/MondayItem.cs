using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class MondayItem
    {
        public string name {get; set;}
        public string id {get; set;}

        public List<MondayColumnValue> column_values {get; set;}
    }
}