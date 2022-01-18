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

        public string StyleColorID {get; set;}
        [MondayItemColumnAttribute("text5")]    public AimsApiLookup fabricContent  {get {return new AimsApiLookup(StyleColorID, LookupType.FABRIC_CONTENT);}}
        [MondayItemColumnAttribute("dropdown2")]public AimsApiLookup brandName      {get {return new AimsApiLookup(StyleColorID, LookupType.BRAND_NAME);}}
        [MondayItemColumnAttribute("dropdown0")]public AimsApiLookup body           {get {return new AimsApiLookup(StyleColorID, LookupType.BODY);}}
        [MondayItemColumnAttribute("dropdown9")]public AimsApiLookup originCountry  {get {return new AimsApiLookup(StyleColorID, LookupType.ORIGIN_COUNTRY);}}
        [MondayItemColumnAttribute("text23")]   public AimsApiLookup sizeScale      {get {return new AimsApiLookup(StyleColorID, LookupType.SIZE_SCALE);}}
        public string Warehouse {get; set;}
        [MondayItemColumnAttribute("dropdown1")]public AimsApiLookup warehouseState {get {return new AimsApiLookup(Warehouse, LookupType.WAREHOUSE_STATE);}}

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
                var item = new MondayItem();
                item.name = GetItemName(allocation);
                var allocationColumnValues = GetObjectColumnValues(allocationDetailsColumnProps, allocation);
                item.AddAllColumnValues(allocationColumnValues);
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