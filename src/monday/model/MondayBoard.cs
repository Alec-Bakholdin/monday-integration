using System;
using System.Collections.Generic;

namespace monday_integration.src.monday.model
{
    public class MondayBoardParameterOptions : MondayParameters<MondayBoard> {
        public Func<MondayBoard, long> ids {get; set;} = (board) => board.id;

        public MondayBoardParameterOptions(MondayBoard board) : base(board) {}
        public MondayBoardParameterOptions(long boardId) : base(new MondayBoard() {id = boardId}) {}
    }

    public class MondayBoardBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = false;
        public bool name {get; set;} = false;
        public bool workspace_id {get; set;} = false;
        
        public MondayItemBodyOptions items {get; set;} = null;
        public MondayColumnBodyOptions columns {get; set;} = null;
    }

    public class MondayBoard
    {
        public long id {get; set;}
        public string name {get; set;}
        public string workspace_id {get; set;}

        public List<MondayItem> items;
        public List<MondayColumn> columns;
    }

    public class MondayBoardList
    {
        public List<MondayBoard> boards;
    }
}