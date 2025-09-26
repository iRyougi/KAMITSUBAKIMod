// Text/BookJsonRewriter.cs
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace KAMITSUBAKIMod.Text
{
    public static class BookJsonRewriter
    {
        class Book { public List<Grid> importGridList; }
        class Grid { public List<Row> rows; }
        class Row { public int rowIndex; public List<string> strings; }

        public static string RewriteChineseColumn(string json)
        {
            try
            {
                var book = JsonConvert.DeserializeObject<Book>(json);
                var grid = book?.importGridList?.FirstOrDefault();
                var header = grid?.rows?.FirstOrDefault()?.strings;
                if (header == null) return json;

                int colZh = header.FindIndex(s =>
                    string.Equals(s, "SimplifiedChinese", System.StringComparison.OrdinalIgnoreCase));
                if (colZh < 0) return json;

                for (int i = 1; i < grid.rows.Count; i++)
                {
                    var arr = grid.rows[i].strings;
                    if (arr != null && arr.Count > colZh)
                        arr[colZh] = TextBookMap.ApplySimple(arr[colZh]);
                }
                return JsonConvert.SerializeObject(book);
            }
            catch { return json; }
        }
    }
}
