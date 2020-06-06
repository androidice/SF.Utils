using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SF.AttendanceManagement.Services
{
    public interface IFinancialReportManagementService
    {
        ICollection<DataRow> GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string schedule, DateTime current_date);
        string GetReportedAttendance(string reported_attendance);
        decimal GetReportedWorkedHours(string reported_attendance, DateTime current_date);
        int CalculateOvertimework(IEnumerable<string> timestamps);
    }
}
