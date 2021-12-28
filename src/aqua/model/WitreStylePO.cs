using System;

namespace monday_integration.src.aqua.model
{
    public class WitreStylePO
    {
        public string PurchaseOrderNo {get; set;}
        public string Vendor {get; set;}

        [MondaySubitemColumnAttribute("text9")]public string Style {get; set;}
        [MondaySubitemColumnAttribute("text")]public string StyleDescription {get; set;}
        [MondaySubitemColumnAttribute("text2")]public string Color {get; set;}
        [MondaySubitemColumnAttribute("text5")]public string ColorDescription {get; set;}
        public string POType {get; set;}
        [MondayItemColumnAttribute("text4")]public string Warehouse {get; set;}
        [MondayItemColumnAttribute("date")]public DateTime IssuedDate {get; set;}
        [MondayItemColumnAttribute("date8")]public DateTime POCancel {get; set;}
        public string Terms {get; set;}
        [MondayItemColumnAttribute("date5")]public DateTime StartDate {get; set;}
        public DateTime EndDate {get; set;}
    }
}