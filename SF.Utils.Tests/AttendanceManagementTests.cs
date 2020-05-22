using SF.Utils.WorkBookConverter;
using SF.Utils.Validators;
using System;
using System.Data;
using System.Linq;
using Xunit;
using System.Collections.Generic;
using System.Globalization;

namespace SF.Utils.Tests
{
    public class AttendanceManagementTests
    {
        [Fact]
        public void CONVERT_GUARD_ROOM_FILE_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls", 0, 1);
            Assert.IsType<DataTable>(result);// check if successfully convert excell file to datatable
        }


        /// <summary>
        /// Validate if the guard room file is valid
        /// </summary>
        [Fact]
        public void VALIDATE_GUARD_ROOM_FILE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();
            const int DATE_TIME_STAMP_INSTANCE = 5;
            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls", 0, 1);
            bool isValid = result.ValidateColumn(new List<DataColumnValidatorModel>() {
                new DataColumnValidatorModel(){
                    columnName = DATE_TIME_STAMP_INSTANCE.ToString(),
                    expectedType = typeof(DateTime),
                    pattern = "yyyy-MM-dd HH:mm:ss"
                }
            });

            Assert.True(isValid);// if false handle error message on the caller
        }

        [Fact]
        public void VALIDATE_SETTLEMENTFILE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\settlement file.xlsx", 2, 0);

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
            });
            Assert.True(isValid);
        }

        [Fact]
        public void VALIDATE_DEPARTMENT_FILE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();
            string dateString = "2019-04-01";//yyyy-MM-dd
            DateTime startDate = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.CurrentCulture);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1); // get the last day of the month

            DataTable result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\template 1-done.xlsx");

            /*validate headers*/
            string[] separators = new string[] { "日期" };
            string[] headerSeparators = new string[] { "姓名" };
            var headerRecords = result.AsEnumerable()
                                .Where(row => headerSeparators.Any(x => x.Equals(row[0].ToString())));


            bool isValid = true;
            isValid = (headerRecords != null && headerRecords.Count() > 0);
            if (isValid)
            {

                foreach (var records in headerRecords)
                {
                    int dayResult = 0;
                    int date_index = 1;
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
                            dateString = string.Format("{0}-{1}-{2}", current_year, current_month, current_day); //"yyyy-MM-dd"
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

            Assert.True(isValid);
            /*validate headers*/
        }
        
        public void GENERATE_FINANCIAL_REPORT()
        {

        }
    }
}
