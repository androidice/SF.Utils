using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.Utils.SFWorkBook
{
    public interface ISFWorkBook
    {
        IWorkbook ReadWorkBook(string path);
        string GetCellValue(ICell cell);
    }
}
