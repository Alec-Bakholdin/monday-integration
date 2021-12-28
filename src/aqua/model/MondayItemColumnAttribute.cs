using System;

namespace monday_integration.src.aqua.model
{
    internal class MondayItemColumnAttribute : Attribute
    {
        public string columnId {get; private set;}

        public MondayItemColumnAttribute(string columnId) {
            this.columnId = columnId;
        }
    }
}