using System;
using System.Collections.Generic;
using System.Text;

namespace SF.AttendanceManagement.Models.RequestModel
{
    public class AttendanceFinancialReportInputModel
    {
        public string ReportDateString { get; set; }
        public string GuardRoomFilePath { get; set; }
        public string SettlementFilePath { get; set; }
        public ICollection<string> DepartmentFilePaths { get; set; }
    }
}
