using System;

namespace EchoBot1.Models
{
    public class BugReportData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CallbackTime { get; set; }
        public string PhoneNumber { get; set; }
        public string Bug { get; set; }
    }
}
