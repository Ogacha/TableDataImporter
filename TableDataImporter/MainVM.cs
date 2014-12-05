using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TableDataImporter
{
    class MainVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void 変更を通知(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            if (SQL作成 != null) SQL作成.状態変更通知();
        }

        public 本体 Model { get; set; }

        public string データファイルパス
        {
            get { return Model.データファイルパス; }
            set { Model.データファイルパス = value; 変更を通知("データファイルパス"); }
        }

        public string テーブル名
        {
            get { return Model.テーブル名; }
            set { Model.テーブル名 = value; 変更を通知("テーブル名"); }
        }

        public string 作成ファイル名
        {
            get { return Model.作成ファイル名; }
            set { Model.作成ファイル名 = value; 変更を通知("作成ファイル名"); }
        }

        public bool 一行目除外
        {
            get { return Model.一行目除外; }
            set { Model.一行目除外 = value; 変更を通知("一行目除外"); }
        }

        public 区切り 区切り文字
        {
            get { return Model.区切り文字; }
            set { Model.区切り文字 = value; 変更を通知("区切り文字"); }
        }

        public int 全体数
        {
            get { return Model.全体数; }
            set { Model.全体数 = value; 変更を通知("全体数"); }
        }

        public int 処理済数
        {
            get { return Model.処理済数; }
            set { Model.処理済数 = value; 変更を通知("処理済数"); }
        }

        public 処理<MainVM> SQL作成 { get; set; }

        public MainVM() { SQL作成 = new SQL作成処理 { 本体 = this }; }
    }

    class SQL作成処理 : 処理<MainVM>
    {
        public override bool CanExecute(object parameter) { return File.Exists(本体.データファイルパス); }
        public override async void Execute(object parameter) { await 本体.Model.作成(); MessageBox.Show("完了しました。"); }
    }


}
