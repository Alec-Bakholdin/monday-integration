using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using monday_integration.src.aqua.model;
using monday_integration.src.monday.model;

namespace monday_integration.src.monday
{
    public class MondayClient
    {
        private MondayApi api;
        public MondayClient() {
            api = MondayApiFactory.GetApi();
        }

        public List<MondayItem> MapWitreStylePosToMondayItems(IEnumerable<WitreStylePO> stylePOs, Dictionary<string, string> vendorBoardIdDict) {
            var itemDict = new Dictionary<string, MondayItem>();
            foreach(var stylePO in stylePOs) {
                if(!itemDict.ContainsKey(stylePO.PurchaseOrderNo)) {
                    itemDict[stylePO.PurchaseOrderNo] = InitializeNewMondayItem(stylePO);
                    if(vendorBoardIdDict.ContainsKey(stylePO.Vendor)) {
                        itemDict[stylePO.PurchaseOrderNo].board_id = vendorBoardIdDict[stylePO.Vendor];
                    }
                }
                var item = itemDict[stylePO.PurchaseOrderNo];
                var subitem = InitializeNewMondaySubitem(stylePO, item);
                item.subitems.Add(subitem);
            }
            return itemDict.Select(pair => pair.Value).ToList();
        }

        private MondaySubitem InitializeNewMondaySubitem(WitreStylePO stylePO, MondayItem parentItem)
        {
            var subitem = new MondaySubitem(parentItem, parentItem.subitems.Count.ToString());
            foreach(var propInfo in typeof(WitreStylePO).GetProperties()) {
                var subitemColAttribute = propInfo.GetCustomAttribute<MondaySubitemColumnAttribute>();
                if(subitemColAttribute != null)
                {
                    MondayColumnValue columnValue = InitializeNewColumnValue(stylePO, propInfo, subitemColAttribute.columnId);
                    subitem.column_values.Add(columnValue);
                }
            }
            return subitem;
        }

        private MondayItem InitializeNewMondayItem(WitreStylePO stylePO) {
            var item = new MondayItem(stylePO.PurchaseOrderNo);
            foreach(var propInfo in typeof(WitreStylePO).GetProperties()) {
                var itemColAttribute = propInfo.GetCustomAttribute<MondayItemColumnAttribute>();
                if(itemColAttribute != null)
                {
                    MondayColumnValue columnValue = InitializeNewColumnValue(stylePO, propInfo, itemColAttribute.columnId);
                    item.column_values.Add(columnValue);
                }
            }
            return item;
        }

        private MondayColumnValue InitializeNewColumnValue(WitreStylePO stylePO, PropertyInfo propInfo, string columnId)
        {
            var columnValue = new MondayColumnValue();
            columnValue.id = columnId;
            columnValue.title = propInfo.Name;
            var value = propInfo.GetValue(stylePO);
            columnValue.value = propInfo.PropertyType == typeof(DateTime) ? ((DateTime)value).ToString("yyyy-MM-dd") : value.ToString();
            return columnValue;
        }



        public async Task<MondaySubitem> CreateMondaySubitem(MondaySubitem subitem, MondaySubitemBodyOptions options = null) {
            var query = "mutation{create_subitem($parameters){$body_options}}";
            var variables = new {
                parameters = subitem.GetCreateSubitemParameters(),
                body_options = options != null ? options.GetBody() : new MondaySubitemBodyOptions().GetBody()
            };

            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};
            var response = await api.MutateAsync<MondaySubitem>(request);
            return response;
        }

        public async Task<MondayItem> CreateMondayItem(MondayItem item, MondayItemBodyOptions options = null) {
            var body_options_obj = options == null ? new MondayItemBodyOptions() : options;
            var query = "mutation{create_item($parameters){$body_options}}";
            var variables = new {
                parameters = item.GetCreateItemParameters(),
                body_options = body_options_obj.GetBody()
            };

            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};
            var response = await api.MutateAsync<MondayCreateItemResponse>(request);
            if(body_options_obj.id) {
                item.id = response.create_item.id;
            }
            return response.create_item;
        }

        public async Task<List<MondayBoard>> GetMondayBoards(MondayBoardBodyOptions options) {
            var query = @"{boards(){$body_options}}";
            var variables = new {
                body_options = options.GetBody()
            };
            
            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};

            var response = await api.QueryAsync<MondayBoardList>(request);
            return response.boards;
        }


        private string SubstituteVariables(string templateString, object variables) {
            var outputString = templateString;
            foreach(var property in variables.GetType().GetProperties()) {
                var varName = "$" + property.Name;
                var varValue = property.GetValue(variables).ToString();
                outputString = outputString.Replace(varName, varValue);
            }
            return outputString;
        }
    }
}