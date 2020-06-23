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
using SF.Utils.Services.DataTableServices;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using NPOI.SS.UserModel;
using NPOI.HSSF.Util;

namespace SF.AttendanceManagement
{

    public class AttendanceManagement : IAttendanceManagement
    {
        private readonly IWorkBookConverter workbookConverter;
        private readonly IDataTableConverter dataTableConverter;
        private readonly IDepartmentReportGeneratorService departmentReportGeneratorService;

        private readonly ILoggerService loggerService;
        private readonly ILogger logger;
        

        /// <summary>
        /// Set upload path for the files to be uploaded to the server
        /// </summary>
        public string UploadPath { get; set; }


        /// <summary>
        /// Set the result path for the result files to be saved on the server
        /// </summary>
        public string ResultPath { get; set; }

        public AttendanceManagement(IConfiguration config = null)
        {
            this.loggerService = new LoggerService();
            this.logger = loggerService.CreateLogger(GetType().FullName);
            
            workbookConverter = new WorkBookConverter(this.logger);
            dataTableConverter = new DataTableConverter(this.logger);
            departmentReportGeneratorService = new DepartmentReportGeneratorService(this.logger);
        }


        ///// <summary>
        ///// Set the path and filename for logger
        ///// if path is empty it will use the current date format yyyyMMddHHmmss
        ///// for filename
        ///// </summary>
        ///// <param name="path"></param>
        ///// <param name="filename"></param>
        ///// <returns></returns>
        public string ConfiguraLoggingPath(string path, string fileName = "")
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Please provide location where to save the logging");
            fileName = string.IsNullOrEmpty(fileName) ? DateTime.Now.ToString("yyyyMMddHHmmss") : fileName;
            path = Path.Combine(path, fileName);
            ILoggerFactory loggerFacotry = loggerService.LoggerFactory.AddFile(path);
            return path;
        }

        private readonly int STANDARD_WORKING_HOURS = 8;

        public IDepartmentReportGeneratorService GetDepartmentReportGeneratorService()
        {
            return departmentReportGeneratorService;
        }

        public DataTable RemoveDoubleTappingInstanceFromGuardRoom(DataTable guardRoomRecords)
        {
            DataTable result = new DataTable(guardRoomRecords.TableName);
            const int DATE_TIME_INDEX = 9;
            const int NAME_INDEX = 3;
            const int DEPARTMENT_INDEX = 0;


            foreach (DataColumn column in guardRoomRecords.Columns)
                result.Columns.Add(new DataColumn(column.ColumnName));

            decimal totalNoOfRecords = guardRoomRecords.Rows.Count;
            decimal transfer = 0;
            decimal percentage = 0;

            foreach (DataRow row in guardRoomRecords.Rows)
            {
                string department = row[DEPARTMENT_INDEX]?.ToString().TrimAllExtraSpace();
                string name = row[NAME_INDEX]?.ToString().TrimAllExtraSpace();
                string dateTimeStamp = row[DATE_TIME_INDEX]?.ToString().TrimAllExtraSpace();
                percentage = (transfer / totalNoOfRecords) * 100;


                logger.LogInformation(string.Format("Clearing guard room records by removing double tapping instance [{0} - {1}] ({2}%)", transfer, totalNoOfRecords, percentage.ToString("0#")));

                bool isValid = !string.IsNullOrEmpty(department) &&
                               !string.IsNullOrEmpty(name) &&
                               !string.IsNullOrEmpty(dateTimeStamp);

                if (isValid)
                {
                    DataRow query = result.AsEnumerable()
                                          .LastOrDefault(crow => (!string.IsNullOrEmpty(crow[DEPARTMENT_INDEX]?.ToString()) && crow[DEPARTMENT_INDEX].ToString().Equals(department)) &&
                                                                 (!string.IsNullOrEmpty(crow[NAME_INDEX]?.ToString()) && crow[NAME_INDEX].ToString().Equals(name))
                                                                );

                    if (query == null)
                    {
                        DataRow newRow = result.NewRow();
                        newRow.ItemArray = row.ItemArray;
                        result.Rows.Add(newRow);
                    }

                    if (query != null)
                    {
                        string last_time_stamp = query[DATE_TIME_INDEX]?.ToString();
                        DataRow queryRow = FilterDuplicateTimeStamp(row, last_time_stamp);
                        if (queryRow == null)
                        {
                            DataRow newRow = result.NewRow();
                            newRow.ItemArray = row.ItemArray;
                            result.Rows.Add(newRow);
                        }
                        else
                        {
                            query.ItemArray = queryRow.ItemArray;
                        }
                    }
                }
                transfer++;
            }
            logger.LogInformation(string.Format("Clearing guard room records by removing double tapping instance [{0} - {1}] ({2}%)", transfer, totalNoOfRecords, percentage.ToString("0#")));
            return result;
        }

        private DataRow FilterDuplicateTimeStamp(DataRow currentRecord, string last_time_stamp)
        {
            const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
            const int DOUBLE_TAPPING_MAX_MINUTES = 15;//teporarily set to maximum of 15mins for the double tapping
            const int DATE_TIME_INDEX = 9;
            string current_time_stamp = currentRecord[DATE_TIME_INDEX].ToString();

            DateTime d1 = DateTime.ParseExact(last_time_stamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
            DateTime d2 = DateTime.ParseExact(current_time_stamp, DATE_TIME_FORMAT, CultureInfo.CurrentCulture);
            TimeSpan diff = d2.Subtract(d1);

            if (diff.Minutes <= DOUBLE_TAPPING_MAX_MINUTES && diff.Hours == 0)
            {
                currentRecord[DATE_TIME_INDEX] = current_time_stamp;
                return currentRecord;
            }

            return null;
        }

        public AttendanceFinancialReportOutputModel GenerateDepertmentReport(AttendanceFinancialReportInputModel inputModel, string destinationPath = "")
        {

            bool isValid = true;
            string errorMsg = string.Empty;
            string outputPath = string.Empty;
            ICollection<DataTable> results = new List<DataTable>();

            errorMsg = ValidateFinancialReportGenerationInput(inputModel);
            isValid = errorMsg.Equals(string.Empty);

            if (isValid)// process financial generation
            {

                string reportDateString = inputModel.ReportDateString;
                DateTime startDate = DateTime.ParseExact(reportDateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                IEnumerable<DataTable> departmentRecords = this.ConvertDepartmentRecordsToDataTable(inputModel.DepartmentFilePaths);
                DataTable guardRoomRecords = this.ConvertGuardRoomRecordsToDataTable(inputModel.GuardRoomFilePath);
                guardRoomRecords = RemoveDoubleTappingInstanceFromGuardRoom(guardRoomRecords);
                DataTable settlementRecords = this.ConvertSettlementRecordsToDataTable(inputModel.SettlementFilePath);

                this.logger.LogInformation("Preparing overtime report, please wait");
                foreach (DataTable departmentRecord in departmentRecords)
                {
                    this.logger.LogInformation("Preparing overtime report for {0}, please wait", departmentRecord.TableName);
                    DataTable overtimeReport = this.PrepareOvertimeReport(startDate, endDate, guardRoomRecords, departmentRecord);
                    results.Add(overtimeReport);
                    this.logger.LogInformation("Preparing overtime report for {0} has been completed", departmentRecord.TableName);
                }
                this.logger.LogInformation("Preparing overtime report has been completed");
            }
            return new AttendanceFinancialReportOutputModel()
            {
                Success = isValid,
                ErrorMsg = errorMsg,
                ResultingTables = results
            };
        }



        public IEnumerable<string> CreateFinancialReportFiles(IEnumerable<DataTable> overtimeReports, string location)
        {
            ICollection<string> locations = new List<string>();
            //DataTable overtimeReport = new DataTable(departmentRecords.TableName);
            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("serialNo"),
            //    new DataColumn("empName"),
            //    new DataColumn("department"),
            //    new DataColumn("weekDayOverTime"),
            //    new DataColumn("weekEndOverTime"),
            //    new DataColumn("nightShiftCount"),
            //    new DataColumn("midShiftCount"),
            //    new DataColumn("medicalLeave"),
            //    new DataColumn("noPayLeave"),
            //    new DataColumn("annualLeave"),
            //    new DataColumn("offInLiue"),
            //    new DataColumn("changeHour")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("workedHoursByLaw"),
            //    new DataColumn("weekdayExceedOt"),
            //    new DataColumn("weekEndExeedOt"),
            //    new DataColumn("nightShiftPay"),
            //    new DataColumn("nightShiftMealAllowance"),
            //    new DataColumn("midShiftPay"),
            //    new DataColumn("miscAllowance")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("awayForOfficialBusiness"),
            //    new DataColumn("workInjuryLeave"),
            //    new DataColumn("remarks")
            //});

            dataTableConverter.InitializeWorkBook();
            /**Todo: add code for converting datatable to a financial report excell file*/

            this.logger.LogInformation("Preparing output templates");

            ICollection<DataTable> templates = new List<DataTable>();

            decimal totalRows = overtimeReports.Count();
            decimal transfer = 0;
            decimal percentage = 0;

            foreach (DataTable table in overtimeReports)
            {
                transfer = 0;

                DataTable template = new DataTable(table.TableName);
                template.Columns.AddRange(new DataColumn[] {
                    new DataColumn("serialNo"),
                    new DataColumn("empName"),
                    new DataColumn("department"),
                    new DataColumn("annualLeave"),
                    new DataColumn("medicalLeave"),
                    new DataColumn("noPayLeave"),
                    new DataColumn("offInLiue"),
                    new DataColumn("awayForOfficialBusiness"),
                    new DataColumn("workInjuryLeave"),
                    new DataColumn("midShiftCount"),
                    new DataColumn("midShiftPay"),
                    new DataColumn("nightShiftCount"),
                    new DataColumn("nightShiftPay"),
                    new DataColumn("nightShiftMealAllowance"),
                    new DataColumn("miscAllowance"),
                    new DataColumn("weekDayOverTime"),
                    new DataColumn("weekEndOverTime"),
                    new DataColumn("weekdayExceedOt"),
                    new DataColumn("weekEndExeedOt"),
                    new DataColumn("workedHoursByLaw"),
                    new DataColumn("remarks"),
                });

                foreach (DataRow row in table.Rows)
                {
                    percentage = (transfer / totalRows) * 100;
                    logger.LogInformation(string.Format("Preparing template [{0} - {1}] ({2}%)", transfer, totalRows, percentage));

                    DataRow tempRow = template.NewRow();
                    tempRow["serialNo"] = row["serialNo"];
                    tempRow["empName"] = row["empName"];
                    tempRow["department"] = row["department"];
                    tempRow["annualLeave"] = row["annualLeave"];
                    tempRow["medicalLeave"] = row["medicalLeave"];
                    tempRow["noPayLeave"] = row["noPayLeave"];
                    tempRow["offInLiue"] = row["offInLiue"];
                    tempRow["awayForOfficialBusiness"] = row["awayForOfficialBusiness"];
                    tempRow["workInjuryLeave"] = row["workInjuryLeave"];
                    tempRow["midShiftCount"] = row["midShiftCount"];
                    tempRow["midShiftPay"] = row["midShiftPay"];
                    tempRow["nightShiftCount"] = row["nightShiftCount"];
                    tempRow["nightShiftPay"] = row["nightShiftPay"];
                    tempRow["nightShiftMealAllowance"] = row["nightShiftMealAllowance"];
                    tempRow["miscAllowance"] = row["miscAllowance"];
                    tempRow["weekDayOverTime"] = row["weekDayOverTime"];
                    tempRow["weekEndOverTime"] = row["weekEndOverTime"];
                    tempRow["weekdayExceedOt"] = row["weekdayExceedOt"];
                    tempRow["weekEndExeedOt"] = row["weekEndExeedOt"];
                    tempRow["workedHoursByLaw"] = row["workedHoursByLaw"];
                    tempRow["remarks"] = row["remarks"];
                    template.Rows.Add(tempRow);
                    transfer++;
                }
                percentage = (transfer / totalRows) * 100;
                logger.LogInformation(string.Format("Preparing template [{0} - {1}] ({2}%)", transfer, totalRows, percentage));
                templates.Add(template);
            }


            this.logger.LogInformation("Writring output files");


            foreach (DataTable table in templates)
            {
                string filelocation = dataTableConverter.ConvertDataTableToExcell(table,
                                                                                  location,
                                                                                  table.TableName,
                                                                                  ApplyFinancialReportStyle,
                                                                                  ApplyFinancialReportStyle,
                                                                                  ApplyFinancialHeaderPorxy,
                                                                                  ApplyFinancialReportRowValueProxy);
                locations.Add(filelocation);
            }
            this.logger.LogInformation("Writring output files completed");

            return locations;
        }


        public string ApplyFinancialHeaderPorxy(string current_value, string columnName)
        {
            //DataTable template = new DataTable(table.TableName);
            //template.Columns.AddRange(new DataColumn[] {
            //        new DataColumn("serialNo"),
            //        new DataColumn("empName"),
            //        new DataColumn("department"),
            //        new DataColumn("annualLeave"),
            //        new DataColumn("medicalLeave"),
            //        new DataColumn("noPayLeave"),
            //        new DataColumn("offInLiue"),
            //        new DataColumn("awayForOfficialBusiness"),
            //        new DataColumn("workInjuryLeave"),
            //        new DataColumn("midShiftCount"),
            //        new DataColumn("midShiftPay"),
            //        new DataColumn("nightShiftCount"),
            //        new DataColumn("nightShiftPay"),
            //        new DataColumn("nightShiftMealAllowance"),
            //        new DataColumn("miscAllowance"),
            //        new DataColumn("weekDayOverTime"),
            //        new DataColumn("weekEndOverTime"),
            //        new DataColumn("weekdayExceedOt"),
            //        new DataColumn("weekEndExeedOt"),
            //        new DataColumn("workedHoursByLaw"),
            //        new DataColumn("remarks"),
            //    });

            Dictionary<string, string> columns = new Dictionary<string, string>() {
                {"serialNo","序号" },
                {"empName","姓名" },
                {"department","部门" },
                {"annualLeave","公休 " },
                {"medicalLeave","病假" },
                {"noPayLeave","事假" },
                {"offInLiue","换休 \n 小时）" },
                {"awayForOfficialBusiness","公出" },
                {"workInjuryLeave","工伤/产假 \n 小时" },
                {"midShiftCount","中班" },
                {"midShiftPay","中班费" },
                {"nightShiftCount","夜班" },
                {"nightShiftPay","夜班费" },
                {"nightShiftMealAllowance","夜班餐费" },
                {"miscAllowance","中夜班及误餐费" },
                {"weekDayOverTime","延时" },
                {"weekEndOverTime","双休" },
                {"weekdayExceedOt","延时 \n（超)" },
                {"weekEndExeedOt","双休 \n（超）" },
                {"workedHoursByLaw","法定" },
                {"remarks","备注" },
            };

            if (columns.ContainsKey(columnName))
                return columns[columnName];

            return columnName;
        }

        public string ApplyFinancialReportRowValueProxy(string current_value, string columName)
        {
            bool isEmpty = (string.IsNullOrEmpty(current_value) || current_value.Equals("0"));
            if (isEmpty) return string.Empty;

            return current_value;
        }

        public ICellStyle ApplyFinancialReportStyle(IWorkbook workbook, ISheet sheet, string columnName = "", int columnIndex = -1)
        {

            //DataTable overtimeReport = new DataTable(departmentRecords.TableName);
            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("serialNo"),
            //    new DataColumn("empName"),
            //    new DataColumn("department"),
            //    new DataColumn("weekDayOverTime"),
            //    new DataColumn("weekEndOverTime"),
            //    new DataColumn("nightShiftCount"),
            //    new DataColumn("midShiftCount"),
            //    new DataColumn("medicalLeave"),
            //    new DataColumn("noPayLeave"),
            //    new DataColumn("annualLeave"),
            //    new DataColumn("offInLiue"),
            //    new DataColumn("changeHour")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("workedHoursByLaw"),
            //    new DataColumn("weekdayExceedOt"),
            //    new DataColumn("weekEndExeedOt"),
            //    new DataColumn("nightShiftPay"),
            //    new DataColumn("nightShiftMealAllowance"),
            //    new DataColumn("midShiftPay"),
            //    new DataColumn("miscAllowance")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("awayForOfficialBusiness"),
            //    new DataColumn("workInjuryLeave"),
            //    new DataColumn("remarks")
            //});
            string[] special_columns = new string[] { "offInLiue", "midShiftCount", "weekDayOverTime", "weekEndOverTime" };
            bool isSpecial = special_columns.Any(identifier => identifier.Equals(columnName));

            IFont font = workbook.CreateFont();
            if (isSpecial)
                font.Color = IndexedColors.Red.Index;

            if (columnIndex >= 0)
                sheet.SetColumnWidth(columnIndex, 3000);

            ICellStyle cellStyle = workbook.CreateCellStyle();
            cellStyle.Alignment = HorizontalAlignment.Center;
            cellStyle.VerticalAlignment = VerticalAlignment.Center;
            cellStyle.WrapText = true;

            cellStyle.SetFont(font);

            return cellStyle;
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

            overtimeReport.Columns.AddRange(new DataColumn[] {
                new DataColumn("workedHoursByLaw"),
                new DataColumn("weekdayExceedOt"),
                new DataColumn("weekEndExeedOt"),
                new DataColumn("nightShiftPay"),
                new DataColumn("nightShiftMealAllowance"),
                new DataColumn("midShiftPay"),
                new DataColumn("miscAllowance")
            });

            overtimeReport.Columns.AddRange(new DataColumn[] {
                new DataColumn("awayForOfficialBusiness"),
                new DataColumn("workInjuryLeave"),
                new DataColumn("remarks")
            });
            const int NAME_INDEX = 1;

            int serialNo = 1;


            string errorMsg = string.Empty;
            decimal totalRecord = departmentRecords.Rows.Count;
            decimal transfer = 0;
            decimal percentage = 0;
            foreach (DataRow departmentRecord in departmentRecords.Rows)
            {
                int date_index = 2;
                percentage = (transfer / totalRecord) * 100;

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

                logger.LogInformation("Processing {0} ({1} out of {2}) - ({3}%)", departmentRecords.TableName, transfer, totalRecord, percentage.ToString("0#"));

                if (!isHeaderIndex)
                {
                    employeeName = departmentRecord[NAME_INDEX]?.ToString();
                    if (!string.IsNullOrEmpty(employeeName))// there should be no empty name from department tempate report
                    {
                        for (DateTime current_date = startDate; DateTime.Compare(current_date, endDate) <= 0; current_date = current_date.AddDays(1))
                        {
                            logger.LogInformation(string.Format("Generating report for {0} on {1}", employeeName, current_date.ToString("yyyy-MM-dd")));
                            const int DATE_TIME_INDEX = 9;
                            const int DEPARTMENT_INDEX = 0;
                            string reported_attendance = departmentRecord[date_index]?.ToString();
                            if (!string.IsNullOrEmpty(reported_attendance))// prevent empty record to be query from guard room
                            {
                                string reported_schedule = departmentReportGeneratorService.GetReportedAttendance(reported_attendance);
                                decimal reported_worked_hours = departmentReportGeneratorService.GetReportedWorkedHours(reported_attendance, current_date);
                                if (!string.IsNullOrEmpty(reported_schedule))
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

                                            if ((worked_hours > STANDARD_WORKING_HOURS && !current_date.IsWeekEnd()) || departmentReportGeneratorService.IsHoliday(current_date))
                                            {
                                                if (!departmentReportGeneratorService.IsHoliday(current_date))
                                                    weekDayOvertime = weekDayOvertime + (worked_hours - STANDARD_WORKING_HOURS);
                                                else
                                                    weekDayOvertime = weekDayOvertime + worked_hours;
                                            } // for weekday overtime
                                            else if (current_date.IsWeekEnd()) //  for weekend overtime
                                                weekEndOvertime = weekEndOvertime + worked_hours;
                                            else if (worked_hours < STANDARD_WORKING_HOURS) // for weekend and week day undertime
                                                offInLiue = offInLiue + (STANDARD_WORKING_HOURS - worked_hours);

                                            bool isException = departmentReportGeneratorService.IsReportedScheduleIsException(reported_schedule);
                                            if (isException && worked_hours > 0)
                                                logger.LogInformation(string.Format("Employee {0} reported {1}, but recorded {2}hour(s) of worked", employeeName, reported_schedule, worked_hours));

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
                                            logger.LogError(string.Format("Employee {0} reported {1} but unable to match any records on {2}", employeeName, reported_attendance, current_date.ToString("yyyy-MM-dd")));
                                        }
                                    }
                                    else
                                    {
                                        switch (employee_login_records.Message)
                                        {
                                            case "NoGuardRoomRecord":
                                                //TODO: Record no guard room entry for the employee
                                                logger.LogError(string.Format("Employee {0} reported {1} on {2} but shows no guard room record", employeeName, reported_schedule, current_date.ToString("yyyy-MM-dd")));
                                                break;
                                            case "NoReport":
                                                //TODO: Record the employee has no login or logout
                                                logger.LogError(string.Format("Employee {0} reported {1} but shows no login records on {2}", employeeName, reported_schedule, current_date.ToString("yyyy-MM-dd")));
                                                break;
                                            case "NoLogOut":
                                                //TODO: Login has no matching logout
                                                const int DATE_INDEX = 9;
                                                DataRow reported = employee_login_records.EmployeeRecords.FirstOrDefault();
                                                offInLiue = offInLiue + STANDARD_WORKING_HOURS;
                                                logger.LogError(string.Format("Employee {0} reported {1} login but shows no logout on {2}", employeeName, reported[DATE_INDEX], current_date.ToString("yyyy-MM-dd")));
                                                break;
                                            case "InvalidTimeLog":
                                                //TODO: Employee guard room records contains not pairing login and logout, for multiple entries
                                                logger.LogError(string.Format("Employee {0} reported {1} but shows invalid time logs on {2}", employeeName, reported_schedule, current_date.ToString("yyyy-MM-dd")));
                                                break;
                                        }
                                        bool isMedicalLeave = reported_schedule.Equals(EmployeeOff.MEDICAL_LEAVE);
                                        bool isNoPayLeave = reported_schedule.Equals(EmployeeOff.NO_PAY_LEAVE);
                                        bool isAnnualLeave = reported_schedule.Equals(EmployeeOff.ANNUAL_LEAVE);
                                        bool isOffInLiue = reported_schedule.Equals(EmployeeOff.OFF_IN_LIUE);

                                        if (isMedicalLeave) medicalLeave = medicalLeave + STANDARD_WORKING_HOURS;
                                        if (isNoPayLeave) noPayLeave = noPayLeave + STANDARD_WORKING_HOURS;
                                        if (isAnnualLeave) annualLeave = annualLeave + STANDARD_WORKING_HOURS;
                                        if (isOffInLiue) offInLiue = offInLiue + STANDARD_WORKING_HOURS;
                                    }
                                }
                                else
                                {
                                    // TODO: record the reported schedule as not supported symbol
                                    logger.LogError(string.Format("Employee {0} recorded {1} on {2} which is not supported", employeeName, reported_attendance, current_date.ToString("yyyy-MM-dd")));
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

                        /*apply adjustments from off in lieu*/
                        decimal[] adjustments = ApplyOvertimeAdjustments(weekDayOvertime, weekEndOvertime, offInLiue);
                        tempRow["weekDayOverTime"] = adjustments[0];// adjust the weekday overtime
                        tempRow["weekEndOverTime"] = adjustments[1];// adjust the weekend overtime 
                        tempRow["changeHour"] = adjustments[2];// record change hour

                        /*compute employee payements*/
                        tempRow = ComputeEmployeeFee(tempRow);

                        /**
                         * TODO: 
                         * 1. Draft the first result to an excell file
                         * 2. Draft the 2nd result with adjustments from the off in liue to an excell file
                         */


                        serialNo++;
                    }
                }

                transfer++;
            }
            logger.LogInformation("Processing {0} ({1} out of {2}) - ({3}%)", departmentRecords.TableName, transfer, totalRecord, percentage.ToString("0#"));

            return overtimeReport;
        }

        public DataRow ComputeEmployeeFee(DataRow employeeRecord)
        {
            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("workedHoursByLaw"), - temporarily set to 0
            //    new DataColumn("weekdayExceedOt"), - after 36hours rule
            //    new DataColumn("weekEndExeedOt"), - after 36hours rule
            //    new DataColumn("nightShiftPay"), - done
            //    new DataColumn("nightShiftMealAllowance"), - done
            //    new DataColumn("midShiftPay"), - done
            //    new DataColumn("miscAllowance") = done
            //});

            const decimal MID_SHIFT_PAY = 8;
            const decimal NIGHT_SHIFT_PAY = 12;
            const decimal NIGHT_MEAL_ALLOWANCE = 4;

            decimal annualLeave = Decimal.Parse(employeeRecord["annualLeave"].ToString());
            decimal medicalLeave = Decimal.Parse(employeeRecord["medicalLeave"].ToString());
            decimal noPayLeave = Decimal.Parse(employeeRecord["noPayLeave"].ToString());
            decimal offInLieu = Decimal.Parse(employeeRecord["offInLiue"].ToString());
            decimal midShift = Decimal.Parse(employeeRecord["midShiftCount"].ToString());
            decimal nightShift = Decimal.Parse(employeeRecord["nightShiftCount"].ToString());
            decimal weekdayOt = Decimal.Parse(employeeRecord["weekDayOverTime"].ToString());
            decimal weekendOt = Decimal.Parse(employeeRecord["weekEndOverTime"].ToString());

            decimal midShiftPay = midShift * MID_SHIFT_PAY;
            decimal nightShiftPay = nightShift * NIGHT_SHIFT_PAY;
            decimal nightShiftMealAllowance = nightShift * NIGHT_MEAL_ALLOWANCE;
            decimal miscAllowance = midShiftPay + nightShiftPay + nightShiftMealAllowance;


            employeeRecord["workedHoursByLaw"] = 0;// temporarily set to 0
            employeeRecord["weekdayExceedOt"] = 0;// temporarily set to 0, update after 36hours rule by applying the settlement
            employeeRecord["weekEndExeedOt"] = 0;// temporarily set to 0, update after 36hours rule by applying the settlement

            employeeRecord["nightShiftPay"] = nightShiftPay;
            employeeRecord["midShiftPay"] = midShiftPay;
            employeeRecord["nightShiftMealAllowance"] = nightShiftMealAllowance;
            employeeRecord["miscAllowance"] = miscAllowance;

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("awayForOfficialBusiness"),
            //    new DataColumn("workInjuryLeave"),
            //    new DataColumn("remarks")
            //});

            employeeRecord["awayForOfficialBusiness"] = 0;
            employeeRecord["workInjuryLeave"] = 0;
            employeeRecord["remarks"] = string.Empty;

            return employeeRecord;
        }

        public decimal[] ApplyOvertimeAdjustments(decimal week_day_overtime, decimal week_end_overtime, decimal off_in_liue)
        {
            decimal[] adjustments = new decimal[] { 0, 0, 0 };// weekday, weekend, changehour
            decimal diff = 0;
            decimal totalOvertime = (week_day_overtime + week_end_overtime);
            adjustments[0] = week_day_overtime;
            adjustments[1] = week_end_overtime;
            adjustments[2] = off_in_liue;

            bool isVaid = off_in_liue > 0;
            if (!isVaid) return adjustments;

            diff = (week_end_overtime - off_in_liue);
            if (diff >= 0)
            {
                week_end_overtime = diff;
                if (diff >= 0)
                    off_in_liue = 0;
            }
            else
            {
                diff = off_in_liue - week_end_overtime;
                week_end_overtime = 0;
                if (diff >= 0)
                {
                    week_day_overtime = week_day_overtime - diff;
                    if (week_day_overtime >= 0)
                        off_in_liue = 0;
                    else
                    {
                        week_day_overtime = 0;
                        off_in_liue = (off_in_liue - totalOvertime);
                    }
                }
            }

            adjustments[0] = week_day_overtime;
            adjustments[1] = week_end_overtime;
            adjustments[2] = off_in_liue; //record change hour if greater than 0
            return adjustments;
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
                string fileName = Path.GetFileName(guardRoomFile);
                isValid = this.IsGuardRoomFileValid(guardRoomFile);

                if (!isValid) errorMsg = string.Format("Guardroom file: {0} is incorrect format", fileName);
            }
            //validate guardroom file

            //validate settlement file
            if (!string.IsNullOrEmpty(inputModel.SettlementFilePath))
            {
                string settlementFile = inputModel.SettlementFilePath;
                string fileName = Path.GetFileName(settlementFile);

                isValid = this.IsSettlementFileValid(settlementFile);

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
