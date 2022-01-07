using System;

namespace monday_integration.src.aqua.model
{
    public class WitreAllocationDetails
    {
        public string WIPReference {get; set;}
        public string Order {get; set;}
        public string Account {get; set;}
        public string CustomerName {get; set;}
        public RecordType RecordType {get; set;}
        public string CustomerPurchaseOrder {get; set;}
        public int Allocatedqty {get; set;}
        public DateTime? StartDate {get; set;}
        public DateTime? Complete {get; set;}
        public string Style {get; set;}
        public string Color {get; set;}

        public WitreAllocationDetails ShallowCopy() {
            return (WitreAllocationDetails)this.MemberwiseClone();
        }

        public string GetIdentifier() {
            return WIPReference + Order + Account + CustomerPurchaseOrder + Style + Color;
        }
    }
}