using System;
using System.Threading.Tasks;
using monday_integration.src.api.model;
using monday_integration.src.json;
using monday_integration.src.logging;
using Newtonsoft.Json;

namespace monday_integration.src.api
{
    [JsonConverter(typeof(ToStringJsonConverter))]
    public class AimsApiLookup
    {
        private static AimsLogger logger = AimsLoggerFactory.CreateLogger(typeof(AimsApiLookup));
        private string identifier;
        private LookupType lookupType;
        private AimsApi api;


        public AimsApiLookup(string identifier, LookupType lookupType) {
            api = AimsApiFactory.GetApi();
            this.identifier = identifier;
            this.lookupType = lookupType;
        }

        public async Task<string> GetValue() {
            var obj = await GetObject();
            var mappingFunc = GetValueMappingFunc();
            var value = mappingFunc(obj);
            return value ?? "";
        }

        public override string ToString()
        {
            var valueTask = GetValue();
            valueTask.Wait();
            return valueTask.Result.ToString();
        }



        private string warehouseStateUrl {
            get{
                return $"/warehouses/v1.0/warehouses?" +
                       $"$filter=warehouseName eq '{identifier}'&" +
                        "$select=state";
            }
        }
        private string orderUrl {
            get {
                return $"/orders/v1.0/orders?" +
                        "$select=entered&" +
                       $"$filter=order eq '{identifier}'"; 
            }
        }
        private string styleColorUrl {
            get{
                return $"/StyleColors/v1.1/StyleColors?" +
                        "$expand=sizes($select=sizeName)&" +
                        "$select=brandName,originCountryName,styleID,bodyCode,bodyDescription,wholesalePrice&" +
                       $"$filter=styleColorID eq {identifier}";
            }
        }
        private string styleIdUrl(string styleId) {
            return $"/styles/V1.0/style/{styleId}";
        }

        private async Task<object> GetObject() {
            switch(lookupType){
                case LookupType.BRAND_NAME:
                case LookupType.ORIGIN_COUNTRY:
                case LookupType.BODY:
                case LookupType.SIZE_SCALE:
                case LookupType.STYLE_PRICE:
                case LookupType.STYLE_ID:
                    return await GetSingleODataObject<AimsStyleColor>(styleColorUrl);
                case LookupType.ORDER_ENTERED_DATE:
                    return await GetSingleODataObject<AimsOrder>(orderUrl);
                case LookupType.WAREHOUSE_STATE:
                    return await GetSingleODataObject<AimsWarehouse>(warehouseStateUrl);
                case LookupType.FABRIC_CONTENT:

                    var styleId = await new AimsApiLookup(identifier, LookupType.STYLE_ID).GetValue();
                    return await api.GetCachedResponseAsync<AimsStyle>(styleIdUrl(styleId));

                default:
                    throw new NotImplementedException($"Unknown lookup type for last layer {lookupType}");
            }
        }

        private async Task<object> GetSingleODataObject<T>(string url)
        {
            var odataResponse = await api.GetCachedResponseAsync<AimsODataResponse<T>>(url);
            if (odataResponse.value.Count != 1)
            {
                throw new InvalidOperationException($"{url} returned {odataResponse.value.Count} values instead of 1");
            }
            return odataResponse.value[0];
        }

        private Func<object, string> GetValueMappingFunc() {
            switch(lookupType){
                case LookupType.BRAND_NAME:
                    return (obj) => ((AimsStyleColor)obj).brandName;
                case LookupType.ORIGIN_COUNTRY:
                    return (obj) => ((AimsStyleColor)obj).originCountryName;
                case LookupType.STYLE_ID:
                    return (obj) => ((AimsStyleColor)obj).styleID;
                case LookupType.STYLE_PRICE:
                    return (obj) => ((AimsStyleColor)obj).wholesalePrice.ToString();
                case LookupType.BODY:
                    return (obj) => ((AimsStyleColor)obj).GetBody();
                case LookupType.SIZE_SCALE:
                    return (obj) => ((AimsStyleColor)obj).GetSizeScale();
                case LookupType.ORDER_ENTERED_DATE:
                    return (obj) => ((AimsOrder)obj).entered?.ToString("yyyy-MM-dd");
                case LookupType.WAREHOUSE_STATE:
                    return (obj) => ((AimsWarehouse)obj).state;
                case LookupType.FABRIC_CONTENT:
                    return (obj) => ((AimsStyle)obj).fabricContent;
                default:
                    throw new NotImplementedException($"Unknown lookup type for last layer {lookupType}");
            }
        }
    }
}