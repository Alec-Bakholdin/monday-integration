using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using monday_integration.src.api;
using monday_integration.src.aqua.model;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class WitreAimsOrder
    {
        [MondayHeader()]
        [MondayItemColumnAttribute("text96")]
        public string CustomerPurchaseOrder {get; set;}

        public string WIPReference {get; set;}
        public RecordType RecordType {get; set;}
        public string Style {get; set;}
        public string Color {get; set;}

        [MondayItemColumnAttribute("dropdown4")]public string Account {get; set;}
        [MondayItemColumnAttribute("dropdown")]public string CustomerName {get; set;}
        [MondayItemColumnAttribute("text4")]public string Order {get; set;}
        [MondayItemColumnAttribute("date44")]public DateTime? StartDate {get; set;}
        [MondayItemColumnAttribute("date6")]public DateTime? Complete {get; set;}
        [MondayItemColumnAttribute("numbers")]public int Allocatedqty {get; set;}

        [MondayItemColumnAttribute("numbers6")]public AimsApiLookup stylePrice {get; set;}
        [MondayItemColumnAttribute("date51")]public AimsApiLookup orderReceivedDate {get; set;}

        [JsonIgnore]
        private static Dictionary<string, PropertyInfo> propInfoDict = typeof(WitreAimsOrder).GetProperties().ToDictionary(propInfo => propInfo.Name);

        public WitreAimsOrder(WitreStylePO parent, WitreAllocationDetails allocDetails) {
            CopyValuesFromAllocationDetails(allocDetails);
            stylePrice = new AimsApiLookup(parent.StyleColorID, LookupType.STYLE_PRICE);
            orderReceivedDate = new AimsApiLookup(Order, LookupType.ORDER_ENTERED_DATE);
        }

        private void CopyValuesFromAllocationDetails(WitreAllocationDetails allocDetails) {
            foreach(var objPropInfo in allocDetails.GetType().GetProperties()) {
                PropertyInfo thisPropInfo;
                if(propInfoDict.TryGetValue(objPropInfo.Name, out thisPropInfo)) {
                    var objPropValue = objPropInfo.GetValue(allocDetails);
                    thisPropInfo.SetValue(this, objPropValue);
                } else {
                    throw new NotImplementedException($"Property {objPropInfo.Name} is not present in WitreAimsOrder");
                }
            }
        }
    }
}