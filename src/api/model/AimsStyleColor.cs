using System.Collections.Generic;
using System.Linq;

namespace monday_integration.src.api.model
{
    public class AimsStyleColor
    {

        public string styleID {get; set;}
        public string bodyCode {get; set;}
        public string bodyDescription {get; set;}
        public string brandName {get; set;}
        public string originCountryName {get; set;}
        public double wholesalePrice {get; set;}
        public List<AimsSize> sizes {get; set;}

        public string GetSizeScale() {
            return string.Join(",", sizes.Select(size => size.sizeName));
        }

        public string GetBody() {
            return $"{bodyCode} - {bodyDescription}";
        }
    }
}