using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using SF.AttendanceManagement.Models.General;

namespace SF.AttendanceManagement.Services
{
    public interface IDepartmentReportGeneratorService
    {
        void SetDepartmentTemplateHeadersIndexes(int index);
        bool IsDepartmetTemplateHeader(int index);

        void SetDepartmentHolidays(IEnumerable<DateTime> dates);
        bool IsHoliday(DateTime current_date);

        EmployeeGuardRoomModel GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string reported_schedule, decimal reported_worked_hours, DateTime current_date);
        string GetReportedAttendance(string reported_attendance);

        decimal GetReportedWorkedHours(string reported_attendance, DateTime current_date);
        int CalculateOvertimework(ICollection<string> timestamps);

        decimal[] Apply36HoursRule(decimal weekdayOt, decimal weekendOt);
    }
}
