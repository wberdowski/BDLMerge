using System.Data;
using System.IO;
using System.Text;

namespace BDLMerge
{
    public static class CsvExport
    {
        public static void Export(DataTable table, string filename)
        {
            StringBuilder sb = new StringBuilder();

            foreach (DataColumn column in table.Columns)
            {
                Write(sb, column.ColumnName);
            }

            sb.AppendLine();

            foreach (DataRow row in table.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    Write(sb, item);
                }

                sb.AppendLine();
            }

            File.WriteAllText(filename, sb.ToString());
        }

        private static void Write(StringBuilder sb, object value)
        {
            if (value.GetType() == typeof(string))
            {
                sb.Append($"\"{value}\";");
            }
            else
            {
                sb.Append($"{value};");
            }
        }
    }
}
