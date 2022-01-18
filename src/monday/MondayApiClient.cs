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
    public class MondayApiClient
    {
        private MondayApi api;
        public MondayApiClient() {
            api = MondayApiFactory.GetApi();
        }

        public async Task<MondayItem> UpdateMondayItem(MondayItem oldItem, MondayItem newItem, MondayUpdateItemParameters reqParams = null, MondayItemBodyOptions options = null)
        {
            var params_obj = reqParams ?? new MondayUpdateItemParameters(oldItem, newItem);
            var body_options_obj = options ?? new MondayItemBodyOptions();
            var query = "mutation{change_multiple_column_values($parameters){$body_options}}";
            var variables = new {
                parameters = params_obj.GetParameters(),
                body_options = body_options_obj.GetBody()
            };

            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};
            var response = await api.MutateAsync<MondayUpdateItemResponse>(request);

            return response.change_multiple_column_values;
        }

        public async Task<MondayItem> CreateMondayItem(MondayItem item, MondayCreateItemParameters reqParams = null, MondayItemBodyOptions options = null) {
            var params_obj = reqParams ?? new MondayCreateItemParameters(item);
            var body_options_obj = options ?? new MondayItemBodyOptions();
            var query = "mutation{create_item($parameters){$body_options}}";
            var variables = new {
                parameters = params_obj.GetParameters(),
                body_options = body_options_obj.GetBody()
            };

            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};
            var response = await api.MutateAsync<MondayCreateItemResponse>(request);
            if(body_options_obj.id) {
                item.id = response.create_item.id;
            }
            return response.create_item;
        }

        public async Task<MondayBoard> GetMondayBoard(long boardId) {
            var paramOptions = new MondayBoardParameterOptions(new MondayBoard(){id = boardId});

            var columnValueOptions = new MondayColumnValueBodyOptions(){id=true, value=true, text=true};
            var itemOptions = new MondayItemBodyOptions(){name=true, column_values = columnValueOptions};
            var bodyOptions = new MondayBoardBodyOptions(){name=true, id=true, items=itemOptions};
            
            var boards = await GetMondayBoards(paramOptions, bodyOptions);
            if(boards.Count != 1) {
                throw new InvalidOperationException($"GetMondayBoard expects 1 board from Monday's API but found {boards.Count}");
            }
            boards[0]?.items?.ForEach(item => item.board_id = boardId);
            return boards[0];
        }

        public async Task<List<MondayBoard>> GetMondayBoards(MondayBoardParameterOptions paramOptionsObj = null, MondayBoardBodyOptions bodyOptionsObj = null) {
            paramOptionsObj = paramOptionsObj ?? new MondayBoardParameterOptions(null);
            bodyOptionsObj = bodyOptionsObj ?? new MondayBoardBodyOptions();
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