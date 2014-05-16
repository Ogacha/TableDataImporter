using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TableDataImporter
{
    public static class ExForString
    {
        /// <summary>
        /// 拡張子の前に指定した文字を割り込ませたパスを取得します。
        /// </summary>
        public static string ファイル名に追加(this string ファイル名, string 追加文字)
        {
            var dir = Path.GetDirectoryName(ファイル名);
            var originalFileName = Path.GetFileNameWithoutExtension(ファイル名);
            var extension = Path.GetExtension(ファイル名);
            return Path.Combine(dir, originalFileName + 追加文字 + extension);
        }
    }
}
