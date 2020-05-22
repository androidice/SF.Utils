using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;

using System.Globalization;

namespace SF.Utils.Validators
{
    public static class DataTableValidators
    {
        /// <summary>
        /// To validate the entire datatable colum values 
        /// for a specific type of format
        /// </summary>
        /// <param name="table"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static bool ValidateColumn(this DataTable table, IList<DataColumnValidatorModel> param)
        {
            bool isValid = true;
            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    string columnName = col.ColumnName;
                    DataColumnValidatorModel columnModel = param.FirstOrDefault(c => c.columnName.Equals(columnName));
                    if (columnModel != null)
                    {

                        if (columnModel.expectedType.Equals(typeof(DateTime)))
                            isValid = IsDateValueValid(row, columnModel);
                        else if (columnModel.expectedType.Equals(typeof(Int32)))
                            isValid = IsNumericValueValid(row, columnModel);
                        else if (columnModel.expectedType.Equals(typeof(Decimal)))
                            isValid = IsDecimalValueValid(row, columnModel);
                        //extend for other types and column that uses patterns
                    }

                    if (!isValid) break;
                }

                if (!isValid) break;
            }
            return isValid;
        }

        /// <summary>
        /// To validate specific row value for a specic type or format
        /// </summary>
        /// <param name="row"></param>
        /// <param name="rowModel"></param>
        /// <returns></returns>
        public static bool ValidateRow(this DataRow row, DataRowValidatorModel rowModel) {
            bool isValid = true;
            if (rowModel != null)
            {
                if (rowModel.expectedType.Equals(typeof(DateTime)))
                    isValid = IsDateValueValid(row, rowModel);
                else if (rowModel.expectedType.Equals(typeof(Int32)))
                    isValid = IsNumericValueValid(row, rowModel);
                else if (rowModel.expectedType.Equals(typeof(Decimal)))
                    isValid = IsDecimalValueValid(row, rowModel);
                //extend for other types and value that uses patterns
            }
            return isValid;
        }

        private static bool IsDateValueValid(DataRow row, DataColumnValidatorModel columnModel)
        {
            string format = (!string.IsNullOrEmpty(columnModel.pattern)) ? columnModel.pattern : "yyyy-MM-dd HH:mm:ss";
            string rowValue = row[columnModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            DateTime outDate;
            return DateTime.TryParseExact(rowValue, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out outDate);
        }

        private static bool IsDateValueValid(DataRow row, DataRowValidatorModel rowModel)
        {
            string format = (!string.IsNullOrEmpty(rowModel.pattern)) ? rowModel.pattern : "yyyy-MM-dd HH:mm:ss";
            string rowValue = row[rowModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            DateTime outDate;
            return DateTime.TryParseExact(rowValue, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out outDate);
        }

        private static bool IsNumericValueValid(DataRow row, DataColumnValidatorModel columnModel)
        {
            string rowValue = row[columnModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            int result = 0;
            return Int32.TryParse(rowValue, out result);
        }

        private static bool IsNumericValueValid(DataRow row, DataRowValidatorModel rowModel)
        {
            string rowValue = row[rowModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            int result = 0;
            return Int32.TryParse(rowValue, out result);
        }

        private static bool IsDecimalValueValid(DataRow row, DataColumnValidatorModel columnModel)
        {
            string rowValue = row[columnModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            decimal result = 0;
            return Decimal.TryParse(rowValue, out result);
        }

        private static bool IsDecimalValueValid(DataRow row, DataRowValidatorModel rowModel)
        {
            string rowValue = row[rowModel.columnName].ToString();

            if (string.IsNullOrEmpty(rowValue))
                return true;

            decimal result = 0;
            return Decimal.TryParse(rowValue, out result);
        }
    }

    public class DataColumnValidatorModel
    {
        public string columnName { get; set; }
        public Type expectedType { get; set; } // use this for type matching
        public string pattern { get; set; } // use this for pattern matching or can be use in formatting 
        public bool useRegEx { get; set; } // true or false to indicate to use regular expression
    }

    public class DataRowValidatorModel
    {
        public string columnName { get; set; }
        public Type expectedType { get; set; } // use this for type matching
        public string pattern { get; set; } // use this for pattern matching or can be use in formatting 
        public bool useRegEx { get; set; } // true or false to indicate to use regular expression
    }
}
