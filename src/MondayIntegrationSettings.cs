namespace monday_integration.src
{
    public class MondayIntegrationSettings
    {
        public string Aims360BaseURL {get; private set;}
        public string AimsBearerToken {get; private set;}
        public string AimsJobId {get; private set;}

        public string MondayBaseURL {get; private set;}
        public string MondayApiKey {get; private set;}

        public MondayIntegrationSettings(System.Collections.IDictionary dictionary) {
            foreach(var property in typeof(MondayIntegrationSettings).GetProperties()) {
                property.SetValue(this, dictionary[property.Name]);
            }
        }

        public override string ToString() {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}