using SF.Utils.Validators;
using SF.Utils.WorkBookConverter;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Xunit;
using SF.AttendanceManagement.Models.RequestModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SF.AttendanceManagement.Tests
{
    public class AttendanceManagementTests
    {
        [Fact]
        public void CONVERT_GUARD_ROOM_FILE_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter();

            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls", 0, 1);
            Assert.IsType<DataTable>(result);// check if successfully convert excell file to datatable
        }

        /// <summary>
        /// Validate if the guard room file is valid
        /// </summary>
        [Fact]
        public void VALIDATE_GUARD_ROOM_FILE()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\4月门卫打卡数据.xls";
            bool isValid = attendanceManagement.IsGuardRoomFileValid(fileLocation);

            Assert.True(isValid);// if false handle error message on the caller
        }

        [Fact]
        public void VALIDATE_SETTLEMENTFILE()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\4月.xlsx";
            bool isValid = attendanceManagement.IsSettlementFileValid(fileLocation);  
            Assert.True(isValid);
        }

        [Fact]
        public void VALIDATE_DEPARTMENT_FILE()
        {
            string dateString = "2020-04-01";//yyyy-MM-dd

            DateTime startDate = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1); // get the last day of the month

            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\2020年控制阀4月考勤.xlsx";

            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            bool isValid = attendanceManagement.IsDepartmentFileValid(fileLocation, startDate, endDate);
            Assert.True(isValid);
        }

        [Fact]
        public void SHOULD_RETURN_EMPTY_IF_DEPATMENTFILE_IS_VALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel() {
                    ReportDateString = "2019-04-01",
                    DepartmentFilePaths= new List<string>()
                    {
                        @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\2020年控制阀4月考勤.xlsx"
                    }
                }
            );
            Assert.True(string.IsNullOrEmpty(errorMsg));
        }

        [Fact]
        public void SHOULD_RETURN_NOT_EMPTY_IF_DEPATMENTFILE_IS_INVALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel()
                {
                    ReportDateString = "2019-04-01",
                    DepartmentFilePaths = new List<string>()
                    {
                        @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation error files\2020年控制阀4月考勤.xlsx"
                    }
                }
            );
            Assert.True(!string.IsNullOrEmpty(errorMsg));
        }
        
        [Fact]
        public void SHOULD_RETURN_EMPTY_IF_GUARDROOMFILE_IS_VALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel()
                {
                    ReportDateString = "2019-04-01",
                    GuardRoomFilePath = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls"
                }
            );
            Assert.True(string.IsNullOrEmpty(errorMsg));
        }

        [Fact]
        public void SHOULD_RETURN_NOT_EMPTY_IF_GUARDROOMFILE_IS_INVALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel()
                {
                    ReportDateString = "2019-04-01",
                    GuardRoomFilePath = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation error files\4月门卫打卡数据.xls"
                }
            );
            Assert.True(!string.IsNullOrEmpty(errorMsg));
        }

        [Fact]
        public void SHOULD_RETURN_EMPTY_IF_SETTLMENTFILE_IS_VALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel()
                {
                    ReportDateString = "2020-04-01",
                    SettlementFilePath = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\4月.xlsx"
                }
            );
            Assert.True(string.IsNullOrEmpty(errorMsg));
        }

        [Fact]
        public void SHOULD_RETURN_NOT_EMPTY_IF_SETTLMENTFILE_IS_INVALID()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            string errorMsg = attendanceManagement.ValidateFinancialReportGenerationInput(
                new AttendanceFinancialReportInputModel()
                {
                    ReportDateString = "2019-04-01",
                    SettlementFilePath = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\settlement file-err.xlsx"
                }
            );
            Assert.True(!string.IsNullOrEmpty(errorMsg));
        }

        [Fact]
        public void GENERATE_OVERTIME_REPORT() {

           
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            //example of how we set holidays
            attendanceManagement.GetDepartmentReportGeneratorService().SetDepartmentHolidays(new List<DateTime>()
            {
                new DateTime(2020,04,06)
            });// set holiday if any

            attendanceManagement.GenerateDepertmentReport(new AttendanceFinancialReportInputModel()
            {
                ReportDateString = "2020-04-01",
                DepartmentFilePaths = new List<string>() {
                    @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\to process1\2020年控制阀4月考勤.xlsx"
                },
                GuardRoomFilePath = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\to process1\4月门卫打卡数据.xls"
            });
            Assert.True(true); // update later
        }
    }
}
