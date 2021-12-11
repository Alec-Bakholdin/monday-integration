using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIMS360.Monday
{
    public class MondayBoard
    {
        // * * * * * * * * * * Properties * * * * * * * * * *
        [JsonProperty("id")]
        public string Id {get; set;}
        [JsonProperty("name")]
        public string Name {get; set;}
        [JsonProperty("board_kind")]
        public string BoardKind {get; set;}


        // * * * * * * * * * * Children * * * * * * * * * *
        [JsonProperty("groups")]
        public List<MondayGroup> Groups {get; set;}
        [JsonProperty("items")]
        public List<MondayItem> Items {get; set;}   
        [JsonProperty("columns")]
        public List<MondayColumn> Columns {get; set;}


        // * * * * * * * * * * Custom Properties * * * * * * * * * *
        public string subitemBoardId = null;

        /**
         * <summary>
         * Iterate over Groups and return the ID
         *  of the group with the given name.
         * Returns null if any target value is null
         * or if the name doesn't exist.
         * </summary>
         * <param name="ColumnName">The name of the column to search for</param>
         * <returns>Returns the id of the column in Monday or null if the column isn't found</param>
         */
        public string GetColumnIdByName(string ColumnName)
        {
            if(Columns == null)
                return null;
            foreach(MondayColumn column in Columns)
                if(column.Name != null && column.Name.ToLower() == ColumnName.ToLower())
                    return column.Id;
            return null;
        }

        /**
         * <summary>
         * Does Linq magic to determine if the board
         * contains an item with the specified id
         * </summary>
         * <param name="ItemId">The ID of the item to search for</param>
         * <returns>True if the item is found and false otherwise</returns>
         */
        public bool ContainsItem(string ItemId)
        {
            if(Items == null)
                return false;
            var ItemMatches = Items.Where(item => item.Id == ItemId).ToList();
            
            return ItemMatches.Count > 0;
        }
    }

    public class MondayBoardCollection
    {
        [JsonProperty("boards")]
        public List<MondayBoard> Boards {get; set;}
    }

    public class CreateMondayBoardResponse
    {
        [JsonProperty("create_board")]
        public MondayBoard Board {get; set;}
    }

    public class DuplicateMondayBoardResponse
    {
        [JsonProperty("duplicate_board")]
        public DuplicateMondayBoardResponseNested Nested {get; set;}
    }
    public class DuplicateMondayBoardResponseNested
    {
        [JsonProperty("board")]
        public MondayBoard Board {get; set;}
    }
}