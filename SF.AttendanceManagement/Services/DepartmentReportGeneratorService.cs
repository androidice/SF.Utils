using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SF.Utils.Extensions;
using SF.AttendanceManagement.Models.General;
using System.Globalization;

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
        private readonly ICollection<DateTime> departmentHolidays = new List<DateTime>();
        private ICollection<int> departmentTemplateHeaderIndexes = new List<int>();


        public int CalculateOvertimework(ICollection<string> timestamps)
        {
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            bool isMultipleEntry = timestamps.Count > 2;

            ICollection<DateTime> datetime_stamps = new List<DateTime>();
            datetime_stamps = timestamps
                .Select(stamp => DateTime.ParseExact(stamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture))
                .ToList();

            datetime_stamps = datetime_stamps.OrderByDescending(date_time => date_time).ToList();
            DateTime logout_entry = datetime_stamps.FirstOrDefault();
            DateTime login_entry = datetime_stamps.LastOrDefault();
            TimeSpan diff = new TimeSpan();

            if (DateTime.Compare(login_entry, logout_entry) == 0)
            {
                diff = logout_entry.Subtract(login_entry);
                return diff.Hours;
            }
            else if (!isMultipleEntry)
            {
                diff = logout_entry.Subtract(login_entry);
                return diff.Hours;
            }
            else
            {
                TimeSpan overbreak = new TimeSpan();
                Stack<DateTime> stamps = new Stack<DateTime>();
                foreach (DateTime current_date in datetime_stamps)
                {
                    bool isLogin = DateTime.Compare(current_date, login_entry) == 0;
                    bool isLogout = DateTime.Compare(current_date, logout_entry) == 0;

                    if (!isLogin && !isLogout)
                    {
                        if (stamps.Count == 0)
                        {
                            stamps.Push(current_date);
                        }
                        else
                        {
                            DateTime last_entry = stamps.Peek();
                            overbreak = overbreak + last_entry.Subtract(current_date);
                        }
                    }
                }
                diff = logout_entry.Subtract(login_entry);
                return diff.Hours - overbreak.Hours;
            }
        }

        public void SetDepartmentTemplateHeadersIndexes(int index) => departmentTemplateHeaderIndexes.Add(index);

        public bool IsDepartmetTemplateHeader(int index)
        {
            int last_index = departmentTemplateHeaderIndexes.TakeWhile(current_index => index <= current_index).FirstOrDefault();

            return index <= last_index;
        }

        public void SetDepartmentHolidays(IEnumerable<DateTime> dates)
        {
            foreach (DateTime date in dates)
            {
                departmentHolidays.Add(date);
            }
        }

        public bool IsHoliday(DateTime current_date) =>
            departmentHolidays.Any(date => DateTime.Compare(date, current_date) == 0);

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

            return string.Empty; // any unsupported symbol will return empty, and need to record as un supported
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

                if (!current_date.IsWeekEnd() && !IsHoliday(current_date)) return (STANDARD_WORKING_HOURS + worked_hours); // this is for week day, and not holiday

                return worked_hours; // for weekend and holiday
            }
            else if (reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
            {
                decimal worked_hours = decimal.Parse(nonSymbol);
                if (!current_date.IsWeekEnd() && !IsHoliday(current_date)) return STANDARD_WORKING_HOURS + worked_hours; // this is  for the week day and not holiday

                return worked_hours; // for weekend and holiday
            }
            else if (reported_schedule.Equals(EmployeeOff.MEDICAL_LEAVE) || reported_schedule.Equals(EmployeeOff.NO_PAY_LEAVE) ||
                     reported_schedule.Equals(EmployeeOff.ANNUAL_LEAVE) || reported_schedule.Equals(EmployeeOff.OFF_IN_LIUE))
            {
                if (string.IsNullOrEmpty(nonSymbol)) return 0;

                decimal worked_hours = decimal.Parse(nonSymbol);

                return worked_hours;
            }
            return 0; // any unsupported attendance should return 0
        }

        private string NormalizeLoginByScheduleToStringFormat(DateTime login, DateTime schedule)
        {
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            const int GREASE_PERIOD = 30; // set 30min for grease period
            if (DateTime.Compare(login, schedule) <= 0)
                return schedule.ToString(DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

            TimeSpan timespan = login.Subtract(schedule);
            if (timespan.Minutes > GREASE_PERIOD)
                login = login.AddHours(1);

            login = login.AddMinutes(login.Minute * -1)
                         .AddSeconds(login.Second * -1);

            return login.ToString(DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
        }

        private string NormalizeLogOutToStringFormat(DateTime logout)
        {
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            logout = logout.AddMinutes(logout.Minute * -1)
                       .AddSeconds(logout.Second * -1);

            return logout.ToString(DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
        }


        public EmployeeGuardRoomModel GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string reported_schedule, decimal reported_worked_hours, DateTime current_date)
        {
            const int NAME_INDEX = 3;
            const int DATE_TIME_INDEX = 9;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            string errMsg = string.Empty;

            employeeName = employeeName.TrimAllExtraSpace();

            IEnumerable<DataRow> empRecords = guardRoomTable.AsEnumerable()
                .Where(row => row[NAME_INDEX].ToString().TrimAllExtraSpace().Equals(employeeName));

            if (empRecords.Count() == 0)
            {
                return new EmployeeGuardRoomModel()
                {
                    Message = "NoGuardRoomRecord"
                };
            }

            if (reported_schedule.Equals(EmployeeShifts.MORNING_SHIFT))
            {
                // "08:00:00-16:00:00";
                string[] schedule = "08:00:00-16:00:00".Split('-');
                string str_current_start_schedule = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule[0]);
                string str_current_end_schedule = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule[1]);

                DateTime login_schedule = DateTime.ParseExact(str_current_start_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule = DateTime.ParseExact(str_current_end_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range = login_schedule.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range = login_schedule.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                empRecords = empRecords.Where(row =>
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                    );

                if (empRecords.Count() == 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "NoReport"
                    };
                }

                if (empRecords.Count() == 1)//no matching logout
                {
                    return new EmployeeGuardRoomModel()
                    {
                        EmployeeRecords = empRecords.ToList(),
                        Message = "NoLogOut" // should record the login time on report and record off in liue 8 hours
                    };
                }

                if (Decimal.Remainder(empRecords.Count(), 2) != 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "InvalidTimeLog"
                    };
                }

                DataRow first_entry = empRecords.FirstOrDefault();
                DataRow second_entry = empRecords.LastOrDefault();

                DateTime login = DateTime.ParseExact(
                                        NormalizeLoginByScheduleToStringFormat(
                                                DateTime.ParseExact(first_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture),
                                                login_schedule),
                                                DATE_TIME_FORMAT,
                                                CultureInfo.CurrentCulture); ;

                DateTime logout = DateTime.ParseExact(NormalizeLogOutToStringFormat(
                                        DateTime.ParseExact(second_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture)
                                        ), DATE_TIME_FORMAT, CultureInfo.CurrentCulture);


                first_entry[DATE_TIME_INDEX] = login.ToString(DATE_TIME_FORMAT);
                second_entry[DATE_TIME_INDEX] = logout.ToString(DATE_TIME_FORMAT);
            }
            else if (reported_schedule.Equals(EmployeeShifts.MID_SHIFT))
            {
                // "16:00:00-00:00:00";
                string[] schedule = "16:00:00-00:00:00".Split('-');
                string str_current_start_schedule = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule[0]);
                string str_current_end_schedule = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule[1]);

                DateTime login_schedule = DateTime.ParseExact(str_current_start_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule = DateTime.ParseExact(str_current_end_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range = login_schedule.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range = login_schedule.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                empRecords = empRecords.Where(row =>
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                    );

                if (empRecords.Count() == 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "NoReport"
                    };
                }

                if (empRecords.Count() == 1)//no matching logout
                {
                    return new EmployeeGuardRoomModel()
                    {
                        EmployeeRecords = empRecords.ToList(),
                        Message = "NoLogOut" // should record the login time on report and record off in liue 8 hours
                    };
                }

                if (Decimal.Remainder(empRecords.Count(), 2) != 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "InvalidTimeLog"
                    };
                }

                DataRow first_entry = empRecords.FirstOrDefault();
                DataRow second_entry = empRecords.LastOrDefault();

                DateTime login = DateTime.ParseExact(
                                        NormalizeLoginByScheduleToStringFormat(
                                                DateTime.ParseExact(first_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture),
                                                login_schedule),
                                                DATE_TIME_FORMAT,
                                                CultureInfo.CurrentCulture); ;

                DateTime logout = DateTime.ParseExact(NormalizeLogOutToStringFormat(
                                        DateTime.ParseExact(second_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture)
                                        ), DATE_TIME_FORMAT, CultureInfo.CurrentCulture);


                first_entry[DATE_TIME_INDEX] = login.ToString(DATE_TIME_FORMAT);
                second_entry[DATE_TIME_INDEX] = logout.ToString(DATE_TIME_FORMAT);
            }
            else if (reported_schedule.Equals(EmployeeShifts.NIGHT_SHIFT))
            {
                // "00:00:00-08:00:00";
                string[] schedule = "00:00:00-08:00:00".Split('-');

                string str_current_start_schedule = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule[0]);
                string str_current_end_schedule = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule[1]);

                DateTime login_schedule = DateTime.ParseExact(str_current_start_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule = DateTime.ParseExact(str_current_end_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range = login_schedule.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range = login_schedule.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                empRecords = empRecords.Where(row =>
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                    DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                    );

                if (empRecords.Count() == 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "NoReport"
                    };
                }

                if (empRecords.Count() == 1)//no matching logout
                {
                    return new EmployeeGuardRoomModel()
                    {
                        EmployeeRecords = empRecords.ToList(),
                        Message = "NoLogOut" // should record the login time on report and record off in liue 8 hours
                    };
                }

                if (Decimal.Remainder(empRecords.Count(), 2) != 0)
                {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "InvalidTimeLog"
                    };
                }

                DataRow first_entry = empRecords.FirstOrDefault();
                DataRow second_entry = empRecords.LastOrDefault();

                DateTime login = DateTime.ParseExact(
                                        NormalizeLoginByScheduleToStringFormat(
                                                DateTime.ParseExact(first_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture),
                                                login_schedule),
                                                DATE_TIME_FORMAT,
                                                CultureInfo.CurrentCulture); ;

                DateTime logout = DateTime.ParseExact(NormalizeLogOutToStringFormat(
                                        DateTime.ParseExact(second_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture)
                                        ), DATE_TIME_FORMAT, CultureInfo.CurrentCulture);


                first_entry[DATE_TIME_INDEX] = login.ToString(DATE_TIME_FORMAT);
                second_entry[DATE_TIME_INDEX] = logout.ToString(DATE_TIME_FORMAT);
            }
            else if (reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
            {
                // schedule1: "20:00:00-08:00:00";
                // schedule2: "16:00:00-04:00:00";
                string[] schedule1 = "20:00:00-08:00:00".Split('-');
                string[] schedule2 = "16:00:00-04:00:00".Split('-');

                string str_current_start_schedule = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule1[0]);
                string str_current_end_schedule = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule1[1]);

                DateTime login_schedule = DateTime.ParseExact(str_current_start_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule = DateTime.ParseExact(str_current_end_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range = login_schedule.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range = login_schedule.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                IEnumerable<DataRow> query1 = empRecords.Where(row =>
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                   );

                if (query1.Count() == 0) {
                    return new EmployeeGuardRoomModel()
                    {
                        Message = "NoReport"
                    };
                }

                if (Decimal.Remainder(query1.Count(), 2) == 0)
                {//consider the first schedule
                    DataRow first_entry = query1.FirstOrDefault();
                    DataRow second_entry = query1.LastOrDefault();

                    DateTime login = DateTime.ParseExact(
                                            NormalizeLoginByScheduleToStringFormat(
                                                    DateTime.ParseExact(first_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture),
                                                    login_schedule),
                                                    DATE_TIME_FORMAT,
                                                    CultureInfo.CurrentCulture); ;

                    DateTime logout = DateTime.ParseExact(NormalizeLogOutToStringFormat(
                                            DateTime.ParseExact(second_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture)
                                            ), DATE_TIME_FORMAT, CultureInfo.CurrentCulture);


                    first_entry[DATE_TIME_INDEX] = login.ToString(DATE_TIME_FORMAT);
                    second_entry[DATE_TIME_INDEX] = logout.ToString(DATE_TIME_FORMAT);

                    empRecords = query1;
                }
                else
                {// consider the second schedule
                    bool query1HasLogin = query1.Count() == 1;
                    IEnumerable<DataRow> query1Copy = query1.ToList();

                    str_current_start_schedule = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule2[0]);
                    str_current_end_schedule = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule2[1]);

                    login_schedule = DateTime.ParseExact(str_current_start_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                    logout_schedule = DateTime.ParseExact(str_current_end_schedule, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                    start_date_range = login_schedule.AddHours(LOGIN_MIN_BUFFER * -1);
                    end_date_range = login_schedule.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                    IEnumerable<DataRow> query2 = empRecords.Where(row =>
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                   );

                    if (query1HasLogin && query2.Count() == 0) {
                        return new EmployeeGuardRoomModel()
                        {
                            EmployeeRecords = query1Copy.ToList(),
                            Message = "NoLogOut" // should record the login time on report and record off in liue 8 hours
                        };
                    }

                    if (query2.Count() == 1) {
                        return new EmployeeGuardRoomModel()
                        {
                            EmployeeRecords = query2.ToList(),
                            Message = "NoLogOut" // should record the login time on report and record off in liue 8 hours
                        };
                    }

                    if (Decimal.Remainder(query2.Count(), 2) == 0)
                    {
                        DataRow first_entry = query2.FirstOrDefault();
                        DataRow second_entry = query2.LastOrDefault();

                        DateTime login = DateTime.ParseExact(
                                                NormalizeLoginByScheduleToStringFormat(
                                                        DateTime.ParseExact(first_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture),
                                                        login_schedule),
                                                        DATE_TIME_FORMAT,
                                                        CultureInfo.CurrentCulture); ;

                        DateTime logout = DateTime.ParseExact(NormalizeLogOutToStringFormat(
                                                DateTime.ParseExact(second_entry[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture)
                                                ), DATE_TIME_FORMAT, CultureInfo.CurrentCulture);


                        first_entry[DATE_TIME_INDEX] = login.ToString(DATE_TIME_FORMAT);
                        second_entry[DATE_TIME_INDEX] = logout.ToString(DATE_TIME_FORMAT);

                        empRecords = query2;
                    }
                }

            }

            return new EmployeeGuardRoomModel()
            {
                EmployeeRecords = empRecords.ToList(),
                Message = errMsg
            };
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
