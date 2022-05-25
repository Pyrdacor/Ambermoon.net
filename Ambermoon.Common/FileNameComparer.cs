using System.Collections.Generic;
using System.IO;

namespace Ambermoon
{
    public class FileNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            string xExt = Path.GetExtension(x).ToLower();
            string yExt = Path.GetExtension(y).ToLower();
            x = Path.GetFileNameWithoutExtension(x).ToLower();
            y = Path.GetFileNameWithoutExtension(y).ToLower();

            if (x.StartsWith(y))
                return 1;
            if (y.StartsWith(x))
                return -1;
            if (x == y)
                return string.Compare(xExt, yExt);

            return string.Compare(x, y);
        }
    }
}
