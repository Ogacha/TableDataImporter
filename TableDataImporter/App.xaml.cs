using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace TableDataImporter
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        void Application_Startup(object sender, StartupEventArgs e)
        {
            var filePath = e.Args.Length == 0 ? "" : e.Args[0];
            var fileName=Path.GetFileNameWithoutExtension(filePath);
            var logic = new 本体 { データファイルパス = filePath, テーブル名 = fileName,作成ファイル名=fileName };
            logic.Run(new MainWindow());
        }
    }
}
