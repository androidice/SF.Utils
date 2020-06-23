using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SF.AttendanceManagement.Models.ResponseModel
{
    public class AttendanceFinancialReportOutputModel
    {
        public bool Success { get; set; }
        public string ErrorMsg { get; set; }
        public ICollection<DataTable> ResultingTables { get; set; }
    }
}
