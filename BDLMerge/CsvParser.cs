using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace BDLMerge
{
    public static class CsvParser
    {
        private static Regex reg = new Regex(@"(?:\""(?<str>.*?)\""\;|(?<num>.*?)\;)+?", RegexOptions.Multiline | RegexOptions.Compiled);

        public static DataTable Parse(string filepath, char separator = ';')
        {
            DataTable t = new DataTable();
            string[] lines = File.ReadAllLines(filepath);

            for (int i = 0; i < lines.Length; i++)
            {
                List<string> items = new List<string>();

                var matches = reg.Matches(lines[i]);

                foreach (Match m in matches)
                {
                    if (m.Groups["str"].Length > 0)
                    {
                        items.Add(m.Groups["str"].Value);
                    }
                    else if (m.Groups["num"].Length > 0)
                    {
                        items.Add(m.Groups["num"].Value);
                    }
                    else
                    {
                        items.Add("");
                    }
                }

                if (i == 0)
                {
                    foreach (var item in items)
                    {
                        t.Columns.Add(item);
                    }
                }
                else
                {
                    var row = t.Rows.Add();

                    row.ItemArray = items.ToArray();
                }
            }

            return t;
        }
    }
}
