using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SF.Utils.Extensions;
using SF.AttendanceManagement.Models.General;

namespace SF.AttendanceManagement.Services
{
    public class DepartmentReportGeneratorService : IDepartmentReportGeneratorService
    {
        private readonly int STANDARD_WORKING_HOURS = 8;
        private readonly int LOGIN_MIN_BUFFER = 2; //set minimum login buffer for 2hours, use this to deduct 2hour from login time this is for the early login records
        private readonly int LOGOUT_MAX_BUFFER = 1; //set max logout buffer for 1hours, use this to add 1hour from lougout time, this is to cater the full 1hour extra time 

        private readonly string[] medical_leave_identifiers = new string[] { "病假" }; // medical leave
        private readonly string[] no_pay_leave_identifiers = new string[] { "事假" }; // no pay leave
        private readonly string[] annual_leave_identifiers = new string[] { "公休" }; //annual leave
        private readonly string[] off_in_liue_identifiers = new string[] { "调休", "休" }; // off in liue

        private readonly string[] morning_shift_identifier = new string[] { "√", "" };// normal shift
        private readonly string[] mid_shift_identifiers = new string[] { "中" }; // midshift 16:00-00:00 or 16:30-00:30
        private readonly string[] night_shift_identifiers = new string[] { "夜" }; // night shift 00:00-08:00
        private readonly string[] half_midshift_half_nightshift_identifiers = new string[] { "中夜" }; //full mid-day shift + 1/2 night shift or 1/2 mid-day shift + full night shift   20:00-08:00 or 16:00-04:00


        public int CalculateOvertimework(IEnumerable<string> timestamps)
        {
            throw new NotImplementedException();
        }

        public string GetReportedAttendance(string reported_attendance)
        {
            string pattern = @"([-+]?\d+(\.\d+)?)|([-+]?\.\d+)";
            string nonSymbol = Regex.Match(reported_attendance, pattern).Value;
            string symbol = string.Empty;

            if (string.IsNullOrEmpty(nonSymbol)) symbol = reported_attendance;
            else
            {
                symbol = reported_attendance.Replace(nonSymbol, string.Empty);
                symbol = symbol.TrimAllExtraSpace();
            }

            bool isMorningShift = morning_shift_identifier.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isMorningShift) return EmployeeShifts.MORNING_SHIFT;

            bool isMidShift = mid_shift_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isMidShift) return EmployeeShifts.MID_SHIFT;

            bool isNightSift = night_shift_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isNightSift) return EmployeeShifts.NIGHT_SHIFT;

            bool isHalfMidAndHalfNight = half_midshift_half_nightshift_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isHalfMidAndHalfNight) return EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT;

            bool isMedicalLeave = medical_leave_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isMedicalLeave) return EmployeeOff.MEDICAL_LEAVE;

            bool isNoPayLeave = no_pay_leave_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isNoPayLeave) return EmployeeOff.NO_PAY_LEAVE;

            bool isAnnualLeave = annual_leave_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isAnnualLeave) return EmployeeOff.ANNUAL_LEAVE;

            bool isOffInLieu = off_in_liue_identifiers.Any(identifier => identifier.TrimAllExtraSpace().Equals(symbol));
            if (isOffInLieu) return EmployeeOff.OFF_IN_LIUE;

            throw new NotSupportedException(string.Format("The symbol: {0} is not supported", symbol));
        }

        public decimal GetReportedWorkedHours(string reported_attendance, DateTime current_date)
        {
            string pattern = @"([-+]?\d+(\.\d+)?)|([-+]?\.\d+)";
            string nonSymbol = Regex.Match(reported_attendance, pattern).Value;

            string reported_schedule = GetReportedAttendance(reported_attendance);
            if (reported_schedule.Equals(EmployeeShifts.MORNING_SHIFT) || reported_schedule.Equals(EmployeeShifts.MID_SHIFT) ||
                reported_schedule.Equals(EmployeeShifts.NIGHT_SHIFT))
            {
                if (string.IsNullOrEmpty(nonSymbol)) return STANDARD_WORKING_HOURS;

                decimal worked_hours = decimal.Parse(nonSymbol);

                if (!current_date.IsWeekEnd() && worked_hours != STANDARD_WORKING_HOURS) return (STANDARD_WORKING_HOURS + worked_hours); // this is for week day

                return worked_hours;
            }
            else if (reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
            {
                decimal worked_hours = decimal.Parse(nonSymbol);
                if (!current_date.IsWeekEnd()) return STANDARD_WORKING_HOURS + worked_hours;

                return worked_hours;
            }
            else if (reported_schedule.Equals(EmployeeOff.MEDICAL_LEAVE) || reported_schedule.Equals(EmployeeOff.NO_PAY_LEAVE) ||
                     reported_schedule.Equals(EmployeeOff.ANNUAL_LEAVE) || reported_schedule.Equals(EmployeeOff.OFF_IN_LIUE))
            {
                if (string.IsNullOrEmpty(nonSymbol)) return 0;

                decimal worked_hours = decimal.Parse(nonSymbol);

                return worked_hours;
            }
            throw new Exception("schedule is not handled");
        }

        public ICollection<DataRow> GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string schedule, DateTime current_date)
        {
            const int NAME_INDEX = 1;

            IEnumerable<DataRow> empRecords = guardRoomTable.AsEnumerable()
                .Where(row => row[NAME_INDEX].ToString().TrimAllExtraSpace().Equals(employeeName));

            if (empRecords == null) return null;
            /**
             * TODO: Filter employee records time stamp based on the schedule
             * and consider the current date.
             */
            throw new NotImplementedException();
        }

        public decimal[] Apply36HoursRule(decimal weekdayOt, decimal weekendOt)
        {
            decimal[] scores = new decimal[4] { 0, 0, 0, 0 };
            var BASE_SCORE = 36;
            decimal totalScore = weekdayOt + weekendOt; // x - weekday ot, y - weekend ot
            if (totalScore >= 36)
            {
                if (weekdayOt > BASE_SCORE)
                {
                    scores[0] = BASE_SCORE; //A
                    scores[1] = 0; //B
                    scores[2] = weekdayOt - BASE_SCORE;//C
                    scores[3] = weekendOt;//D
                }
                else if (weekdayOt < BASE_SCORE)
                {
                    scores[0] = weekdayOt;//A
                    scores[1] = BASE_SCORE - weekdayOt;//B
                    scores[2] = 0;//C
                    scores[3] = weekendOt - scores[1];//D
                }
                else
                {
                    scores[0] = weekdayOt;//A
                    scores[1] = 0;//B
                    scores[2] = 0;//C
                    scores[3] = weekendOt - 0;//D
                }
            }
            else
            {
                scores[0] = weekdayOt;
                scores[1] = weekendOt;
            }

            return scores;
        }
    }
}
