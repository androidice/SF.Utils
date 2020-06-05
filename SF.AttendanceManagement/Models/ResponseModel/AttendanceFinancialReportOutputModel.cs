using System;
using System.Collections.Generic;
using System.Text;

namespace SF.AttendanceManagement.Models.ResponseModel
{
    public class AttendanceFinancialReportOutputModel
    {
        public bool Success { get; set; }
        public string ErrorMsg { get; set; }
        public ICollection<string> DestinationPaths { get; set; }
    }
}
