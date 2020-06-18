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

        public bool IsReportedScheduleIsException(string reported_schedule) {
            bool isException = false;

            isException = reported_schedule.Equals(EmployeeOff.MEDICAL_LEAVE);
            isException = isException || reported_schedule.Equals(EmployeeOff.NO_PAY_LEAVE);
            isException = isException || reported_schedule.Equals(EmployeeOff.ANNUAL_LEAVE);
            isException = isException || reported_schedule.Equals(EmployeeOff.OFF_IN_LIUE);

            return isException;
        }

        private EmployeeGuardRoomModel HandleExceptionCases(IEnumerable<DataRow> empRecords, string reported_schedule, DateTime current_date)
        {
            string message = string.Empty;
            bool hasGuardRoomRecord = empRecords.Count() > 0;

            if (hasGuardRoomRecord)
            {
                /*get the full range of possible time log, for this current date*/
                DateTime start_range = current_date;
                DateTime end_range = current_date.AddDays(1).AddSeconds(-1);
                /*get the full range of possible time log, for this current date*/
                IEnumerable<DataRow> queryResult = QueryEmployeeRecords(empRecords, start_range, end_range);

                EmployeeGuardRoomModel validateResult = this.ValidateGuardRoomEntries(queryResult);
                if (validateResult != null)
                {
                    if (validateResult.Message.Equals("NoReport"))
                    {
                        bool isMedicalLeave = reported_schedule.Equals(EmployeeOff.MEDICAL_LEAVE);
                        bool isNoPayLeave = reported_schedule.Equals(EmployeeOff.NO_PAY_LEAVE);
                        bool isAnnualLeave = reported_schedule.Equals(EmployeeOff.ANNUAL_LEAVE);
                        bool isOffInLiue = reported_schedule.Equals(EmployeeOff.OFF_IN_LIUE);

                        if (isMedicalLeave) message = EmployeeOff.MEDICAL_LEAVE;
                        if (isNoPayLeave) message = EmployeeOff.NO_PAY_LEAVE;
                        if (isAnnualLeave) message = EmployeeOff.ANNUAL_LEAVE;
                        if (isOffInLiue) message = EmployeeOff.OFF_IN_LIUE;

                        return new EmployeeGuardRoomModel()
                        {
                            Message = message
                        };
                    }
                }
                return validateResult;
            }
            else {
                return new EmployeeGuardRoomModel()
                {
                    Message = "NoGuardRoomRecord"
                };
            }
        }

        public EmployeeGuardRoomModel GetEmployeeRecordFromGuardRoom(DataTable guardRoomTable, string employeeName, string reported_schedule, decimal reported_worked_hours, DateTime current_date)
        {
            const int NAME_INDEX = 3;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            const int DATE_TIME_INDEX = 9;
            string errMsg = string.Empty;
        

            employeeName = employeeName.TrimAllExtraSpace();

            IEnumerable<DataRow> empRecords = guardRoomTable.AsEnumerable()
                .Where(row => row[NAME_INDEX].ToString().TrimAllExtraSpace().Equals(employeeName));

            bool isException = IsReportedScheduleIsException(reported_schedule);

            if (!isException && empRecords.Count() == 0)
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

                IEnumerable<DataRow> resultQuery = QueryEmployeeRecords(empRecords, start_date_range, end_date_range);

                EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(resultQuery);
                if (validationResult != null)
                {
                    if (validationResult.Message.Equals("NoLogOut"))
                    {
                        /**
                         * To Handle case where the default query results to no logout, but find any instance login and logout
                         * instance within the schedule
                         */
                        string loginstamp = validationResult.EmployeeRecords.FirstOrDefault()[DATE_TIME_INDEX].ToString();
                        DateTime loginEntry = DateTime.ParseExact(loginstamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                        DateTime logoutRange = logout_schedule.AddHours(1);
                        resultQuery = QueryEmployeeRecords(empRecords, loginEntry, logoutRange);

                        if (Decimal.Remainder(resultQuery.Count(), 2) == 0)
                        {
                            return new EmployeeGuardRoomModel()
                            {
                                EmployeeRecords = resultQuery.ToList(),
                                Message = errMsg
                            };
                        }
                    }

                    if (validationResult.Message.Equals("InvalidTimeLog"))
                    {
                        /**
                         * if found invalid time log from the default logout time, look out for the extension
                         * of logout temporarily upto 1hour extension
                         */
                        const int MAXIMUM_LOGOUT_EXTENSION = 60;// set 30 min maximum extension after the schedule
                        end_date_range = end_date_range.AddMinutes(MAXIMUM_LOGOUT_EXTENSION);

                        resultQuery = QueryEmployeeRecords(empRecords, start_date_range, end_date_range);

                        if (Decimal.Remainder(resultQuery.Count(), 2) == 0)
                        {
                            resultQuery = NormalizeLoginAndLogout(resultQuery, login_schedule, login_schedule);

                            return new EmployeeGuardRoomModel()
                            {
                                EmployeeRecords = resultQuery.ToList(),
                                Message = errMsg
                            };
                        }
                    }
                    return validationResult;
                }


                resultQuery = NormalizeLoginAndLogout(resultQuery, login_schedule, login_schedule);
                empRecords = resultQuery;
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

                empRecords = QueryEmployeeRecords(empRecords, start_date_range, end_date_range);

                EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(empRecords);
                if (validationResult != null)
                    return validationResult;

                empRecords = NormalizeLoginAndLogout(empRecords, login_schedule, logout_schedule);
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

                empRecords = QueryEmployeeRecords(empRecords, start_date_range, end_date_range);

                EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(empRecords);
                if (validationResult != null)
                    return validationResult;

                empRecords = NormalizeLoginAndLogout(empRecords, login_schedule, logout_schedule);
            }
            else if (reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
            {
                // schedule1: "20:00:00-08:00:00";
                // schedule2: "16:00:00-04:00:00";
                string[] schedule1 = "20:00:00-08:00:00".Split('-');
                string[] schedule2 = "16:00:00-04:00:00".Split('-');

                string str_current_start_schedule1 = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule1[0]);
                string str_current_end_schedule1 = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule1[1]);

                DateTime login_schedule1 = DateTime.ParseExact(str_current_start_schedule1, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule1 = DateTime.ParseExact(str_current_end_schedule1, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range1 = login_schedule1.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range1 = login_schedule1.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                /*query for first schedule*/
                IEnumerable<DataRow> firstShiftQuery = QueryEmployeeRecords(empRecords, start_date_range1, end_date_range1);

                string str_current_start_schedule2 = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), schedule2[0]);
                string str_current_end_schedule2 = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), schedule2[1]);

                DateTime login_schedule2 = DateTime.ParseExact(str_current_start_schedule2, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                DateTime logout_schedule2 = DateTime.ParseExact(str_current_end_schedule2, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                DateTime start_date_range2 = login_schedule2.AddHours(LOGIN_MIN_BUFFER * -1);
                DateTime end_date_range2 = login_schedule2.AddHours((double)reported_worked_hours + LOGOUT_MAX_BUFFER);

                IEnumerable<DataRow> secondShiftQuery = QueryEmployeeRecords(empRecords, start_date_range2, end_date_range2);

                bool isFirstSchedule = false;
                bool isSecondSchedule = false;

                if (firstShiftQuery.Count() > 0)
                {
                    string maxLoginShed = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), "20:30:00");
                    DateTime maxLogin = DateTime.ParseExact(maxLoginShed, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                    string loginStamp = firstShiftQuery.FirstOrDefault()[DATE_TIME_INDEX].ToString();
                    DateTime loginEntry = DateTime.ParseExact(loginStamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                    isFirstSchedule = DateTime.Compare(loginEntry, maxLogin) <= 0;
                }

                if (secondShiftQuery.Count() > 0)
                {
                    string maxLoginShed = string.Format("{0} {1}", current_date.ToString("yyyy-MM-dd"), "16:30:00");
                    DateTime maxLogin = DateTime.ParseExact(maxLoginShed, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

                    string loginStamp = secondShiftQuery.FirstOrDefault()[DATE_TIME_INDEX].ToString();
                    DateTime loginEntry = DateTime.ParseExact(loginStamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
                    isSecondSchedule = DateTime.Compare(loginEntry, maxLogin) <= 0;
                }


                if (isSecondSchedule && !isFirstSchedule && Decimal.Remainder(secondShiftQuery.Count(), 2) == 0)// to identify the record in guard room, that record should be paired
                {
                    empRecords = NormalizeLoginAndLogout(secondShiftQuery, login_schedule2, logout_schedule2);
                    return new EmployeeGuardRoomModel()
                    {
                        EmployeeRecords = empRecords.ToList(),
                        Message = errMsg
                    };
                }


                if (isFirstSchedule && !isSecondSchedule && Decimal.Remainder(firstShiftQuery.Count(), 2) == 0)// to identify the record in guard room, that record should be paired
                {
                    empRecords = NormalizeLoginAndLogout(firstShiftQuery, login_schedule1, logout_schedule1);
                    return new EmployeeGuardRoomModel()
                    {
                        EmployeeRecords = empRecords.ToList(),
                        Message = errMsg
                    };
                }


                if (!isFirstSchedule && !isSecondSchedule)
                {
                    EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(secondShiftQuery);
                    if (validationResult != null)
                        return validationResult;
                }

                if (isSecondSchedule && Decimal.Remainder(secondShiftQuery.Count(), 2) != 0)
                {
                    EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(secondShiftQuery);
                    if (validationResult != null)
                        return validationResult;
                }

                if (isFirstSchedule && Decimal.Remainder(firstShiftQuery.Count(), 2) != 0)
                {
                    EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(firstShiftQuery);
                    if (validationResult != null)
                        return validationResult;
                }

                if (isSecondSchedule && secondShiftQuery.Count() == 1)
                {
                    EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(secondShiftQuery);
                    if (validationResult != null)
                        return validationResult;
                }

                if (isFirstSchedule && firstShiftQuery.Count() == 1)
                {
                    EmployeeGuardRoomModel validationResult = ValidateGuardRoomEntries(firstShiftQuery);
                    if (validationResult != null)
                        return validationResult;
                }
            }
            else if (isException)
            {
                EmployeeGuardRoomModel result = HandleExceptionCases(empRecords, reported_schedule, current_date);
                return result;
            }
         
            return new EmployeeGuardRoomModel()
            {
                EmployeeRecords = empRecords.ToList(),
                Message = errMsg
            };
        }

        private IEnumerable<DataRow> QueryEmployeeRecords(IEnumerable<DataRow> empRecords, DateTime start_date_range, DateTime end_date_range)
        {
            const int DATE_TIME_INDEX = 9;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

            IEnumerable<DataRow> records = empRecords.Where(row =>
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), start_date_range) >= 0 &&
                                                   DateTime.Compare(DateTime.ParseExact(row[DATE_TIME_INDEX].ToString(), DATE_TIME_FORMAT, CultureInfo.CurrentCulture), end_date_range) <= 0
                                                   );
            return records;
        }

        private IEnumerable<DataRow> NormalizeLoginAndLogout(IEnumerable<DataRow> empRecords, DateTime login_schedule, DateTime logout_schedule)
        {
            const int DATE_TIME_INDEX = 9;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

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

            return empRecords;
        }

        private EmployeeGuardRoomModel ValidateGuardRoomEntries(IEnumerable<DataRow> empRecords)
        {
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

            if (Decimal.Remainder(empRecords.Count(), 2) != 0) // to identify the record in guard room, that record should be paired
            {
                return new EmployeeGuardRoomModel()
                {
                    Message = "InvalidTimeLog"
                };
            }

            return null;
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
