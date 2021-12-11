using System.Collections.Generic;

namespace monday_integration.src.aqua.model
{
    public class AquaPublishLinkResponse<T>
    {
        public int totalRecordCount {get; set;}
        public List<T> data {get; set;}
    }
}