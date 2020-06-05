using SF.AttendanceManagement.Models.FinancialReportModel;
using SF.AttendanceManagement.Models.RequestModel;
using SF.AttendanceManagement.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.AttendanceManagement
{
    public interface IAttendanceManagement
    {
        AttendanceFinancialReportOutputModel GenerateFinancialReport(AttendanceFinancialReportInputModel inputModel, string destinationPath = "");
        string ValidateFinancialReportGenerationInput(AttendanceFinancialReportInputModel inputModel);

        IEnumerable<DataTable> ConvertDepartmentRecordsToDataTable(ICollection<string> files);
        DataTable ConvertGuardRoomRecordsToDataTable(string path);
        DataTable ConvertSettlementRecordsToDataTable(string path);

        bool IsGuardRoomFileValid(string path);
        bool IsSettlementFileValid(string path);
        bool IsDepartmentFileValid(string path, DateTime startDate, DateTime endDate);

        AttendanceReportModel GetAttendanceReport(AttendanceReportModel model);
    }
}
