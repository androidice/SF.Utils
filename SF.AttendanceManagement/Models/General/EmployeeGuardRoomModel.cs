using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.AttendanceManagement.Models.General
{
    public class EmployeeGuardRoomModel
    {
        public ICollection<DataRow> EmployeeRecords { get; set; }
        public string Message { get; set; }
    }
}
