using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Nito.AsyncEx;

using Custom.Utility;
using AIMS360.Azure;

namespace AIMS360.Monday
{
    using static JsonUtility;
    public static class MondayUtility
    {
        // * * * * * * * * * * Private Variables * * * * * * * * * *
        private static string MondayBaseURL             = Environment.GetEnvironmentVariable("MondayBaseURL");
        private static string MondayApiToken            = Environment.GetEnvironmentVariable("MondayApiToken");
        private static string MondayWorkspaceId         = Environment.GetEnvironmentVariable("MondayWorkspaceId");
        private static GraphQLHttpClient graphQLClient  = InitGraphQLClient();
        private static int subitemCreationDelay         = 1000;

        private static string AimsBearerToken           = Environment.GetEnvironmentVariable("AimsBearerToken");
        private static string AimsBaseURL               = Environment.GetEnvironmentVariable("Aims360BaseURL");
        private static string StyleColorEndpoint        = Environment.GetEnvironmentVariable("StyleColorEndpoint");

        private static string CutTicketBoardName        = "Cut Tickets";


        // * * * * * * * * * * public Variables * * * * * * * * * *
        public static ILogger log = null;
        






        // * * * * * * * * * * Generic Sync Functions * * * * * * * * * *

        /**
         * <summary>
         * Generic function that synchronizes the board with
         * the name MondayBoardName with the data found in 
         * AimsEntries
         * </summary>
         * <param name="AimsEntries">The JArray containing all the entries in AIMS we are syncing with Monday</param>
         * <param name="MondayBoardName">The first part of the Monday board name and also how we look up the ids of all the boards in the table</param>
         * <param name="AimsAzureTable">Table where all id information  is stored so the client doesn't have to worry about maintaining naming conventions</param>
         * <param name="MondayBoardDescription">The second part of the generated board name (e.g. {name} - {description}). Only include for vendor boards.</param>
         * <param name="MondayBoardTemplateId">Id of the board we want to copy. Use this if you are dynamically generating vendor boards.</param>
         * <param name="GetItemInformation">Function that returns the names and column values of the ITEM as a string.</param>
         * <param name="GetSubitemInformation">Function that returns the names and column values of the SUBITEM as a string.</param>
         */
        public static async Task<bool> SyncBoardWithJArray(
            JArray      AimsEntries,
            string      MondayBoardName,
            AzureTable  AimsAzureTable,
            string      MondayBoardDescription,
            string      MondayBoardTemplateId,
            Func<JObject, MondayBoard, (string Name, string ColumnValues)>  GetItemInformation,
            Func<JObject, MondayBoard, (string Name, string ColumnValues)>  GetSubitemInformation,

            string BoardTemplateId = null) 
        {
            // retrieve the board
            var ItemBoard = await FindOrCreateBoard(MondayBoardName, AimsAzureTable, MondayBoardDescription, MondayBoardTemplateId);
            var SubitemBoard = await GetSubitemBoard(ItemBoard);

            foreach(JObject AimsEntry in AimsEntries)
            {
                await SyncItem(
                    AimsEntry,
                    ItemBoard,
                    MondayBoardName,
                    SubitemBoard,
                    AimsAzureTable,
                    GetItemInformation,
                    GetSubitemInformation
                );
            }

            return true;
        }





        /**
         * <summary>
         * Generic function that either finds the board in the table
         * and retrieves it or creates a new board and uses that.
         * If the templateId is null, then we know we are doing 
         * cut tickets, so that affects our decisions later.
         * </summary>
         * <param name="MondayBoardName">The first part of the Monday board name and also how we look up the ids of all the boards in the table</param>
         * <param name="AimsAzureTable">Table where all id information  is stored so the client doesn't have to worry about maintaining naming conventions</param>
         * <param name="MondayBoardDescription">The second part of the generated board name (e.g. {name} - {description}). Only include for vendor boards. Null by default.</param>
         * <param name="MondayBoardTemplateId">Id of the board we want to copy. Use this if you are dynamically generating vendor boards. Null by default.</param>
         */
        private static async Task<MondayBoard> FindOrCreateBoard(
            string MondayBoardName,
            AzureTable AimsAzureTable,
            string MondayBoardDescription = null,
            string MondayBoardTemplateId = null)
        {
            MondayBoard TargetBoard = null;

            // attempt to get board id
            var TargetBoardId = await AimsAzureTable.GetTableEntityValue(MondayBoardName);

            //if the board is present, get the board straight away
            if(TargetBoardId != null)
            {
                LogInformation($"Board {MondayBoardName} is registered in the table. Fetching board from API");
                TargetBoard = await GetMondayBoard(TargetBoardId);
            }

            // if the we didn't find the board, for some reason
            if(TargetBoard == null)
            {
                if(TargetBoardId == null) LogInformation($"Board {MondayBoardName} was not registered. Creating new board");
                else                      LogInformation($"Board {MondayBoardName} was registered in the table but not found in Monday. Creating new board");

                if(MondayBoardTemplateId == null)  // cut ticket board
                    TargetBoard = await RegisterCutTicketBoard(MondayBoardName, AimsAzureTable);
                else                               // vendor PO board
                    TargetBoard = await CreateAndRegisterNewBoard(MondayBoardName, MondayBoardDescription, AimsAzureTable, MondayBoardTemplateId);
            }

            LogInformation($"Successfully retrieved or created {MondayBoardName}");
            return TargetBoard;
        }


        /**
         * <summary>
         * If the template ID is null in the FindOrCreateBoard and
         * the cut ticket board is not yet registerd, we need to find
         * it by name. If the name doesn't exist (should be 'Cut Tickets' as of writing this),
         * then we have an exception and we can't sync cut tickets :(
         * </summary>
         * <param name="CutTicketBoardName">The name of the board we're looking up, in this case, it's going to be 'Cut Tickets' almost always.</param>
         * <param name="AimsAzureTable">The table we look up id information and where we store the id of the new board</param>
         */
        private static async Task<MondayBoard> RegisterCutTicketBoard(string CutTicketBoardName, AzureTable AimsAzureTable)
        {
            // fetch the first board with the correct name. If there are none, throws an exception
            var AllBoards = await GetAllMondayBoards();
            var TargetBoardList = AllBoards.Where(board => board.Name == CutTicketBoardName).ToList();
            if(TargetBoardList.Count == 0)
                throw new Exception($"There are no boards with name {CutTicketBoardName}");
            if(TargetBoardList.Count != 1)
                LogWarning($"There are multiple boards with the name {CutTicketBoardName}. Selecting first one: {TargetBoardList[0].Id}");

            // fetch the full board from Monday and update Azure tables.
            var CutTicketBoard = await GetMondayBoard(TargetBoardList[0].Id);
            
            // add/update the board's id in the table
            var addEntityResponse =  await AimsAzureTable.AddEntitySimple(CutTicketBoardName, TargetBoardList[0].Id);
            if(addEntityResponse == null)
                await AimsAzureTable.UpdateEntitySimple(CutTicketBoardName, TargetBoardList[0].Id);
            
            return CutTicketBoard;
        }


        /**
         * <summary>
         * Creates and registers a new board with the given name.
         * Registers using just the name, but the description is
         * appended to the Monday Board's name
         * </summary>
         * <param name="MondayBoardName">The first part of the Monday board name and also how we look up the ids of all the boards in the table</param>
         * <param name="MondayBoardDescription">The second part of the generated board name (e.g. {name} - {description}). Only include for vendor boards. Null by default.</param>
         * <param name="AimsAzureTable">Table where all id information  is stored so the client doesn't have to worry about maintaining naming conventions</param>
         * <param name="MondayBoardTemplateId">Id of the board we want to copy. Use this if you are dynamically generating vendor boards. Null by default.</param>
         */
        private static async Task<MondayBoard> CreateAndRegisterNewBoard(string MondayBoardName, string MondayBoardDescription, AzureTable AimsAzureTable, string MondayBoardTemplateId)
        {
            
            // create the new board
            var NewBoard = await DuplicateMondayBoard(MondayBoardTemplateId, $"{MondayBoardName} - {MondayBoardDescription}",  MondayWorkspaceId);

            // update azure table
            var addEntityResponse =  await AimsAzureTable.AddEntitySimple(MondayBoardName, NewBoard.Id);
            if(addEntityResponse == null)
                await AimsAzureTable.UpdateEntitySimple(MondayBoardName, NewBoard.Id);
            
            return NewBoard;
        }


        /**
         * <summary>
         *  Gets all the boards, finds the one whose name is formatted
         *  as 'Subitems of {parentName}' and get all the necessary
         *  fields of that subitem board (column titles, id, name).
         *  Because it's a subitem board, we don't need the items
         * </summary>
         * <param name="ParentBoard">The parent board whose subitem board we need</param>
         */
        private static async Task<MondayBoard> GetSubitemBoard(MondayBoard ParentBoard)
        {
            // get all the boards and find the one that's the format 'Subitems of {parent}'
            var AllBoards = await GetAllMondayBoards();
            var SubitemBoards = AllBoards.Where(board => board.Name == $"Subitems of {ParentBoard.Name}").ToList();
            if(SubitemBoards.Count > 1)
                throw new Exception($"Duplicate boards with name {ParentBoard.Name}");
            
            // obviously, if there's only one, we only have to fetch the first board
            var SubitemBoardId = SubitemBoards[0].Id;
            var SubitemBoard = await GetMondayBoard(SubitemBoardId, "id, name, columns{id, title}");
            return SubitemBoard;
        }


        /**
         * <summary>
         * Creates an item in the given board if necessary. Gets the values for the
         * item using the function passed in the 5th and 6th parameters.
         * </summary>
         * <param name="AzureEntry">The vendor PO or cut ticket object that contains all the item and subitem information. Subitems should be stored as a child with key 'members'</param>
         * <param name="ItemBoard">The object representing the board the item gets created into.</param>
         * <param name="SubitemBoard">The object representing the board the subitems gets created into.</param>
         * <param name="MondayBoardName">The first part of the name used for the monday board. (Recall {name} - {description} format) Used to look up the item id in the azure table</param>
         * <param name="GetItemInformation">Function that returns the names and column values of the ITEM as a string.</param>
         * <param name="GetSubitemInformation">Function that returns the names and column values of the SUBITEM as a string.</param>
         */
        private static async Task<MondayItem> SyncItem(
            JObject     AzureEntry,
            MondayBoard ItemBoard,
            string MondayBoardName,
            MondayBoard SubitemBoard,
            AzureTable AimsAzureTable,

            Func<JObject, MondayBoard, (string Name, string ColumnValues)>  GetItemInformation,
            Func<JObject, MondayBoard, (string Name, string ColumnValues)>  GetSubitemInformation)
        {
            // get item name and then Id
            var ItemInformation = GetItemInformation(AzureEntry, ItemBoard);
            if(ItemInformation.Name == null)
                throw new Exception($"Could not parse item name for {JsonConvert.SerializeObject(AzureEntry)}");
            var ItemId = await AimsAzureTable.GetTableEntityValue(ItemInformation.Name, MondayBoardName);
            
            // if item id was not found or item is no longer in the board, we create the item from scratch
            if(ItemId == null || !ItemBoard.ContainsItem(ItemId))
            {
                if(ItemId == null)  LogInformation($"Creating new item {ItemInformation.Name}");
                else                LogInformation($"Item {ItemId} is registered but is not present in Monday. Creating new item to replace it");

                // creates the item and its subitems
                var CreatedItem = await CreateMondayItem(ItemInformation.Name, ItemBoard.Id, ItemInformation.ColumnValues);
                CreateSubitems(CreatedItem, SubitemBoard, (JArray)AzureEntry["members"], GetSubitemInformation);

                // update azure
                LogInformation("$Created item. Now updating Azure Table");
                if(ItemId == null) await AimsAzureTable.AddEntitySimple(ItemInformation.Name, CreatedItem.Id, MondayBoardName);
                else               await AimsAzureTable.UpdateEntitySimple(ItemInformation.Name, CreatedItem.Id, null, MondayBoardName);

            }

            LogInformation($"Successfully synced item: '{ItemInformation.Name}'");
            return null;
        }


        /**
         * <summary>
         * Creates an item in the given board. Gets the values for the
         * item using the function passed in the 5th and 6th parameters.
         * </summary>
         * <param name="AzureEntry">The vendor PO or cut ticket object that contains all the item and subitem information. Subitems should be stored as a child with key 'members'</param>
         * <param name="ItemBoard">The object representing the board the item gets created into.</param>
         * <param name="SubitemBoard">The object representing the board the subitems gets created into.</param>
         * <param name="GetItemInformation">Function that returns the names and column values of the ITEM as a string.</param>
         * <param name="GetSubitemInformation">Function that returns the names and column values of the SUBITEM as a string.</param>
         */
        private static async Task<MondayItem> CreateItemWithValues(
            JObject     AzureEntry,
            MondayBoard ItemBoard,
            MondayBoard SubitemBoard,

            Func<JObject, MondayBoard, (string name, string colVals)>  GetItemInformation,
            Func<JObject, MondayBoard, (string name, string colVals)>  GetSubitemInformation)
        {
            // item
            var ItemInformation = GetItemInformation(AzureEntry, ItemBoard);
            var CreatedItem = await CreateMondayItem(ItemInformation.name, ItemBoard.Id, ItemInformation.colVals);

            // subitems
            CreateSubitems(CreatedItem, SubitemBoard, (JArray)AzureEntry["members"], GetSubitemInformation);

            return CreatedItem;
        }
























        // * * * * * * * * * * Item Information Functions * * * * * * * * * *



        /**
         * Get and set the monday.com item values from the PO and also
         * sets the default status for receving and transit and all that.
         */
        public static (string, string) Item_GetPOColumnValuesAsStr(JObject PO, MondayBoard Board)
        {
            var MondayColValues = new JObject();

            // string values
            AssignColumnValue(PO, MondayColValues, Board, "PurchaseOrderNo", "Vendor PO #", typeof(string));
            AssignColumnValue(PO, MondayColValues, Board, "XRef"           , "X-Ref"      , typeof(string));

            var POTypeId = Board.GetColumnIdByName("PO Type");
            if((string)PO["POType"] == "S")
                MondayColValues[POTypeId] = "Style";
            else
                MondayColValues[POTypeId] = "Fabric";

            // date values
            AssignColumnValue(PO, MondayColValues, Board, "Start Date", "Created Date", typeof(DateTime));
            AssignColumnValue(PO, MondayColValues, Board, "XFactory", "PO X Factory Date", typeof(DateTime));
            AssignColumnValue(PO, MondayColValues, Board, "PO Cancel", "PO Due Date", typeof(DateTime));

            var ItemName = "PO # " + (string)PO["PurchaseOrderNo"];

            return (ItemName, JsonConvert.SerializeObject(MondayColValues));
        }


        /**
         * Using the groups in board, set the values of the columns.
         * The column names *should* be set in stone, so hopefully
         * this doesn't break I:
         */
        public static (string, string) Subitem_GetStylePOColumnValuesAsStr(JObject LineItem, MondayBoard Board)
        {
            var MondayColValues = new JObject();

            // get the style and color descriptions
            var styleColorDescription = $"{LineItem["Style Description"]} - {LineItem["Color Description"]}"; 
            MondayColValues[Board.GetColumnIdByName("Style + Color Description")] = styleColorDescription;

            // string values
            AssignColumnValue(LineItem, MondayColValues, Board, "Style", "Style", typeof(string));
            AssignColumnValue(LineItem, MondayColValues, Board, "Color", "Color", typeof(string));
            AssignColumnValue(LineItem, MondayColValues, Board, "Size" , "Size" , typeof(string));

            // int values
            AssignColumnValue(LineItem, MondayColValues, Board, "Order Qty"         , "Ordered" , typeof(Int32));
            AssignColumnValue(LineItem, MondayColValues, Board, "Total Received Qty", "Received", typeof(Int32));

            // date values
            AssignColumnValue(LineItem, MondayColValues, Board, "PO Cancel", "Due Date"       , typeof(DateTime));
            AssignColumnValue(LineItem, MondayColValues, Board, "XFactory" , "Ex-Factory Date", typeof(DateTime));
            
            var SubitemName = $"{LineItem["Style"]}-{LineItem["Color"]}-{LineItem["Size"]}";
            if((string)LineItem["Order Qty"] == "0")
                SubitemName = null;

            return (SubitemName, JsonConvert.SerializeObject(MondayColValues));
        }


        /**
         * Gets the name and the column values for a purchase order
         * that's a fabric (material)
         */
        public static (string, string) Subitem_GetMaterialPOColumnValuesAsStr(JObject LineItem, MondayBoard Board)
        {
            var MondayColValues = new JObject();

            // get the style and color descriptions
            var styleColorDescription = $"{LineItem["Material Description"]} - {LineItem["Color Description"]}"; 
            MondayColValues[Board.GetColumnIdByName("Style + Color Description")] = styleColorDescription;

            // string values
            AssignColumnValue(LineItem, MondayColValues, Board, "Material Item", "Style", typeof(string));
            AssignColumnValue(LineItem, MondayColValues, Board, "Color", "Color", typeof(string));

            // int values
            AssignColumnValue(LineItem, MondayColValues, Board, "Order Qty"         , "Ordered" , typeof(Int32));
            //AssignColumnValue(LineItem, MondayColValues, Board, "Total Received Qty", "Received", typeof(Int32));

            // date values
            AssignColumnValue(LineItem, MondayColValues, Board, "PO Cancel", "Due Date"       , typeof(DateTime));
            AssignColumnValue(LineItem, MondayColValues, Board, "XFactory" , "Ex-Factory Date", typeof(DateTime));
            
            var SubitemName = $"{LineItem["Material Item"]}-{LineItem["Color"]}";
            if((string)LineItem["Order Qty"] == "0")
                SubitemName = null;

            return (SubitemName, JsonConvert.SerializeObject(MondayColValues));
        }


        /**
         * Self-explanatory, converts the group values into a string
         * so we can pass it to the API.
         */
        public static (string, string) Item_GetCutTicketColumnValuesAsStr(JObject CutTicketGroup, MondayBoard Board)
        {
            var MondayColumnValues = new JObject();
            // string values
            AssignColumnValue(CutTicketGroup, MondayColumnValues, Board, "Ticket", "Ticket #", typeof(string));
            AssignColumnValue(CutTicketGroup, MondayColumnValues, Board, "Status", "Ticket Status", typeof(string));

            // date values
            AssignColumnValue(CutTicketGroup, MondayColumnValues, Board, "Entered", "Created Date", typeof(DateTime));
            AssignColumnValue(CutTicketGroup, MondayColumnValues, Board, "Complete", "Due Date", typeof(DateTime));

            var ItemName = "Ticket # " + (string)CutTicketGroup["Ticket"];

            return (ItemName, JsonConvert.SerializeObject(MondayColumnValues));
        }

        private static object getStyleObject = new object();
        /**
         * Again, converts the item values into a string so we can pass
         * it to the API. This time, it's the subitem, though
         */
        public static (string, string) Subitem_GetCutTicketColumnValuesAsStr(JObject CutTicketMember, MondayBoard Board)
        {
            var MondayColValues = new JObject();

            // get style and color descriptions and add them to subitem column values
            // locking is necessary because otherwise we lose out on caching benefits if
            // 2 related style/color pairs call the api at once
            lock(getStyleObject)
            {
                var styleObject = GetStyleObject((string)CutTicketMember["Style"], (string)CutTicketMember["Color"]);
                var styleColorDescription = $"{styleObject["description"]} - {styleObject["colorDescription"]}";
                MondayColValues[Board.GetColumnIdByName("Style + Color Description")] = styleColorDescription;
            }

            // string values
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Style", "Style", typeof(string));
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Color", "Color", typeof(string));
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Size" , "Size" , typeof(string));

            // integer values
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Budget" , "Ordered" , typeof(Int32));
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Received" , "Received" , typeof(Int32));

            // date values
            AssignColumnValue(CutTicketMember, MondayColValues, Board, "Complete" , "Due Date" , typeof(DateTime));

            var SubitemName = $"{CutTicketMember["Style"]}-{CutTicketMember["Color"]}-{CutTicketMember["Size"]}";
            if((int)CutTicketMember["Budget"] == 0)
                SubitemName = null;
                

            return (SubitemName, JsonConvert.SerializeObject(MondayColValues));
        }



        



































        // * * * * * * * * * * Api Calls * * * * * * * * * *


        /**
         * Get a board with the specific ID. By default, returns the
         * id and name of the board, as well as the items with their ids
         * attached
         */
        public static async Task<MondayBoard> GetMondayBoard(string id, string responseFields = "id, name, items{id}, columns{id, title}")
        {
            // set up query to get boards and the items underneath
            var getBoardQuery = new GraphQLRequest(){
                Query = $"query{{boards(ids: {id}){{{responseFields}}}}}"
            };

            // make request and make sure there are no errors
            var graphQLResponse = await SendGraphQLRequest<MondayBoardCollection>(getBoardQuery);
            if(graphQLResponse.Errors != null)
            {
                LogError($"Error retrieving board: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                return null;
            }

            // check there is only one board
            var mondayBoards = graphQLResponse.Data.Boards;
            if(mondayBoards.Count < 1)
                return null;
            else if(mondayBoards.Count > 1)
                throw new Exception($"There were duplicate boards with id {id}?????!!!!");
            
            return mondayBoards[0];
        }


        /**
         * Gets all monday boards, with their id and name
         * This is generally going to be done only on the first
         * call of this Azure Function, as we will be
         * putting the Cut Tickets board in this way.
         */
        public static async Task<List<MondayBoard>> GetAllMondayBoards(string responseFields = "id, name")
        {
            // create query
            var getAllBoardsQuery = new GraphQLRequest(){
                Query = $"query{{boards{{ {responseFields} }} }}"
            };

            // make request and make sure there are no errors
            var graphQLResponse = await SendGraphQLRequest<MondayBoardCollection>(getAllBoardsQuery);
            if(graphQLResponse.Errors != null)
            {
                LogError($"Error retrieving board: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                return null;
            }

            return graphQLResponse.Data.Boards;
        }


        /**
         * Creates a board with the given name, optionally using the templateId
         * passed in the parameters. Returns a MondayBoard object with the
         * fields in responseFields
         */
        public static async Task<MondayBoard> CreateMondayBoard(string boardName, string templateId = null, string workspaceId = null, string responseFields = "id, name, items{id}, columns{title, id}", string boardKind = "public")
        {
            while(true) {

                // determine whether or not to use template id in the creation of the board
                var useTemplateString = templateId == null ? "" : $", template_id: {templateId}";

                // create request for mutation
                var createBoardQuery = new GraphQLRequest(){
                    // set mutation
                    Query = $"mutation{{create_board(board_name: \"{boardName}\", board_kind: {boardKind}{useTemplateString}) {{ {responseFields} }} }}"
                };

                // make request and make sure there are no errors
                var graphQLResponse = await SendGraphQLRequest<CreateMondayBoardResponse>(createBoardQuery);
                if(graphQLResponse.Errors != null)
                {
                    string errors = GraphQLErrorsToString(graphQLResponse.Errors);
                    string targetStr = "out of 1000000 reset in ";
                    int targetPosition = errors.IndexOf(targetStr);  
                    if(targetPosition >= 0) {
                        Thread.Sleep(10000);
                        continue;
                    }
                    LogError($"Error creating board: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                    return null;
                }

                // get and return the board in the response
                return graphQLResponse.Data.Board;
            }
        }


        /**
         * Duplicate a board with the specififed duplicate type.
         * Returns the resulting duplicate board.
         * Other valid values of DuplicateType are duplicate_board_with_pulses and duplicate_board_with_pulses_and_updates
         * If you want to copy items, use CreateMondayBoard with a template
         */
        public static async Task<MondayBoard> DuplicateMondayBoard(string OldBoardID, string NewBoardName, string workspaceId, string ResponseFields = "board{id, name, items{id},columns{title, id}}", string DuplicateType = "duplicate_board_with_structure")
        {
            while(true) {
                // create request for mutation
                var createBoardQuery = new GraphQLRequest(){
                    // set mutation
                    Query = $"mutation{{duplicate_board(board_id: {OldBoardID}, board_name: \"{NewBoardName}\", duplicate_type: {DuplicateType}, workspace_id: {workspaceId}) {{ {ResponseFields} }} }}"
                };

                // make request and make sure there are no errors
                var graphQLResponse = await SendGraphQLRequest<DuplicateMondayBoardResponse>(createBoardQuery);
                if(graphQLResponse.Errors != null)
                {
                    string errors = GraphQLErrorsToString(graphQLResponse.Errors);
                    string targetStr = "out of 1000000 reset in ";
                    int targetPosition = errors.IndexOf(targetStr);  
                    if(targetPosition >= 0) {
                        Thread.Sleep(10000);
                        continue;
                    }
                    LogError($"Error duplicating board: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                    return null;
                }

                // get and return the board in the response
                return graphQLResponse.Data.Nested.Board;
            }
        }

        /**
         * Creates an item in a board with the given parameters. Returns an object
         * representing the created card with the fields listed in responseFields
         */
        public static async Task<MondayItem> CreateMondayItem(string itemName, string boardId, string columnValues = null, string responseFields = "id")
        {
            while(true) {

                // determine whether to use group Id or just use boardId and itemName
                var useColumnValues = columnValues == null ? "" : $", column_values: \"{columnValues.Replace(@"""", @"\""")}\"";
                
                // create mutation request
                var createItemRequest = new GraphQLRequest(){
                    Query = $"mutation{{ create_item(item_name: \"{itemName}\", board_id: {boardId}{useColumnValues}) {{ {responseFields} }} }}"
                };

                // send request
                var graphQLResponse = await SendGraphQLRequest<CreateMondayItemResponse>(createItemRequest);
                if(graphQLResponse.Errors != null)
                {
                    string errors = GraphQLErrorsToString(graphQLResponse.Errors);
                    string targetStr = "out of 1000000 reset in ";
                    int targetPosition = errors.IndexOf(targetStr);  
                    if(targetPosition >= 0) {
                        Thread.Sleep(10000);
                        continue;
                    }
                    LogError($"Error creating item: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                    return null;
                }

                // find and return item
                return graphQLResponse.Data.Item;
            }
        }

        /**
         * Creates a subitem of the parent whose ID is defined in parentItemId with the name subItemName
         */
        public static async Task<MondayItem> CreateMondaySubitem(string subitemName, string parentItemId, string columnValues, string responseFields = "id")
        {
            while(true) {
                var columnValuesStr = (columnValues == null ? "" : $", column_values: \"{columnValues.Replace(@"""", @"\""")}\"");
                // create query. All fields are mandatory, so we don't have to do anything before this
                var createSubitemRequest =  new GraphQLRequest(){
                    Query = $"mutation{{ create_subitem(item_name: \"{subitemName}\", parent_item_id: {parentItemId}{columnValuesStr}){{ {responseFields} }} }}"
                };

                // call API
                var graphQLResponse = await SendGraphQLRequest<CreateMondaySubitemResponse>(createSubitemRequest);
                if(graphQLResponse.Errors != null)
                {
                    string errors = GraphQLErrorsToString(graphQLResponse.Errors);
                    string targetStr = "out of 1000000 reset in ";
                    int targetPosition = errors.IndexOf(targetStr);  
                    if(targetPosition >= 0) {
                        Thread.Sleep(10000);
                        continue;
                    }
                    LogError($"Error creating subitem for {parentItemId}: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                    return null;
                } else if(graphQLResponse.Data.Item == null) {
                    LogError("Something bad happened here");
                } else {
                    // find and return item
                    return graphQLResponse.Data.Item;
                }
            }

        }


        /**
         * Updates the item whose ID is passed with
         * the column values, json formatted, passed in  the
         * columnValues string
         */
        public static async Task<MondayItem> UpdateMondayItemColumnValues(string boardId, string itemId, string columnValues, string responseFields = "id")
        {
            columnValues = columnValues.Replace(@"""", @"\""");
            var updateItemColumnValuesRequest = new GraphQLRequest(){
                Query = $"mutation{{ change_multiple_column_values(board_id: {boardId}, item_id: {itemId}, column_values: \"{columnValues}\"){{ {responseFields} }} }}"
            };
            
            // call API
            var graphQLResponse = await SendGraphQLRequest<ChangeMultipleColumnValuesResponse>(updateItemColumnValuesRequest);
            if(graphQLResponse.Errors != null)
            {
                LogError($"Error updating column values for {itemId}: {GraphQLErrorsToString(graphQLResponse.Errors)}");
                return null;
            }

            return graphQLResponse.Data.Item;
        }































        // * * * * * * * * * * Generalized Syncing Functions * * * * * * * * * *


        /**
         * <summary>
         * Generic function for creating subitems for a parent item
         * The subitem members are listed in the Members JArray, and the column
         * values are generated by passing each member to the GetColumnValues function.
         * The function returns the SubitemBoard value after execution
         * </summary>
         * <param name="ParentItem">Parent under which we create all the subitems</param>
         * <param name="SubitemBoard">Board which the subitems will be created under.</param>
         * <param name="Members">JArray which contains information about the subitems. Each JObject child will be passed to GetSubitemInformation to get column values.</param>
         * <param name="GetSubitemInformation">Function which takes the member and the subitem board and returns the name to call the subitem and the column values in a tuple.</param>
         * <returns>The board the subitems were created on, with no items.</returns>
         */
        private static MondayBoard CreateSubitems(
            MondayItem ParentItem,
            MondayBoard SubitemBoard,
            JArray Members,
            Func<JObject, MondayBoard, (string name, string colVals)> GetSubitemInformation)


        {
            int counter = 1;
            var subitemTaskList = new List<Task<MondayItem>>();

            foreach(JObject Member in Members)
            {   
                // get subitem information and skip if quantity is 0 (return from getinfo is null)
                var SubitemInformation = GetSubitemInformation(Member, SubitemBoard);
                if(SubitemInformation.name == null)
                {
                    LogInformation($"Skipping subitem {counter++} out of {Members.Count}");
                    continue;
                }

                // create the task to create the subitem so we can run in parallel
                subitemTaskList.Add(CreateMondaySubitemTask(ParentItem, SubitemBoard, SubitemInformation.name, SubitemInformation.colVals, counter++, Members.Count));
                Thread.Sleep(subitemCreationDelay);
            }

            Task.WaitAll(subitemTaskList.ToArray());

            return SubitemBoard;
        }


        /**
         * Perform the basic operation of getting the subitem column values
         * and creating the subitem itself. Prints success message on completion
         */
        private static async Task<MondayItem> CreateMondaySubitemTask(
            MondayItem ParentItem,
            MondayBoard SubitemBoard,
            string SubitemName,
            string ColumnValues,
            int NumCompleted,
            int SubitemTotalCount)


        {
            var subitem = await CreateMondaySubitem(SubitemName, ParentItem.Id, ColumnValues);

            if(subitem != null)
                LogInformation($"Created subitem {NumCompleted} out of {SubitemTotalCount}");
            else
                LogWarning($"Something is funky about {NumCompleted} out of {SubitemTotalCount}. The response was null?");

            return subitem;
        }




























        // * * * * * * * * * * Generic Monday API functions * * * * * * * * * *


        /**
         * Makes the most basic request to the graphQL client specified in
         * in the parameters using the request. Returns the response we get
         */
        public async static Task<GraphQLResponse<T>> SendGraphQLRequest<T>(GraphQLRequest request)
        {
            int maxAttempts = 5;
            for(int i = 1; i <= maxAttempts; i++)
            {
                
                var response = (GraphQLHttpResponse<T>)await graphQLClient.SendQueryAsync<T>(request);
                switch((int)response.StatusCode)
                {
                    case 200: // we either have a well-defined error or we're done. We do need to handle query overload, though
                        // complexity overload
                        if(response.Errors != null && response.Errors[0].Message.StartsWith("Query has complexity"))
                        {
                            // huge backoff
                            LogError($"We've reached complexity maximum. Setting thread to sleep for 10 seconds and increasing subitem creation delay");
                            subitemCreationDelay = 4000;
                            Thread.Sleep(10000);
                            i--;
                            continue;
                        }
                        else
                            subitemCreationDelay = (subitemCreationDelay <= 200 ? subitemCreationDelay : subitemCreationDelay - 200);
                        return response; 
                    // log messages describe other cases
                    case 400: 
                        LogError($"Received error code 400. Ensure your query string is passed with the 'query' key. Ensure your request is a POST with JSON body. Ensure your query does not contain unterminated strings. Query: {request.Query}");
                        return null;
                    case 500:
                        LogError($"Received error code 500. Check the format of any JSON strings in your query. If you are updating a column, check that you are using the right data structure for each column value. Query: {request.Query}");
                        return null;
                    case 401:
                        LogError($"Received error code 401. Api Key must not be working. Key: {graphQLClient.HttpClient.DefaultRequestHeaders.ToString()}");
                        return null;
                    default:
                        break;
                }

                // on unexpected error (e.g. 403 Forbidden)
                if(i != maxAttempts && log != null) log.LogError($"Got back status code {(int)response.StatusCode} on API request. Attempting request {i + 1} out of {maxAttempts}...");

                // sleep by 1.5^(i-1) seconds, so anywhere between 1 and somewhere around 4 seconds?
                Thread.Sleep(1000*(int)Math.Pow(1.5, i - 1));
            }
            
            LogError($"Retry failed too many times. Reached maximum number of requests.");
            return null;

        }


        /**
         * Returns a string representing the errors retrieved from the API call,
         * if any
         */
        public static string GraphQLErrorsToString(ICollection<GraphQLError> errors)
        {
            List<string> errorMessages = errors.Select(error => error.Message).ToList();
            string output = "{" + String.Join(", ", errorMessages) + "}";

            return output;
        }



































        // * * * * * * * * * * AIMS Api Functions * * * * * * * * * *


        private static Dictionary<string, string> StyleHistory = new Dictionary<string, string>();
        private static Dictionary<string, string> ColorHistory = new Dictionary<string, string>();
        /**
         * Fetches the JObject for the style with the corresponding
         * style and color codes. As of setting up this function, the endpoint
         * has a $select odata clause that only shows description and colorDescription
         */
        private static JObject GetStyleObject(string style, string color)
        {
            if(StyleHistory.ContainsKey(style) && ColorHistory.ContainsKey(color))
            {
                var newObject = new JObject();
                newObject.Add("description", StyleHistory[style]);
                newObject.Add("colorDescription", ColorHistory[color]);
                return newObject;
            }

            // format the url
            var url = StyleColorEndpoint
                .Replace("{{BaseURL}}", AimsBaseURL)
                .Replace("{{Style}}", style)
                .Replace("{{Color}}", color);

            // make request
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", AimsBearerToken);

            // validate success
            urlForAims = url;
            requestForAims = request;
            var response = AsyncContext.Run(CallAimsApiAsyncContext);
            if(!response.IsSuccessful)
                throw new Exception($"Error retrieving style information: {response.Content}");

            // convert and return
            var responseJson = (JObject)JsonConvert.DeserializeObject(response.Content);
            var valueArray = (JArray)responseJson["value"];
            var styleObject = (JObject)valueArray[0];
            StyleHistory.TryAdd(style, (string)styleObject["description"]);
            ColorHistory.TryAdd(color, (string)styleObject["colorDescription"]);
            return styleObject;
        }

        private static string urlForAims = null;
        private static RestRequest requestForAims = null;
        /**
         * <summary>
         * Calls the CallAimsApi function using the two static variables,
         *  - private static string urlForAims;
         *  - private static RestRequest requestForAims;
         * THESE VARIABLES MUST BE SET, otherwise there's an exception.
         * This just allows non-async functions to call the AIMS api using
         * the function I built for it asynchronously.
         * </summary>
         */
        private static async Task<IRestResponse> CallAimsApiAsyncContext()
        {
            if(urlForAims == null || requestForAims == null)
                throw new Exception("Please assign both urlForAims and requestForAims variables");
            
            var response = await CallAimsApi(urlForAims, requestForAims);
            
            return response;
        }



        /**
        * Simplest Api request, gets the json from the get request and
        * converts it to JObject using JsonConvert.DeserializeObject.
        * Implements exponential backoff. Does not do error checking beyond
        * the weird 400 request.
        */
        private static async Task<IRestResponse> CallAimsApi(string url, RestRequest request)
        {
            // initialize everything
            var client = new RestClient(url);

            // call the request
            IRestResponse response = null;
            int counter = 0;
            do{
                // exponential backoff, after the first iteration
                if(counter > 0)
                    Thread.Sleep((int)Math.Pow(2, counter - 1) * 1000);

                // get api response
                response = await client.ExecuteAsync(request);

                // handle the case where the request fails due to overloaded servers
                if((int)response.StatusCode == 400)
                {
                    var responseObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                    if(!responseObj.ContainsKey("error"))
                    {
                        //log.LogError(response.Content); this was to handle the inconsistent error message, but we now handle that elsewhere
                        break;
                    }
                    if(responseObj["error"]["code"].ToString() != "Request_ProcessingFailed")
                        break;
                }
                // if some well-defined error or OK, we exit out of the loop
                else
                    break;

            }while(counter++ < 5);

            return response;
        }
    






















        // * * * * * * * * * * Helper Functions * * * * * * * * * *
        private static void LogError(string data)
        {
            if(log == null) return;
            lock(log)
            {
                log.LogError(data);
            }
        }

        private static void LogWarning(string data)
        {
            if(log == null) return;
            lock(log)
            {
                log.LogWarning(data);
            }
        }

        private static void LogInformation(string data)
        {
            if(log == null) return;
            lock(log)
            {
                log.LogInformation(data);
            }
        }


        /**
         * Gets the value from AIMS, converts it into the correct format,
         * and inserts it into the MondayColValues object.
         */
        public static JObject AssignColumnValue(JObject AimsValues, JObject MondayColValues, MondayBoard Board, string AimsFieldName, string MondayColumnName, Type DataType)
        {
            // fetch data from AIMS object and get column ID for monday
            var AimsData = (string)AimsValues[AimsFieldName];
            var MondayColumnId = Board.GetColumnIdByName(MondayColumnName);
            if(MondayColumnId == null)
                throw new Exception($"{MondayColumnName} is not a valid column name");

            // convert data and insert into the object
            if      (DataType == typeof(DateTime) && AimsData != "")
            {
                var MondayValue = DateTime.Parse(AimsData).ToString("yyyy-MM-dd HH:mm:ss");
                MondayColValues[MondayColumnId] = MondayValue;
            }
            else if (DataType == typeof(string) && AimsFieldName == "Ship To Address")
            {
                var MondayValue = GetShipToAddress(AimsValues);
                MondayColValues[MondayColumnId] = MondayValue;
            }
            else if (DataType == typeof(string))
            {
                var MondayValue = AimsData;
                MondayColValues[MondayColumnId] = MondayValue;
            }
            else if (DataType == typeof(Int32))
            {
                var MondayValue = Int32.Parse(AimsData);
                MondayColValues[MondayColumnId] = MondayValue;
            }

            return MondayColValues;
        }


        /**
         * <summary>
         * Converts the data in the 'Ship To Address' Field in the AIMS response
         * into an address that we can put into Monday
         * </summary>
         */
        private static string GetShipToAddress(JObject AimsValues)
        {

            return "";
        }















        // * * * * * * * * * * Initialization function(s) * * * * * * * * * *
        /**
         * Initializes the http client with correct authorization
         * and everything
         */
        private static GraphQLHttpClient InitGraphQLClient()
        {
            // init client
            var client = new GraphQLHttpClient(MondayBaseURL, new NewtonsoftJsonSerializer());

            // add bearer token
            client.HttpClient.DefaultRequestHeaders.Add("Authorization", MondayApiToken);

            return client;
        }
    }
}