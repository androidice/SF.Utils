using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SF.AttendanceManagement.Services
{
    public interface IDepartmentReportGeneratorService
    {
        ICollection<DataRow> GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string schedule, DateTime current_date);
        string GetReportedAttendance(string reported_attendance);
        decimal GetReportedWorkedHours(string reported_attendance, DateTime current_date);
        int CalculateOvertimework(IEnumerable<string> timestamps);
        decimal[] Apply36HoursRule(decimal weekdayOt, decimal weekendOt);
    }
}
