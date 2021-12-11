namespace monday_integration.src.aqua.model
{
    public class AquaRerunResponse
    {
        public string jobId {get; set;}
        public string jobRunId {get; set;}
        public AquaJobStatus jobStatus {get; set;}
        public string publishLink {get; set;}
        public AquaAccessScope publishLinkAccessScope {get; set;}
    }
}