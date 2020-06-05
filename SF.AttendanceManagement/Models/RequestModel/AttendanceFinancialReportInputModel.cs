using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SF.AttendanceManagement.Models.RequestModel
{
    public class AttendanceFinancialReportInputModel
    {
        [Required]
        [Display(Name = "Report Date")]
        public string ReportDateString { get; set; }

        [Required]
        [Display(Name = "Guard Room File")]
        public string GuardRoomFilePath { get; set; }

        [Display(Name = "Settlement File")]
        public string SettlementFilePath { get; set; }

        [Display(Name = "Department Files")]
        public ICollection<string> DepartmentFilePaths { get; set; }
    }
}
