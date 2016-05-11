using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TableDataImporter
{
    class 本体
    {
        // 公開プロパティー

        MainWindow _画面;
        public MainWindow 画面
        {
            get { return _画面; }
            set
            {
                _画面 = value;
                _画面.作成要求 += (s, e) => 作成();
                _画面.テーブル名 = テーブル名;
                _画面.作成ファイル名 = 作成ファイル名;
                _画面.区切り文字 = 区切り文字;
                _画面.データファイルパス = データファイルパス;
            }
        }

        string _データファイルパス="";
        public string データファイルパス
        {
            get { return _データファイルパス; }
            set { _データファイルパス = value; if (画面 != null) 画面.データファイルパス = value;}
        }

        string _テーブル名 = "";
        public string テーブル名
        {
            get { return _テーブル名; }
            set { _テーブル名 = value; if (画面 != null) 画面.テーブル名 = value; }
        }

        string _作成ファイル名 = "";
        public string 作成ファイル名
        {
            get { return _作成ファイル名; }
            set { _作成ファイル名 = value; if (画面 != null) 画面.作成ファイル名 = value; }
        }

        bool _一行目除外;
        public bool 一行目除外
        {
            get { return _一行目除外; }
            set { _一行目除外 = value; if (画面 != null) 画面.一行目除外 = value; }
        }

        区切り _区切り文字=区切り.タブ;
        public 区切り 区切り文字
        {
            get { return _区切り文字; }
            set { _区切り文字 = value; if (画面 != null) 画面.区切り文字 = value; }
        }

        public void Run(MainWindow window)
        {
            画面 = window;
            画面.Show();
        }

        async void 作成()
        {
            if (画面.データファイルパス == "") return;
            _テーブル名 = 画面.テーブル名;
            _作成ファイル名 = 画面.作成ファイルパス;
            _一行目除外 = 画面.一行目除外;
            _区切り文字 = 画面.区切り文字;
            _データファイルパス = 画面.データファイルパス;
            Encoding readEncoding;
            int 全行数;
            using (var 判定fs = File.OpenRead(データファイルパス))
            {
                var 先頭 = new byte[4];
                判定fs.Read(先頭, 0, 4);
                readEncoding = エンコード判定(先頭);
                全行数 = File.ReadLines(データファイルパス, readEncoding).Count() - (一行目除外 ? 1 : 0);
            }
            画面.進捗.Maximum = 全行数;
            var progress = new Progress<long>(p => 画面.進捗.Value=p);
            await Task.Run(() => 作成本体(progress, readEncoding, 全行数));
            画面.Close();
        }


        void 作成本体(IProgress<long> progress, Encoding 文字コード, int 全行数)
        {
            var separator = (区切り文字 == 区切り.タブ) ? "\t" : ",";

            var source = File.ReadLines(データファイルパス, 文字コード);

            var writeEncoding = Encoding.UTF8; // BOMあり。BOM がないと、SQLCMD が正しく動作しない。

            //using (var destination = new StreamWriter(作成ファイル名))
            var 分割数 = 10000;
            //var 全行数 = source.Count() - (一行目除外 ? 1 : 0);
            //画面.進捗.Maximum = 全行数;
            var stream = new FileStream(作成ファイル名.ファイル名に追加((全行数 > 分割数) ? "_001" : ""), FileMode.Create);
            var destination = new StreamWriter(stream, writeEncoding);
            progress.Report(0);
            var count = 0;
            try
            {
                foreach (var line in source.Skip(一行目除外 ? 1 : 0))
                {
                    count++;
                    progress.Report(count);
                    if (count % 分割数 == 1 && 全行数 > 分割数)
                    {
                        destination.Close();
                        stream.Close();
                        stream = new FileStream(作成ファイル名.ファイル名に追加("_" + ((count / 分割数) + 1).ToString("D3")), FileMode.Create);
                        destination = new StreamWriter(stream, writeEncoding);
                    }
                    if (string.IsNullOrWhiteSpace(line) || line.Contains("行処理されました)")) continue;
                    var newline = line.Replace("'", "''").Replace(separator, "','");
                    var sb = new StringBuilder("INSERT ");
                    sb.Append(テーブル名).Append(" VALUES('").Append(newline).Append("\');");
                    destination.WriteLine(sb.ToString().Replace("'NULL'", "NULL"));

                }
            }
            catch (Exception ex)
            {
                画面.エラー通知("ファイル作成中にエラーが発生しました。↓\r\n" + ex.ToString());
            }
            finally
            {
                if (destination != null)
                    try { destination.Close(); }
                    catch { }
                if (stream != null) stream.Close();
            }
        }

        /// <summary>
        /// BOM 必須。なければシフト JIS に強制的に解釈。
        /// </summary>
        Encoding エンコード判定(byte[] 先頭バイト)
        {
            var digit=string.Join("",先頭バイト.Select(b=>b.ToString("X2")).ToArray());
            if (digit.StartsWith("EFBBBF")) return Encoding.UTF8;
            if (digit.StartsWith("FFFE0000")) return Encoding.UTF32;
            if (digit.StartsWith("FEFF0000")) return Encoding.GetEncoding("utf-32BE");
            if (digit.StartsWith("FFFE")) return Encoding.Unicode;
            if (digit.StartsWith("FEFF")) return Encoding.BigEndianUnicode;
            return Encoding.GetEncoding("Shift_JIS");
        }


        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }
    }


}
