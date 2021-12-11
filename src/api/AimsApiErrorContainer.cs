namespace monday_integration.src.api
{
    public class AimsApiErrorContainer
    {
        public string code {get; private set;}
        public string message {get; private set;}

        public AimsApiErrorContainer(string code, string message) 
        {
            this.code = code;
            this.message = message;
        }
    }
}