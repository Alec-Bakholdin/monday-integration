using System;

namespace monday_integration.src.aqua.model
{
    internal class MondayAttribute : Attribute
    {
        public string test {get; private set;}

        public MondayAttribute(string test) {
            this.test = test;
        }
    }
}