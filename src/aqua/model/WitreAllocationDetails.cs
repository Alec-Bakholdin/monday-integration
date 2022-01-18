using System;
using CsvHelper.Configuration.Attributes;
using monday_integration.src.api;
using monday_integration.src.monday.model;

namespace monday_integration.src.aqua.model
{
    public class WitreAllocationDetails
    {
        [MondayHeader()]
        [MondayItemColumnAttribute("text96")]
        public string CustomerPurchaseOrder {get; set;}

        public string WIPReference {get; set;}
        public RecordType RecordType {get; set;}
        [MondayHeader()]public string Style {get; set;}
        [MondayHeader()]public string Color {get; set;}
        public string StyleColorID {get; set;}

        [MondayItemColumnAttribute("dropdown")]     public string CustomerName {get; set;}
        [MondayItemColumnAttribute("text4")]        public string Order {get; set;}
        [MondayItemColumnAttribute("date44")]       public DateTime? StartDate {get; set;}
        [MondayItemColumnAttribute("date6")]        public DateTime? Complete {get; set;}
        [MondayItemColumnAttribute("dropdown40")]   public string TermDescription {get; set;}
        [MondayItemColumnAttribute("numbers")]      public int Allocatedqty {get; set;}
        [MondayItemColumnAttribute("numbers6")]     public AimsApiLookup stylePrice {get {return new AimsApiLookup(StyleColorID, LookupType.STYLE_PRICE);}}
        [MondayItemColumnAttribute("date51")]       public AimsApiLookup orderReceivedDate {get {return new AimsApiLookup(Order, LookupType.ORDER_ENTERED_DATE);}}
        
        [Name("StartDate")]
        [MondayItemColumnAttribute("date_17", false)]
        public DateTime? StartDate2 {get; set;}
        [Name("Complete")]
        [MondayItemColumnAttribute("date5", false)]
        public DateTime? Complete2 {get; set;}

        public WitreAllocationDetails ShallowCopy() {
            return (WitreAllocationDetails)this.MemberwiseClone();
        }

        public string GetIdentifier() {
            return WIPReference + Order + CustomerPurchaseOrder + Style + Color;
        }
    }
}