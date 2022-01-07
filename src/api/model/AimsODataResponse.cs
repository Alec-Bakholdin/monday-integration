using System.Collections.Generic;

namespace monday_integration.src.api.model
{
    public class AimsODataResponse<T>
    {
        public List<T> value {get; set;}
    }
}