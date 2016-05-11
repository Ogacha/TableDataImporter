using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IO=System.IO;

namespace TableDataImporter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged//, INotifyDataErrorInfo
    {
        // 公開イベント

        public event EventHandler 作成要求;

        // 公開プロパティー

        public string データファイルパス
        {
            get { return GetProperty<string>("データファイルパス"); }
            set
            {
                SetProperty("データファイルパス", value);
                if (value.ToLower().EndsWith(".rpt")) { 一行目除外 = true; 区切り文字 = 区切り.タブ; }
                if (value.ToLower().EndsWith(".csv")) { 一行目除外 = true; 区切り文字 = 区切り.コンマ; }
                if (string.IsNullOrEmpty(テーブル名)) テーブル名 = IO::Path.GetFileNameWithoutExtension(value);
                if (string.IsNullOrEmpty(作成ファイル名)) 作成ファイル名 = IO::Path.GetFileNameWithoutExtension(value);
            }
        }

        public string 作成ファイル名
        {
            get { return GetProperty<string>("作成ファイル名"); }
            set { SetProperty("作成ファイル名", value); }
        }

        public string テーブル名
        {
            get { return GetProperty<string>("テーブル名"); }
            set { SetProperty("テーブル名", value); }
        }

        public bool 一行目除外
        {
            get { return GetProperty<bool>("一行目除外"); }
            set { SetProperty("一行目除外", value); }
        }

        public 区切り 区切り文字
        {
            get { return GetProperty<区切り>("区切り文字"); }
            set { SetProperty("区切り文字", value); }
        }
        

        public string 作成ファイルパス
        {
            get
            {
                return string.IsNullOrEmpty(データファイルパス) ? "" :
                    IO::Path.Combine(IO::Path.GetDirectoryName(データファイルパス), 作成ファイル名 + ".sql");
            }
        }


        // 公開メソッド

        public void エラー通知(string メッセージ)
        {
            MessageBox.Show(メッセージ, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        

        // 基本

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        // プロパティー機構

        public event PropertyChangedEventHandler PropertyChanged;
        Dictionary<string, object> propertyField = new Dictionary<string, object>();

        void SetProperty(string プロパティー名, object 値)
        {
            if (!propertyField.ContainsKey(プロパティー名))
            {
                propertyField.Add(プロパティー名, 値);
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(プロパティー名));
            }
            else
            {
                if (propertyField[プロパティー名] == 値) return;
                propertyField[プロパティー名] = 値;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(プロパティー名));
            }
        }

        T GetProperty<T>(string プロパティー名)
        {
            return (propertyField.ContainsKey(プロパティー名)) ? (T)propertyField[プロパティー名] : default(T);
        }

        //イベント処理

        void ファイル指定ボタン_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var dialog = new OpenFileDialog { Filter = "タブ区切りテキスト;ダブルクォートのないCSV|*.txt;*.rpt;*.csv|すべてのファイル|*.*" };
            var res = dialog.ShowDialog();
            if (res == true) データファイルパス = dialog.FileName;
        }

        void 作成ボタン_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (作成要求 != null) 作成要求(this, new EventArgs());
        }


        // エラー機構

        //ConcurrentDictionary<string, List<string>> propertyErrors = new ConcurrentDictionary<string, List<string>>();

        //public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        //public IEnumerable GetErrors(string propertyName)
        //{
        //    List<string> errors;
        //    propertyErrors.TryGetValue(propertyName, out errors);
        //    return errors;
        //}

        //public bool HasErrors
        //{
        //    get { return propertyErrors.Any(dic=>dic.Value.Any(v=>string.IsNullOrEmpty(v))); }
        //}
    }


}
