using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using SF.Utils.Extensions;
using SF.Utils.Validators;
using SF.Utils.WorkBookConverter;
using SF.AttendanceManagement.Models.RequestModel;
using SF.AttendanceManagement.Models.ResponseModel;
using SF.AttendanceManagement.Services;
using SF.AttendanceManagement.Models.General;
using Microsoft.Extensions.Logging;
using SF.Utils.Services;
using SF.Utils.Services.Logger;

namespace SF.AttendanceManagement
{
    public class AttendanceManagement : IAttendanceManagement
    {
        private readonly IWorkBookConverter workbookConverter = new WorkBookConverter();
        private readonly IDepartmentReportGeneratorService departmentReportGeneratorService = new DepartmentReportGeneratorService();
        private readonly ILogger<AttendanceManagement> logger;

        public AttendanceManagement()
        {
            ILoggerService loggerService = new LoggerService();
            logger = loggerService.CreateLogger<AttendanceManagement>();
        }

        public AttendanceManagement(ILogger<AttendanceManagement> logger)
        {
            this.logger = logger;
        }

        private readonly int STANDARD_WORKING_HOURS = 8;

        public IDepartmentReportGeneratorService GetDepartmentReportGeneratorService()
        {
            return departmentReportGeneratorService;
        }

        public AttendanceFinancialReportOutputModel GenerateDepertmentReport(AttendanceFinancialReportInputModel inputModel, string destinationPath = "")
        {

            bool isValid = true;
            string errorMsg = string.Empty;
            string outputPath = string.Empty;

            errorMsg = ValidateFinancialReportGenerationInput(inputModel);
            isValid = errorMsg.Equals(string.Empty);

            if (isValid)// process financial generation
            {

                string reportDateString = inputModel.ReportDateString;
                DateTime startDate = DateTime.ParseExact(reportDateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                IEnumerable<DataTable> departmentRecords = this.ConvertDepartmentRecordsToDataTable(inputModel.DepartmentFilePaths);
                DataTable guardRoomRecords = this.ConvertGuardRoomRecordsToDataTable(inputModel.GuardRoomFilePath);
                DataTable settlementRecords = this.ConvertSettlementRecordsToDataTable(inputModel.SettlementFilePath);

                foreach (DataTable departmentRecord in departmentRecords)
                {
                    this.PrepareOvertimeReport(startDate, endDate, guardRoomRecords, departmentRecord);
                }

            }
            return new AttendanceFinancialReportOutputModel()
            {
                Success = isValid,
                ErrorMsg = errorMsg,
                DestinationPaths = new List<string>()
            };
        }

        public DataTable PrepareOvertimeReport(DateTime startDate, DateTime endDate, DataTable guardRoomRecords, DataTable departmentRecords)
        {
            DataTable overtimeReport = new DataTable(departmentRecords.TableName);
            overtimeReport.Columns.AddRange(new DataColumn[] {
                new DataColumn("serialNo"),
                new DataColumn("empName"),
                new DataColumn("department"),
                new DataColumn("weekDayOverTime"),
                new DataColumn("weekEndOverTime"),
                new DataColumn("nightShiftCount"),
                new DataColumn("midShiftCount"),
                new DataColumn("medicalLeave"),
                new DataColumn("noPayLeave"),
                new DataColumn("annualLeave"),
                new DataColumn("offInLiue"),
                new DataColumn("changeHour")
            });
            const int NAME_INDEX = 1;

            int serialNo = 1;


            string errorMsg = string.Empty;
            int totalRecord = departmentRecords.Rows.Count;
            int totalTransfer = 0;
            int transferPercentage = 0;

            foreach (DataRow departmentRecord in departmentRecords.Rows)
            {
                int date_index = 2;
                transferPercentage = (totalTransfer / totalRecord) * 100;

                string employeeName = string.Empty;
                string departmentName = string.Empty;
                decimal weekEndOvertime = 0;
                decimal weekDayOvertime = 0;
                decimal nightShiftCount = 0;
                decimal midShiftCount = 0;
                decimal medicalLeave = 0;
                decimal noPayLeave = 0;
                decimal annualLeave = 0;
                decimal offInLiue = 0;
                decimal changeHour = 0;

                DataRow tempRow = overtimeReport.NewRow();

                int rowIndex = departmentRecords.Rows.IndexOf(departmentRecord);
                bool isHeaderIndex = departmentReportGeneratorService.IsDepartmetTemplateHeader(rowIndex);

                logger.LogInformation("Processing {0} ({1} out of {2}) - {3}%", departmentRecords.TableName, totalTransfer, totalRecord, transferPercentage);

                if (!isHeaderIndex)
                {
                    employeeName = departmentRecord[NAME_INDEX]?.ToString();
                    if (!string.IsNullOrEmpty(employeeName))// there should be no empty name from department tempate report
                    {
                        for (DateTime current_date = startDate; DateTime.Compare(current_date, endDate) <= 0; current_date = current_date.AddDays(1))
                        {
                            logger.LogInformation(string.Format("Generating report for {0} on {1}", employeeName, current_date));
                            const int DATE_TIME_INDEX = 9;
                            const int DEPARTMENT_INDEX = 0;
                            string reported_attendance = departmentRecord[date_index]?.ToString();
                            if (!string.IsNullOrEmpty(reported_attendance))// prevent empty record to be query from guard room
                            {
                                string reported_schedule = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
                                decimal reported_worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, current_date);
                                if (!string.IsNullOrEmpty(reported_schedule))
                                {
                                    if (reported_schedule.Equals(EmployeeShifts.MORNING_SHIFT) || reported_schedule.Equals(EmployeeShifts.MID_SHIFT) ||
                                     reported_schedule.Equals(EmployeeShifts.NIGHT_SHIFT) || reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
                                    {
                                        EmployeeGuardRoomModel employee_login_records = departmentReportGeneratorService.GetEmployeeRecordFromGuardRoom(guardRoomRecords, employeeName, reported_schedule, reported_worked_hours, current_date);
                                        if (string.IsNullOrEmpty(employee_login_records.Message))
                                        {
                                            ICollection<DataRow> _employee_records = employee_login_records.EmployeeRecords;

                                            if (_employee_records.Count() > 0)
                                            {
                                                departmentName = _employee_records.FirstOrDefault()[DEPARTMENT_INDEX].ToString();
                                                IEnumerable<string> timestamps = _employee_records.Select(c => c[DATE_TIME_INDEX].ToString()).ToList();
                                                decimal worked_hours = departmentReportGeneratorService.CalculateOvertimework(timestamps.ToList());

                                                if (worked_hours > STANDARD_WORKING_HOURS && !current_date.IsWeekEnd() && !departmentReportGeneratorService.IsHoliday(current_date)) // for weekday overtime
                                                    weekDayOvertime = weekDayOvertime + (worked_hours - STANDARD_WORKING_HOURS);
                                                else if (current_date.IsWeekEnd() || departmentReportGeneratorService.IsHoliday(current_date)) //  for weekend overtime
                                                    weekEndOvertime = weekEndOvertime + worked_hours;
                                                else if (worked_hours < STANDARD_WORKING_HOURS) // for weekend and week day undertime
                                                    offInLiue = offInLiue + (STANDARD_WORKING_HOURS - worked_hours);

                                                if (reported_schedule.Equals(EmployeeShifts.MID_SHIFT)) midShiftCount = midShiftCount + 1;
                                                if (reported_schedule.Equals(EmployeeShifts.NIGHT_SHIFT)) nightShiftCount = nightShiftCount + 1;
                                                if (reported_schedule.Equals(EmployeeShifts.HALF_MID_SHIFT_HALF_NIGHT_SHIFT))
                                                {
                                                    midShiftCount = midShiftCount + GetMidShiftCount(_employee_records, current_date);
                                                    nightShiftCount = nightShiftCount + GetNightShiftCount(_employee_records, current_date);
                                                }
                                            }
                                            else
                                            {
                                                //TODO: record no employee record
                                                logger.LogError(string.Format("Employee {0} reported {1} but unable to match any records on {2}", employeeName, reported_attendance, current_date));
                                            }
                                        }
                                        else
                                        {
                                            switch (employee_login_records.Message)
                                            {
                                                case "NoGuardRoomRecord":
                                                    //TODO: Record no guard room entry for the employee
                                                    logger.LogError(string.Format("Employee {0} reported {1} on {2} but shows no guard room record", employeeName, reported_attendance, current_date));
                                                    break;
                                                case "NoReport":
                                                    //TODO: Record the employee has no login or logout
                                                    logger.LogError(string.Format("Employee {0} reported {1} but shows no login records on {2}", employeeName, reported_attendance, current_date));
                                                    break;
                                                case "NoLogOut":
                                                    //TODO: Login has no matching logout
                                                    const int DATE_INDEX = 9;
                                                    DataRow reported = employee_login_records.EmployeeRecords.FirstOrDefault();
                                                    logger.LogError(string.Format("Employee {0} reported {1} login but shows no logout on {2}", employeeName, reported[DATE_INDEX], current_date));
                                                    break;
                                                case "InvalidTimeLog":
                                                    //TODO: Employee guard room records contains not pairing login and logout, for multiple entries
                                                    logger.LogError(string.Format("Employee {0} reported {1} but shows invalid time logs on {2}", employeeName, reported_attendance, current_date));
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // TODO: record the reported schedule as not supported symbol
                                    logger.LogError(string.Format("Employee {0} recorded {1} on {2} which is not supported", employeeName, reported_attendance, current_date));
                                }

                            }

                            date_index++;
                        }

                        //new DataColumn("serialNo"),
                        //new DataColumn("empName"),
                        //new DataColumn("department"),
                        //new DataColumn("weekDayOverTime"),
                        //new DataColumn("weekEndOverTime"),
                        //new DataColumn("nightShiftCount"),
                        //new DataColumn("midShiftCount"),
                        //new DataColumn("medicalLeave"),
                        //new DataColumn("noPayLeave"),
                        //new DataColumn("annualLeave"),
                        //new DataColumn("offInLiue"),
                        //new DataColumn("changeHour")
                        tempRow["serialNo"] = serialNo;
                        tempRow["empName"] = employeeName;
                        tempRow["department"] = departmentName;
                        tempRow["weekDayOverTime"] = weekDayOvertime;
                        tempRow["weekEndOverTime"] = weekEndOvertime;
                        tempRow["nightShiftCount"] = nightShiftCount;
                        tempRow["midShiftCount"] = midShiftCount;
                        tempRow["medicalLeave"] = medicalLeave;
                        tempRow["noPayLeave"] = noPayLeave;
                        tempRow["annualLeave"] = annualLeave;
                        tempRow["offInLiue"] = offInLiue;
                        tempRow["changeHour"] = changeHour;
                        overtimeReport.Rows.Add(tempRow);

                        serialNo++;
                    }
                }

                totalTransfer++;
            }

            return overtimeReport;
        }

        private decimal GetMidShiftCount(ICollection<DataRow> employee_records, DateTime current_date)
        {
            const int DATE_TIME_INDEX = 9;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            decimal count = 0;
            string midnight_timestamp = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), "00:00:00");
            string logintimestamp = employee_records.FirstOrDefault()[DATE_TIME_INDEX].ToString();

            DateTime login_entry = DateTime.ParseExact(logintimestamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
            DateTime midnight_mark = DateTime.ParseExact(midnight_timestamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
            TimeSpan diff = midnight_mark.Subtract(login_entry);
            count = STANDARD_WORKING_HOURS / diff.Hours;
            count = 1 / count;
            return count;
        }


        public decimal GetNightShiftCount(ICollection<DataRow> employee_records, DateTime current_date)
        {
            const int DATE_TIME_INDEX = 9;
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            decimal count = 0;
            string midnight_timestamp = string.Format("{0} {1}", current_date.AddDays(1).ToString("yyyy-MM-dd"), "00:00:00");
            string logouttimestamp = employee_records.LastOrDefault()[DATE_TIME_INDEX].ToString();
            DateTime logout_entry = DateTime.ParseExact(logouttimestamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
            DateTime midnight_mark = DateTime.ParseExact(midnight_timestamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);

            TimeSpan diff = logout_entry.Subtract(midnight_mark);
            count = STANDARD_WORKING_HOURS / diff.Hours;
            count = 1 / count;
            return count;
        }

        public void PrepareSettlementmentReport()
        {
            //TODO: prepare settlement report for weekday ot and overtime 
            throw new NotImplementedException();
        }

        public void PrepareOvertimeReportWithSettlement()
        {
            //TODO: prepare overtime report with the settlement
            throw new NotImplementedException();
        }

        public void PrerpareFinancialReport()
        {
            //TODO: process financial report
            throw new NotImplementedException();
        }

        public void PrepareSettlementReport()
        {
            //TODO: prepare overtime report with the settlement
            throw new NotImplementedException();
        }

        public void PrepareErrorLogReport()
        {
            //TODO: create a log report for errors like unsupported symbol
            throw new NotImplementedException();
        }



        public IEnumerable<DataTable> ConvertDepartmentRecordsToDataTable(ICollection<string> files)
        {
            bool hasRecords = files != null && files.Count > 0;
            if (hasRecords)
            {
                ICollection<DataTable> departmentRecords = new List<DataTable>();
                foreach (string file in files)
                {
                    DataTable records = ConvertDepartmentRecordsToDataTable(file);
                    departmentRecords.Add(records);
                }
                return departmentRecords;
            }
            return null;
        }


        public DataTable ConvertGuardRoomRecordsToDataTable(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DataTable guardRoomRecords = workbookConverter.ConvertWorkBookToDataTable(path, 6, 1);
                return guardRoomRecords;
            }
            return null;
        }

        public DataTable ConvertDepartmentRecordsToDataTable(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DataTable departmentRecords = workbookConverter.ConvertWorkBookToDataTable(path);
                return departmentRecords;
            }
            return null;
        }

        public DataTable ConvertSettlementRecordsToDataTable(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                DataTable settlementRecords = workbookConverter.ConvertWorkBookToDataTable(path, 2, 0);
                return settlementRecords;
            }
            return null;
        }


        public string ValidateFinancialReportGenerationInput(AttendanceFinancialReportInputModel inputModel)
        {
            bool isValid = true;
            string errorMsg = string.Empty;
            string reportDateString = inputModel.ReportDateString;
            DateTime startDate = DateTime.ParseExact(reportDateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            //validate department files
            bool hasRecords = inputModel.DepartmentFilePaths != null && inputModel.DepartmentFilePaths.Count > 0;
            if (hasRecords)
            {
                foreach (string departmentFile in inputModel.DepartmentFilePaths)
                {
                    string fileName = Path.GetFileName(departmentFile);
                    isValid = this.IsDepartmentFileValid(departmentFile, startDate, endDate);

                    if (!isValid)
                    {
                        errorMsg = string.Format("Department file: {0} is incorrect format.", fileName);
                        break;
                    }
                }
            }

            //validate department files

            //validate guardroom file
            if (!string.IsNullOrEmpty(inputModel.GuardRoomFilePath))
            {
                string guardRoomFile = inputModel.GuardRoomFilePath;
                string fileName = guardRoomFile.Substring(guardRoomFile.LastIndexOf(@"\") + 1);
                isValid = this.IsGuardRoomFileValid(guardRoomFile);

                if (!isValid) errorMsg = string.Format("Guardroom file: {0} is incorrect format", fileName);
            }
            //validate guardroom file

            //validate settlement file
            if (!string.IsNullOrEmpty(inputModel.SettlementFilePath))
            {
                string settlementFile = inputModel.SettlementFilePath;
                string fileName = settlementFile.Substring(settlementFile.LastIndexOf(@"\") + 1);

                isValid = this.IsSettlementFileValid(inputModel.SettlementFilePath);

                if (!isValid) errorMsg = string.Format("Settlement file: {0} is incorrect format", fileName);
            }
            //validate settlement file
            return errorMsg;
        }


        public bool IsGuardRoomFileValid(string path)
        {
            const int DATE_TIME_STAMP_INSTANCE = 9;
            var result = ConvertGuardRoomRecordsToDataTable(path);
            bool isValid = result.ValidateColumn(new List<DataColumnValidatorModel>() {
                new DataColumnValidatorModel(){
                    columnName = DATE_TIME_STAMP_INSTANCE.ToString(),
                    expectedType = typeof(DateTime),
                    pattern = "yyyy-MM-dd HH:mm:ss"
                }
            });
            return isValid;
        }

        public bool IsSettlementFileValid(string path)
        {
            var result = ConvertSettlementRecordsToDataTable(path);

            const int WEEKDAY_OT = 3;
            const int WEEKEND_OT = 4;
            const int PREV_WEEKDAY_OT = 5;
            const int PREV_WEEKEND_OT = 6;
            const int TOTAL_OT = 7;
            const int SETTLEMENT = 8;
            const int WEEKDAY_CARRYOVER_OT = 9;
            const int WEEKEND_CARRYOVER_OT = 10;

            bool isValid = result.ValidateColumn(new List<DataColumnValidatorModel>() {
                new DataColumnValidatorModel(){
                    columnName = WEEKDAY_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = WEEKEND_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = PREV_WEEKDAY_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = PREV_WEEKEND_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = TOTAL_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = SETTLEMENT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = WEEKDAY_CARRYOVER_OT.ToString(),
                    expectedType = typeof(decimal)
                },
                new DataColumnValidatorModel(){
                    columnName = WEEKEND_CARRYOVER_OT.ToString(),
                    expectedType = typeof(decimal)
                }
            }, new List<string>() {
                "编制：张雪明",
                "审核：",
                "批准："
            });

            return isValid;
        }

        public bool IsDepartmentFileValid(string path, DateTime startDate, DateTime endDate)
        {

            DataTable result = ConvertDepartmentRecordsToDataTable(path);

            /*validate headers*/
            string[] separators = new string[] { "日期" };
            string[] headerSeparators = new string[] { "姓名" };
            const int NAME_INDEX = 1;
            var headerRecords = result.AsEnumerable()
                                .Where(row => headerSeparators.Any(x => x.TrimAllExtraSpace().Equals(row[NAME_INDEX].ToString().TrimAllExtraSpace())));

            ICollection<DataRow> headerRows = new List<DataRow>();
            foreach (DataRow headerRow in headerRecords)
            {
                int rowIndex = result.Rows.IndexOf(headerRow);
                rowIndex = rowIndex + 1;
                departmentReportGeneratorService.SetDepartmentTemplateHeadersIndexes(rowIndex);
                DataRow tempRow = result.Rows[rowIndex];
                headerRows.Add(tempRow);
            }


            bool isValid = true;
            isValid = (headerRows.Count() > 0);
            if (isValid)
            {

                foreach (var records in headerRows)
                {
                    int dayResult = 0;
                    int date_index = 2;
                    string rowValue = records[date_index].ToString();
                    isValid = Int32.TryParse(rowValue, out dayResult);
                    if (isValid)
                    {
                        DateTime sdate = startDate;
                        while (DateTime.Compare(sdate, endDate) <= 0)
                        {
                            rowValue = records[date_index].ToString();
                            string current_year = sdate.Year.ToString();
                            string current_month = sdate.Month.ToString("00");
                            string current_day = Int32.Parse(rowValue).ToString("00");
                            string dateformat = "yyyy-MM-dd";
                            string dateString = string.Format("{0}-{1}-{2}", current_year, current_month, current_day); //"yyyy-MM-dd"
                            records[date_index] = dateString;
                            rowValue = records[date_index].ToString();

                            isValid = records.ValidateRow(new DataRowValidatorModel()
                            {
                                columnName = date_index.ToString(),
                                expectedType = typeof(DateTime),
                                pattern = dateformat
                            });

                            isValid = (isValid &&
                                        DateTime.Compare(
                                                DateTime.ParseExact(rowValue, dateformat, CultureInfo.CurrentCulture),
                                                sdate) == 0);// to validate if follows a sequence

                            if (!isValid) break;

                            sdate = sdate.AddDays(1);
                            date_index++;
                        }
                    }

                    if (!isValid) break;
                }
            }
            /*validate headers*/
            return isValid;
        }
    }
}
