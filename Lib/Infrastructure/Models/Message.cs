using Domain.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.Models
{
    public class Message
    {
        public string StudentCode { get; set; }
        public string CurrentQuestion { get; set; }
        public string CurrentTestCase { get; set; }
        public string CurrentStage { get; set; }    
        public string Type { get; set; }
        public string CurrentStep { get; set; }
        public Status Status { get; set; }
        public string Exception { get; set; } = null;
        public bool? IsTestCaseSuccess { get; set; }
    }
}
