using System;
using System.Collections.Generic;
using monday_integration.src.api;
using monday_integration.src.monday.model;

namespace monday_integration.src.aqua.model
{
    public class WitreStyleVendorPO
    {
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

        public List<WitreAllocationDetails> allocationDetails {get{return _allocationDetails;} set{_allocationDetails = value; value.ForEach(e => e.StyleColorID = StyleColorID);}}
        private List<WitreAllocationDetails> _allocationDetails = new List<WitreAllocationDetails>();

        public List<MondayItem> ConvertToMondayItems(MondayBoard targetBoard) {
            if(targetBoard == null || targetBoard.id == default(int))
                throw new InvalidOperationException("When creating a Monday Item, board with a non-null id must be supplied");
            var itemList = new List<MondayItem>();
            foreach(var allocation in _allocationDetails){
                var item = CreateItemFromAllocationDetails(allocation);
                item.board_id = targetBoard.id;
                itemList.Add(item);
            }
            return itemList;
        }

        private MondayItem CreateItemFromAllocationDetails(WitreAllocationDetails allocation) {
            var item = new MondayItem();
            return item;
        }
    }
}