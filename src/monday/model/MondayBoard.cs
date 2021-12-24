using System.Collections.Generic;

namespace monday_integration.src.monday.model
{
    public enum MondayBoardBodyOption {
        id,
        name
    }

    public class MondayBoard
    {
        public string id {get; set;}
        public string name {get; set;}

        public List<MondayItem> items;
    }

    public class MondayBoardList
    {
        public List<MondayBoard> boards;
    }
}