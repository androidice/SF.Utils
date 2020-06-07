using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using SF.Utils.Extensions;
using SF.Utils.Validators;
using SF.Utils.WorkBookConverter;
using SF.AttendanceManagement.Models.RequestModel;
using SF.AttendanceManagement.Models.ResponseModel;


namespace SF.AttendanceManagement
{
    public class AttendanceManagement : IAttendanceManagement
    {
        private readonly IWorkBookConverter workbookConverter = new WorkBookConverter();

        private readonly int STANDARD_WORKING_HOURS = 8;
        private readonly int LOGIN_MIN_BUFFER = 2; //set minimum login buffer for 2hours, use this to deduct 2hour from login time this is for the early login records
        private readonly int LOGOUT_MAX_BUFFER = 1; //set max logout buffer for 1hours, use this to add 1hour from lougout time, this is to cater the full 1hour extra time 

        private readonly string[] medical_leave_identifiers = new string[] { "病假" }; // medical leave
        private readonly string[] no_pay_leave_identifiers = new string[] { "事假" }; // no pay leave
        private readonly string[] annual_leave_identifiers = new string[] { "公休" }; //annual leave

        private readonly string[] off_in_liue_identifiers = new string[] { "调休", "休" }; // off in liue
        private readonly string[] normal_shift_identifier = new string[] { "√" };// normal shift

        private readonly string[] mid_shift_identifiers = new string[] { "中" }; // midshift 16:00-00:00 or 16:30-00:30
        private readonly string[] night_shift_identifiers = new string[] { "夜" }; // night shift 00:00-08:00
        private readonly string[] mixed_schedule_identifiers = new string[] { "中夜" }; //full mid-day shift + 1/2 night shift or 1/2 mid-day shift + full night shift   20:00-08:00 or 16:00-04:00

        public DateTime? AttendanceReportStartDate
        {
            get
            {
                return AttendanceReportStartDate;
            }
            set
            {
                this.AttendanceReportStartDate = value;
            }
        }

        public DateTime? AttendanceReportEndDate
        {
            get
            {
                return AttendanceReportEndDate;
            }
            set
            {
                this.AttendanceReportEndDate = value;
            }
        }

        /// <summary>
        /// Generate financial report and return the path location 
        /// for the generated file location
        /// </summary>
        /// <returns></returns>
        public AttendanceFinancialReportOutputModel GenerateFinancialReport(AttendanceFinancialReportInputModel inputModel, string destinationPath = "")
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

                this.AttendanceReportStartDate = startDate;
                this.AttendanceReportEndDate = endDate;

                IEnumerable<DataTable> departmentRecords = this.ConvertDepartmentRecordsToDataTable(inputModel.DepartmentFilePaths);
                DataTable guardRoomRecords = this.ConvertGuardRoomRecordsToDataTable(inputModel.GuardRoomFilePath);
                DataTable settlementRecords = this.ConvertSettlementRecordsToDataTable(inputModel.SettlementFilePath);

                IEnumerable<DataTable> results = this.GenerateFinancialReport(startDate, endDate, departmentRecords, guardRoomRecords, settlementRecords);

            }
            return new AttendanceFinancialReportOutputModel()
            {
                Success = isValid,
                ErrorMsg = errorMsg,
                DestinationPaths = new List<string>()
            };
        }

        private IEnumerable<DataTable> GenerateFinancialReport(DateTime startDate,
                                             DateTime endDate,
                                             IEnumerable<DataTable> departmentRecords,
                                             DataTable guardRoomRecords,
                                             DataTable settlementRecords)
        {
            string[] separators = new string[] { "日期", "姓名", "" };
            ICollection<DataTable> outResults = new List<DataTable>();

            foreach (var departmentRecord in departmentRecords)
            { // loop through each department records

                DataTable outResult = new DataTable();
                outResult.Columns.AddRange(new DataColumn[] {
                    new DataColumn("empName"),
                    new DataColumn("wkDayOt"),
                    new DataColumn("wkEndOt"),
                    new DataColumn("midShiftOt"),
                    new DataColumn("nightShiftOt"),
                    new DataColumn("noOfMedicalLeave"),
                    new DataColumn("noOfOffInLiue"),
                    new DataColumn("noOfNoPayLeave")
                });

                foreach (DataRow record in departmentRecord.Rows)
                {
                    int column_index = 1;
                    string rowValue = record[0].ToString();

                    for (DateTime sDate = startDate; DateTime.Compare(sDate, endDate) < 0; sDate = sDate.AddDays(1))
                    {// loop through date columns
                        bool shoudEscape = separators.Any(x => x.Trim().Equals(rowValue.Trim()));
                        if (!shoudEscape)
                        {//means that the record is an employee record
                            //this.AnalyzeEmployeeRecord(record, guardRoomRecords, column_index);
                            column_index++;
                        }
                        else
                        {
                            break;//go to the next record
                        }
                    }
                }
            }
            return outResults;
        }

        

        public void ProccessSchedules()
        {

        }

        public void ProcessMorningSchedule(decimal overtimeHours, DataRow empRow)
        {
            //shedule: "08:00:00-16:00:00";
            DateTime loginDateTime = DateTime.Today.AddHours(8 - LOGIN_MIN_BUFFER);//set login time from 7am
            DateTime logoutDateTime = loginDateTime.AddHours(STANDARD_WORKING_HOURS + LOGOUT_MAX_BUFFER);
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
            foreach (DataRow headerRow in headerRecords) {
                int rowIndex = result.Rows.IndexOf(headerRow);
                DataRow tempRow = result.Rows[rowIndex + 1];
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
