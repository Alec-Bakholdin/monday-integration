using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using monday_integration.src.api;
using monday_integration.src.logging;
using monday_integration.src.monday.model;

namespace monday_integration.src.aqua.model
{
    public class WitreStyleVendorPO
    {
        private static AimsLogger logger = AimsLoggerFactory.CreateLogger(typeof(WitreStyleVendorPO));

        [MondayHeader()]
        [MondayItemColumnAttribute("text8")]
        public string PurchaseOrderNo {get; set;}

        [MondayItemColumnAttribute("text79")]   public string Vendor {get; set;}
        [MondayItemColumnAttribute("text9")]    public string Style {get; set;}
        [MondayItemColumnAttribute("text93")]   public string StyleDescription {get; set;}
        [MondayItemColumnAttribute("text0")]    public string Color {get; set;}
        [MondayItemColumnAttribute("text7")]    public string ColorDescription {get; set;}
        [MondayItemColumnAttribute("date")]     public DateTime? IssuedDate {get; set;} // Date Placed
        [MondayItemColumnAttribute("date1")]    public DateTime? XFactory {get; set;} // XFactory
        [MondayItemColumnAttribute("text2")]    public string XRef {get; set;} // Fty PO # (OG)
        [MondayItemColumnAttribute("numbers1")] public double Price {get; set;}
        [MondayItemColumnAttribute("numbers8")] public int OrderQty {get; set;}

        public string StyleColorID {get{return _styleColorID;} set {
            _styleColorID = value;
            lookupFabricContent = new AimsApiLookup(StyleColorID, LookupType.FABRIC_CONTENT);
            lookupBrandName = new AimsApiLookup(StyleColorID, LookupType.BRAND_NAME);
            lookupBody = new AimsApiLookup(StyleColorID, LookupType.BODY);
            lookupOriginCountry = new AimsApiLookup(StyleColorID, LookupType.ORIGIN_COUNTRY);
            lookupSizeScale = new AimsApiLookup(StyleColorID, LookupType.SIZE_SCALE);
        }}
        private string _styleColorID;
        [MondayItemColumnAttribute("text5")]    public String FabricContent {get{return lookupFabricContent.ToString();}}
        [MondayItemColumnAttribute("dropdown2")]public String BrandName     {get{return lookupBrandName.ToString();}}
        [MondayItemColumnAttribute("dropdown0")]public String Body          {get{return lookupBody.ToString();}}
        [MondayItemColumnAttribute("dropdown9")]public String OriginCountry {get{return lookupOriginCountry.ToString();}}
        [MondayItemColumnAttribute("text23")]   public String SizeScale     {get{return lookupSizeScale.ToString();}}
        private AimsApiLookup lookupFabricContent;
        private AimsApiLookup lookupBrandName;
        private AimsApiLookup lookupBody;
        private AimsApiLookup lookupOriginCountry;
        private AimsApiLookup lookupSizeScale;

        public string Warehouse {set {
            _warehouse = value;
            lookupWarehouseState = new AimsApiLookup(value, LookupType.WAREHOUSE_STATE);
            }}
        private string _warehouse;

        [MondayItemColumnAttribute("dropdown1")] public string warehouseState {get{return lookupWarehouseState.ToString();}}
        private AimsApiLookup lookupWarehouseState;

        public List<WitreAllocationDetails> allocationDetails {
            get {
                return _allocationDetails;
            }
            set{
                value.ForEach(e => e.StyleColorID = StyleColorID);
                _allocationDetails = value ?? new List<WitreAllocationDetails>();
            }
        }
        private List<WitreAllocationDetails> _allocationDetails = new List<WitreAllocationDetails>();

        public List<MondayItem> ConvertToMondayItems(MondayBoard targetBoard) {
            if(targetBoard == null || targetBoard.id == default(int))
                throw new InvalidOperationException("When creating a Monday Item, board with a non-null id must be supplied");
            var itemList = new List<MondayItem>();
            var stylePoColValues = GetObjectColumnValues(vendorPoColumnProps, this);
            foreach(var allocation in allocationDetails){
                if(allocation.Style != Style || allocation.Color != Color) {
                    continue;
                }
                var item = new MondayItem();
                item.name = GetItemName(allocation);
                var allocationColumnValues = GetObjectColumnValues(allocationDetailsColumnProps, allocation);
                item.AddAllColumnValues(allocationColumnValues);
                item.AddAllColumnValues(stylePoColValues);
                item.board_id = targetBoard.id;
                itemList.Add(item);
            }
            return itemList;
        }

        private String GetItemName(WitreAllocationDetails allocation) {
            var vendorPoHeaderVals = vendorPoHeaderProps.Select(propInfo => propInfo.GetValue(this));
            var allocationDetailsHeaderVals = allocationDetailsHeaderProps.Select(propInfo => propInfo.GetValue(allocation));
            var headerVals = vendorPoHeaderVals.Concat(allocationDetailsHeaderVals);
            return String.Join(" - ", headerVals);
        }

        private List<MondayColumnValue> GetObjectColumnValues(List<PropertyInfo> colPropInfoList, object obj) {
            var colValueList = new List<MondayColumnValue>();
            foreach(var propInfo in colPropInfoList) {
                var colAttribute = propInfo.GetCustomAttribute<MondayItemColumnAttribute>();
                if(colAttribute == null) {
                    throw new InvalidOperationException("MondayItemColumn attribute cannot be null in colPropInfoList");
                }
                var propValue = propInfo.GetValue(obj);
                var colValue = new MondayColumnValue(colAttribute, propValue);
                colValueList.Add(colValue);
            }
            return colValueList;
        }


        private static List<PropertyInfo> vendorPoHeaderProps = typeof(WitreStyleVendorPO).GetProperties()
                                                .Where(propInfo => propInfo.GetCustomAttribute<MondayHeaderAttribute>() != null)
                                                .ToList();
        private static List<PropertyInfo> allocationDetailsHeaderProps = typeof(WitreAllocationDetails).GetProperties()
                                                .Where(propInfo => propInfo.GetCustomAttribute<MondayHeaderAttribute>() != null)
                                                .ToList();           
        private static List<PropertyInfo> vendorPoColumnProps = typeof(WitreStyleVendorPO).GetProperties()
                                                .Where(propInfo => propInfo.GetCustomAttribute<MondayItemColumnAttribute>() != null)
                                                .ToList();
        private static List<PropertyInfo> allocationDetailsColumnProps = typeof(WitreAllocationDetails).GetProperties()
                                                .Where(propInfo => propInfo.GetCustomAttribute<MondayItemColumnAttribute>() != null)
                                                .ToList();                                                
    }
}