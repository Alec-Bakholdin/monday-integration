using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using monday_integration.src.monday.model;

namespace monday_integration.src.monday
{
    public class MondayClient
    {
        private MondayApi api;
        public MondayClient() {
            api = MondayApiFactory.GetApi();
        }

        public List<MondayItem> MapWitreStylePosToMondayItems(IEnumerable<WitreStylePO> stylePOs, string boardId) {
            var itemList = new List<MondayItem>();
            foreach(var stylePO in stylePOs) {
                foreach(var customerPO in stylePO.aimsOrders) {
                    var item = InitializeNewMondayItem(stylePO, customerPO);
                    item.board_id = boardId;
                    itemList.Add(item);
                }
            }
            return itemList;
        }

        private MondayItem InitializeNewMondayItem(params object[] sourceObjects) {
            var item = new MondayItem();
            var headerValues = new List<string>();
            foreach(var obj in sourceObjects) {
                var newHeaders = PopulateItemWithObjectValues(item, obj);
                headerValues.AddRange(newHeaders);
            }
            item.name = String.Join(" - ", headerValues);
            return item;
        }

        private static List<string> PopulateItemWithObjectValues(MondayItem item, object obj)
        {
            List<string> headerValues = new List<string>();
            foreach (var propInfo in obj.GetType().GetProperties())
            {
                var itemColAttribute = propInfo.GetCustomAttribute<MondayItemColumnAttribute>();
                if (itemColAttribute != null)
                {
                    MondayColumnValue columnValue = new MondayColumnValue(itemColAttribute, propInfo.GetValue(obj));
                    item.column_values.Add(columnValue);
                }
                var itemHeaderAttribute = propInfo.GetCustomAttribute<MondayHeaderAttribute>();
                if (itemHeaderAttribute != null)
                {
                    var headerValue = propInfo.GetValue(obj).ToString();
                    headerValues.Add(headerValue);
                }
            }
            return headerValues;
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

        public async Task<MondayBoard> GetMondayBoard(string boardId) {
            int boardIdParsed;
            if(boardId == null || !Int32.TryParse(boardId, out boardIdParsed)) {
                throw new InvalidOperationException("boardId string must be a non-null integer");
            }
            var paramOptions = new MondayBoardParameterOptions(){ids=boardIdParsed};
            var itemOptions = new MondayItemBodyOptions(){name=true};
            var bodyOptions = new MondayBoardBodyOptions(){name=true, id=true, items=itemOptions};
            var boards = await GetMondayBoards(paramOptions, bodyOptions);
            if(boards.Count != 1) {
                throw new InvalidOperationException($"GetMondayBoard expects 1 board from Monday's API but found {boards.Count}");
            }
            return boards[0];
        }

        public async Task<List<MondayBoard>> GetMondayBoards(MondayBoardParameterOptions paramOptionsObj = null, MondayBoardBodyOptions bodyOptionsObj = null) {
            paramOptionsObj = paramOptionsObj == null ? new MondayBoardParameterOptions() : paramOptionsObj;
            bodyOptionsObj = bodyOptionsObj == null ? new MondayBoardBodyOptions() : bodyOptionsObj;
            var query = @"{boards($parameters){$body_options}}";
            var variables = new {
                parameters = paramOptionsObj.GetParameters(),
                body_options = bodyOptionsObj.GetBody()
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