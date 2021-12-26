namespace monday_integration.src.monday.model
{
    public class MondayColumnBodyOptions : MondayBodyOptions {
        public bool id {get; set;} = true;
        public bool title {get; set;} = true;
        public bool type {get; set;} = true;
    }

    public class MondayColumn
    {
        public string id {get; set;}
        public string title {get; set;}
        public string type {get; set;}
    }
}