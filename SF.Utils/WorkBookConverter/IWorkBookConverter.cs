using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.Utils.WorkBookConverter
{
    public interface IWorkBookConverter
    {
        DataTable ConvertWorkBookToDataTable(string path, int beginRow = 0, int beginCol = 0, int headerIndex = 0, bool useWorkBookTitle = false);
    }
}
