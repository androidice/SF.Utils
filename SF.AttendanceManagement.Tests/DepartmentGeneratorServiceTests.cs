using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Xunit;
using SF.AttendanceManagement.Services;
using SF.AttendanceManagement.Models.General;

namespace SF.AttendanceManagement.Tests
{
    public class DepartmentGeneratorServiceTests
    {

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_NORMAL_SHIFT()
        {
            /// identify morning shift if only given reported attendance is only a number
            string reported_attendance = "√";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MORNING_SHIFT, attendance);

            reported_attendance = "√8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MORNING_SHIFT, attendance);

            /// identify morning shift if there is a positive integer is reported from department attendance
            reported_attendance = "√8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MORNING_SHIFT, attendance);

            /// identify morning shift if there is +- sign on the reported attendance
            reported_attendance = "√+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MORNING_SHIFT, attendance);

            reported_attendance = "√-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MORNING_SHIFT, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDDANCE_FOR_MID_SHIFT()
        {
            /// identify mid shift
            string reported_attendance = "中";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MID_SHIFT, attendance);

            reported_attendance = "中8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MID_SHIFT, attendance);

            reported_attendance = "中+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MID_SHIFT, attendance);

            reported_attendance = "中-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.MID_SHIFT, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDDANCE_FOR_NIGHT_SHIFT()
        {
            /// identify night shift
            string reported_attendance = "夜";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.NIGHT_SHIFT, attendance);

            reported_attendance = "夜8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.NIGHT_SHIFT, attendance);

            reported_attendance = "夜+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.NIGHT_SHIFT, attendance);

            reported_attendance = "夜-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.NIGHT_SHIFT, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_HALF_MIDSHIFT_AND_HALF_NIGHT()
        {
            string reported_attendance = "中夜";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT, attendance);

            reported_attendance = "中夜8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT, attendance);

            reported_attendance = "中夜+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT, attendance);

            reported_attendance = "中夜-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_MEDICAL_LEAVE()
        {
            string reported_attendance = "病假";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.MEDICAL_LEAVE, attendance);

            reported_attendance = "病假8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.MEDICAL_LEAVE, attendance);

            reported_attendance = "病假+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.MEDICAL_LEAVE, attendance);

            reported_attendance = "病假-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.MEDICAL_LEAVE, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_NO_PAY_LEAVE()
        {
            string reported_attendance = "事假";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.NO_PAY_LEAVE, attendance);

            reported_attendance = "事假8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.NO_PAY_LEAVE, attendance);

            reported_attendance = "事假+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.NO_PAY_LEAVE, attendance);

            reported_attendance = "事假-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.NO_PAY_LEAVE, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_ANNUAL_LEAVE()
        {
            string reported_attendance = "公休";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.ANNUAL_LEAVE, attendance);

            reported_attendance = "公休8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.ANNUAL_LEAVE, attendance);

            reported_attendance = "公休+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.ANNUAL_LEAVE, attendance);

            reported_attendance = "公休-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.ANNUAL_LEAVE, attendance);
        }

        [Fact]
        public void GET_REPORTED_ATTENDANCE_FOR_OFF_IN_LIEU()
        {
            string reported_attendance = "调休";
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            string attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "调休8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "调休+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "调休-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);



            reported_attendance = "休";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "休8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "休+8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);

            reported_attendance = "休-8";
            attendance = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
            Assert.Equal(EmployeeOff.OFF_IN_LIUE, attendance);
        }

        [Fact]
        public void GET_REPORTED_WORKHOURS_WEEKDAY_MORNING_OR_ANY_SCHEDULE()
        {
            string reported_attendance = "√";
            decimal worked_hours = 0;
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 05));

            Assert.Equal(8, worked_hours);

            reported_attendance = "√8";
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 05));

            Assert.Equal(16, worked_hours);

            reported_attendance = "√2";
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 05));

            Assert.Equal(10, worked_hours);
        }

        [Fact]
        public void GET_REPORTED_WORKHOURS_WEEKEND_FOR_MIDSHIFT_OR_ANY_SCHEDULE()
        {
            string reported_attendance = "中";
            decimal worked_hours = 0;
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 06));

            Assert.Equal(8, worked_hours);

            reported_attendance = "中8";
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 06));

            Assert.Equal(8, worked_hours);

            reported_attendance = "中5";
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 06));

            Assert.Equal(5, worked_hours);
        }

        [Fact]
        public void GET_REPORTED_WORKHOURS_WEEKDAY_FOR_HALF_MID_HALF_NIGHT_SCHEDULE()
        {
            string reported_attendance = "中夜4";
            decimal worked_hours = 0;
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 05));

            Assert.Equal(12, worked_hours);
        }

        [Fact]
        public void GET_REPORTED_WORKHOURS_WEEKEND_FOR_HALF_MID_HALF_NIGHT_SCHEDULE()
        {
            string reported_attendance = "中夜12";
            decimal worked_hours = 0;
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, new DateTime(2020, 06, 06));

            Assert.Equal(12, worked_hours);
        }

        [Fact]
        public void CAN_CHECK_IF_DATE_IS_HOLIDAY()
        {
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            DateTime current_date = new DateTime(2020, 06, 10);
            bool isHoliday = departmentReportGeneratorService.IsHoliday(current_date);
            Assert.False(isHoliday);
        }

        [Fact]
        public void CAN_SET_AND_DETERMINE_HOLIDAYS()
        {
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
            DateTime current_date = new DateTime(2020, 06, 10);
            departmentReportGeneratorService.SetDepartmentHolidays(new List<DateTime>()
            {
                new DateTime(2020, 06, 10)
            });
            bool isHoliday = departmentReportGeneratorService.IsHoliday(current_date);
            Assert.True(isHoliday);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_FROM_GUARDROOM_SHOULD_CHECK_IF_THE_EMPLOYEE_HAS_GUARDROOM_RECORDS_FOR_MORNING_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MORNING_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "王燕";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\morning shift\4月门卫打卡数据 - no guard room data.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Null(result.EmployeeRecords); ;
            Assert.Equal("NoGuardRoomRecord", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_FROM_GUARDROOM_SHOULD_CHECK_IF_THE_EMPLOYEE_HAS_NO_REPORT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MORNING_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "王燕";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\morning shift\4月门卫打卡数据 - no report.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Null(result.EmployeeRecords);
            Assert.Equal("NoReport", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_FROM_GUARDROOM_SHOULD_CHECK_IF_THE_EMPLOYEE_HAS_NO_LOGOUT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MORNING_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "王燕";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\morning shift\4月门卫打卡数据 - no logout record.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal(1, result.EmployeeRecords.Count); ;
            Assert.Equal("NoLogOut", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_NORMAL_WORKED_HOURS_FOR_MORNING_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MORNING_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "王燕";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\morning shift\4月门卫打卡数据.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);
            const int DATE_TIME_INDEX = 9;
            string login_entry = result.EmployeeRecords.FirstOrDefault()[DATE_TIME_INDEX].ToString();
            string logout_entry = result.EmployeeRecords.LastOrDefault()[DATE_TIME_INDEX].ToString();

            Assert.Equal(string.Empty, result.Message);
            Assert.Equal("2020-04-01 08:00:00", login_entry);
            Assert.Equal("2020-04-01 16:00:00", logout_entry);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_IF_HAS_EMPLOYEE_RECORD_ON_MIDHISFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MID_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "张友俊";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\midshift\4月门卫打卡数据 - no guard room record.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoGuardRoomRecord", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_IF_HAS_EMPLOYEE_REPORTED_ON_MIDHISFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MID_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "张友俊";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\midshift\4月门卫打卡数据 - no reporting time.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoReport", result.Message);
        }


        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_IF_HAS_EMPLOYEE_HAS_NO_LOGOUT_MIDHISFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MID_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "张友俊";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\midshift\4月门卫打卡数据 - no logout.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoLogOut", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_IF_HAS_EMPLOYEE_HAS_INVALID_LOGS_MIDSHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.MID_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "张友俊";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\midshift\4月门卫打卡数据 - invalid time log.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("InvalidTimeLog", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_NIGHT_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.NIGHT_SHIFT;
            decimal reported_worked_hours = 8;
            DateTime date = new DateTime(2020, 04, 01);
            string employeeName = "尚彩红";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\midshift\4月门卫打卡数据.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_MID_AND_HALF_NIGHT_SHIFT_1st_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 03);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal(string.Empty, result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_MID_AND_HALF_NIGHT_SHIFT_2nd_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 02);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据-2nd shift.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal(string.Empty, result.Message);
        }


        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_NO_RECORD_FROM_GUARDROOM_FOR_HALF_MID_AND_HALF_NIGHT() {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 02);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据 - no login 2nd shift.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoReport", result.Message);
        }


        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_NO_LOGOUT_FROM_GUARDROOM_FOR_HALF_MID_AND_HALF_NIGHT_FOR_1st_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 02);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据 - no logout 1st shift.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoLogOut", result.Message);
            Assert.Equal(1, result.EmployeeRecords.Count);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_HANDLE_NO_LOGOUT_FROM_GUARDROOM_FOR_HALF_MID_AND_HALF_NIGHT_FOR_2ND_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 03);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据 - no logout 2nd shift.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("NoLogOut", result.Message);
            Assert.Equal(1, result.EmployeeRecords.Count);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_FOR_INVALID_TIME_LOG_FROM_GUARDROOM_FOR_HALF_MID_AND_HALF_NIGHT_FOR_2ND_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 03);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据 - 2nd shift invalid time log.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("InvalidTimeLog", result.Message);
        }

        [Fact]
        public void GET_EMPLOYEE_RECORD_SHOULD_CHECK_FOR_INVALID_TIME_LOG_FROM_GUARDROOM_FOR_HALF_MID_AND_HALF_NIGHT_FOR_1st_SHIFT()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            string reported_schedule = EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;
            decimal reported_worked_hours = 12;
            DateTime date = new DateTime(2020, 04, 02);
            string employeeName = "邓建平";
            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\half mid half night\4月门卫打卡数据 - 1st shift invalid time log.xls");

            EmployeeGuardRoomModel result = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomTable, employeeName, reported_schedule, reported_worked_hours, date);

            Assert.Equal("InvalidTimeLog", result.Message);
        }
    }
}
