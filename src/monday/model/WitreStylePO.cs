using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using monday_integration.src.api;
using monday_integration.src.aqua.model;
using monday_integration.src.json;
using Newtonsoft.Json;

namespace monday_integration.src.monday.model
{
    public class WitreStylePO
    {

        [MondayHeader()]
        [MondayItemColumnAttribute("text8")]
        public string PurchaseOrderNo {get; set;}

        [MondayItemColumnAttribute("text79")]public string Vendor {get; set;}
        [MondayItemColumnAttribute("text9")]public string Style {get; set;}
        [MondayItemColumnAttribute("text93")]public string StyleDescription {get; set;}
        [MondayItemColumnAttribute("text0")]public string Color {get; set;}
        [MondayItemColumnAttribute("text7")]public string ColorDescription {get; set;}
        [MondayItemColumnAttribute("text")]public string Warehouse {get; set;}
        [MondayItemColumnAttribute("date")]public DateTime? IssuedDate {get; set;} // Date Placed
        [MondayItemColumnAttribute("date15")]public DateTime? POCancel {get; set;} // Goal in Warehouse
        [MondayItemColumnAttribute("date1")]public DateTime? XFactory {get; set;}
        [MondayItemColumnAttribute("text2")]public string XRef {get; set;} // Fty PO # (OG)
        [MondayItemColumnAttribute("numbers1")]public double Price {get; set;}
        [MondayItemColumnAttribute("numbers8")]public int OrderQty {get; set;}

        public string StyleColorID {get; set;}
        [MondayItemColumnAttribute("text5")]public AimsApiLookup fabricContent {get; set;}
        [MondayItemColumnAttribute("dropdown2")]public AimsApiLookup brandName {get; set;}
        [MondayItemColumnAttribute("dropdown0")]public AimsApiLookup body {get; set;}
        [MondayItemColumnAttribute("dropdown9")]public AimsApiLookup originCountry {get; set;}
        [MondayItemColumnAttribute("text23")]public AimsApiLookup sizeScale {get; set;}
        [MondayItemColumnAttribute("dropdown1")]public AimsApiLookup warehouseState {get; set;}

        public List<WitreAimsOrder> aimsOrders;

        [JsonIgnore]
        private static Dictionary<string, PropertyInfo> propInfoDict = typeof(WitreStylePO).GetProperties().ToDictionary(propInfo => propInfo.Name);

        public WitreStylePO(WitreStyleVendorPO vendorPO, IEnumerable<WitreAllocationDetails> allocDetailsEnumerable)
        {
            CopyValuesFromVendorPO(vendorPO);
            InitializeApiLookups();
            InitializeAimsOrders(allocDetailsEnumerable);
        }

        private void InitializeAimsOrders(IEnumerable<WitreAllocationDetails> allocDetailsEnumerable)
        {
            this.aimsOrders = new List<WitreAimsOrder>();
            foreach (var allocDetails in allocDetailsEnumerable)
            {
                var aimsOrder = new WitreAimsOrder(this, allocDetails);
                this.aimsOrders.Add(aimsOrder);
            }
        }

        private void CopyValuesFromVendorPO(WitreStyleVendorPO vendorPO) {
            foreach(var objPropInfo in vendorPO.GetType().GetProperties()) {
                PropertyInfo thisPropInfo;
                if(propInfoDict.TryGetValue(objPropInfo.Name, out thisPropInfo)) {
                    var objPropValue = objPropInfo.GetValue(vendorPO);
                    thisPropInfo.SetValue(this, objPropValue);
                } else {
                    throw new NotImplementedException($"Property {objPropInfo.Name} is not present in WitreStylePO");
                }
            }
        }

        private void InitializeApiLookups() {
            fabricContent   = new AimsApiLookup(StyleColorID, LookupType.FABRIC_CONTENT);
            brandName       = new AimsApiLookup(StyleColorID, LookupType.BRAND_NAME);
            body            = new AimsApiLookup(StyleColorID, LookupType.BODY);
            originCountry   = new AimsApiLookup(StyleColorID, LookupType.ORIGIN_COUNTRY);
            sizeScale       = new AimsApiLookup(StyleColorID, LookupType.SIZE_SCALE);
            warehouseState  = new AimsApiLookup(Warehouse, LookupType.WAREHOUSE_STATE);
        }

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }
}