using System;

namespace SemanticBackup.WebClient.Models.Response
{
    public class StatusResponseModel
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public Guid ID { get; set; }
        public string IdString { get; set; }
    }
}
