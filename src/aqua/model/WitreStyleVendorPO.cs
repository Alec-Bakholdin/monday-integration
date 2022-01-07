using System;

namespace monday_integration.src.aqua.model
{
    public class WitreStyleVendorPO
    {
        public string PurchaseOrderNo {get; set;}

        // TODO: brand name, body (bodyCode - bodyDescription) from style API
        // TODO: fabric content has to come from style by style id 
        // TODO: size scale (desciption preferable to size scale), have to do lookup in codes
        // TODO: originCountryName
        // TODO: PU Loc: pull state of warehouse by looking up through 
        // TODO: buyer units is total for order
        // TODO: make everything by order line, not vendor po
        // TODO: ONE LINE PER CUSTOMER ORDER LINE

        // Start
        public string Vendor {get; set;}
        public string StyleColorID {get; set;}
        public string Style {get; set;}
        public string StyleDescription {get; set;}
        public string Color {get; set;}
        public string ColorDescription {get; set;}
        public string Warehouse {get; set;}
        public DateTime? IssuedDate {get; set;} // Date Placed
        public DateTime? POCancel {get; set;} // Goal in Warehouse
        //public string EndDate {get; set;}
        public DateTime? XFactory {get; set;} // XFactory
        public string XRef {get; set;} // Fty PO # (OG)
        public double Price {get; set;}
        public int OrderQty {get; set;}
    }
}