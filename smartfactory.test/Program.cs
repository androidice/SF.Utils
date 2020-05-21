using System;
using SF.Utils.WorkBookConverter;

namespace smartfactory.test
{
    class Program
    {
        static void Main(string[] args)
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter();
            
            // trigger default implementation to convert workbook to datatable
            workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Downloads\file_example_XLS_50.xls", 1,0,0, true);

            workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Downloads\file_example_XLSX_50.xlsx", 1, 0, 0, true);

            workBookConverter = new GuardRoomWorkBookConverter();
            workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\attendance log\guard room.xls", 0, 0, 0);
            Console.WriteLine("Hello World!");
            
            Console.ReadKey();
        }
    }
}
