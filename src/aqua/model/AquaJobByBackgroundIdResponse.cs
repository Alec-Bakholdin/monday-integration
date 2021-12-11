namespace monday_integration.src.aqua.model
{
    public class AquaJobByBackgroundIdResponse
    {
        public string jobId {get; set;}
        public string jobDescription {get; set;}
        public AquaJobStatus jobStatus {get; set;}
        public string jobStatusText {get; set;}
        public string publishLink {get; set;}
        public AquaAccessScope publishLinkAccessScope {get; set;}
    }
}