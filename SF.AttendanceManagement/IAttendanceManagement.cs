using SF.AttendanceManagement.Models.RequestModel;
using SF.AttendanceManagement.Models.ResponseModel;
using SF.AttendanceManagement.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.AttendanceManagement
{
    public interface IAttendanceManagement
    {
        AttendanceFinancialReportOutputModel GenerateDepertmentReport(AttendanceFinancialReportInputModel inputModel, string destinationPath = "");
        IDepartmentReportGeneratorService GetDepartmentReportGeneratorService();

        DataTable RemoveDoubleTappingInstanceFromGuardRoom(DataTable guardRoomRecords);

        string ValidateFinancialReportGenerationInput(AttendanceFinancialReportInputModel inputModel);

        IEnumerable<DataTable> ConvertDepartmentRecordsToDataTable(ICollection<string> files);
        DataTable ConvertDepartmentRecordsToDataTable(string path);
        DataTable ConvertGuardRoomRecordsToDataTable(string path);
        DataTable ConvertSettlementRecordsToDataTable(string path);

        bool IsGuardRoomFileValid(string path);
        bool IsSettlementFileValid(string path);
        bool IsDepartmentFileValid(string path, DateTime startDate, DateTime endDate);
    }
}
