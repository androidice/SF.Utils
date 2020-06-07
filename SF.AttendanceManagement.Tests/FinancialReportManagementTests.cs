using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Xunit;
using SF.AttendanceManagement.Services;
using SF.AttendanceManagement.Models.General;

namespace SF.AttendanceManagement.Tests
{
    public class FinancialReportManagementTests
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

            Assert.Equal(8, worked_hours);

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
        public void GET_EMPLOYEE_RECORD_FROM_GUARD_ROOM()
        {
            IAttendanceManagement attendanceManagement = new AttendanceManagement();
            IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();

            DataTable guardRoomTable = attendanceManagement.ConvertGuardRoomRecordsToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\attendance generation files\4月门卫打卡数据.xls");

            throw new Exception("In complete implementation");
        }
    }
}
