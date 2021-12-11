using System;

namespace monday_integration.src.aqua.model
{
    public class WitreStylePO
    {
        public string PurchaseOrderNo {get; set;}
        public string Vendor {get; set;}
        public string Style {get; set;}
        public string StyleDescription {get; set;}
        public string Color {get; set;}
        public string ColorDescription {get; set;}
        public string POType {get; set;}
        public string Warehouse {get; set;}
        public DateTime IssuedDate {get; set;}
        public DateTime POCancel {get; set;}
        public string Terms {get; set;}
        public DateTime StartDate {get; set;}
        public DateTime EndDate {get; set;}
    }
}