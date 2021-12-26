using System.Collections.Generic;

namespace monday_integration.src.monday.model
{
    public class MondayBoardBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = false;
        public bool name {get; set;} = false;
        
        public MondayItemBodyOptions items {get; set;} = null;
        public MondayColumnBodyOptions columns {get; set;} = null;
    }

    public class MondayBoard
    {
        public string id {get; set;}
        public string name {get; set;}

        public List<MondayItem> items;
        public List<MondayColumn> columns;
    }

    public class MondayBoardList
    {
        public List<MondayBoard> boards;
    }
}