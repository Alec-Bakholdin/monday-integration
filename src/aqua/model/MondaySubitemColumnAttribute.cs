using System;

namespace monday_integration.src.aqua.model
{
    internal class MondaySubitemColumnAttribute : Attribute
    {
        public string columnId {get; private set;}

        public MondaySubitemColumnAttribute(string columnId) {
            this.columnId = columnId;
        }
    }
}