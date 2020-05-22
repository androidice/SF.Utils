using SF.AttendanceManagement.Models.RequestModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.AttendanceManagement
{
    public class AttendanceManagement: IAttendanceManagement
    {
        /// <summary>
        /// Generate financial report and return the path location 
        /// for the generated file location
        /// </summary>
        /// <returns></returns>
        public string GenerateFinancialReport(AttendanceFinancialReportInputModel inputModel, string destinationDirectory)
        {
            throw new NotImplementedException();
        }

        private void AnalizeDepertmentReport()
        {
            throw new NotImplementedException();
        }

        private bool IsGuardRoomFileValid(string path)
        {
            throw new NotImplementedException();
        }

        private bool IsSettlementFileValid(string path)
        {
            throw new NotImplementedException();
        }

        private bool IsDepartmentFileValid(string path)
        {
            throw new NotImplementedException();
        }

    }
}
