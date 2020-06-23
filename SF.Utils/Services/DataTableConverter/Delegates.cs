using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace SF.Utils.Services.DataTableConverter
{
    //apply if you want to create stype for your specific column
    public delegate ICellStyle CreateStyle(IWorkbook workbook, ISheet sheet, string columnName = "", int columnIndex = -1);
    
    //apply if you want to have your own value at specific column that will meet your conditions
    public delegate string ValueProxy(string value, string columName);
}
