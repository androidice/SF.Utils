using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;

using System.Globalization;

namespace SF.Utils.Validators
{
    public static class DataTableColumnValidator
    {
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
                        {
                            isValid = IsDateColumnValid(row, columnModel);
                        }
                        //extend for other types and column that uses patterns
                    }

                    if (!isValid) break;
                }

                if (!isValid) break;
            }
            return isValid;
        }

        private static bool IsDateColumnValid(DataRow row, DataColumnValidatorModel columnModel)
        {
            string format = (!string.IsNullOrEmpty(columnModel.pattern)) ? columnModel.pattern : "yyyy-MM-dd HH:mm:ss";
            string columnValue = row[columnModel.columnName].ToString();

            if (string.IsNullOrEmpty(columnValue))
                return true;

            DateTime outDate;
            return DateTime.TryParseExact(columnValue, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out outDate);
        }
    }

    public class DataColumnValidatorModel
    {
        public string columnName { get; set; }
        public Type expectedType { get; set; } // use this for type matching
        public string pattern { get; set; } // use this for pattern matching or can be use in formatting 
        public bool useRegEx { get; set; } // true or false to indicate to use regular expression
    }
}
