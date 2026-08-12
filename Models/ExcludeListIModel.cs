using Microsoft.Win32;

namespace R88.BackupTool.Models
{
    internal sealed class ExcludeListModel
    {
        /// <summary>
        /// 共通ダイアログでファイルを選択するメソッド
        /// </summary>
        /// <param name="current">元のファイルパス</param>
        /// <param name="root">ダイアログで開くディレクトリ</param>
        /// <returns>選択したファイルパス。キャンセル時は元のファイルパス</returns>
        public static string FileSelect(string current, string root)
        {
            // rootがnullか空の場合ドキュメント
            if(string.IsNullOrWhiteSpace(root))
            {
                root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			}

            var dlg = new OpenFileDialog()
            {
                InitialDirectory = root
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                return dlg.FileName;
            }
            else 
            {
                return current;
            }
        }

		/// <summary>
		/// 共通ダイアログでファイルを選択するメソッド
		/// </summary>
		/// <param name="root">ダイアログで開くディレクトリ</param>
		/// <returns>選択したファイルパス。キャンセル時は空</returns>
		public static string FileSelect(string root)
        {
            return FileSelect(string.Empty, root);
        }

		/// <summary>
		/// 共通ダイアログでファイルを選択するメソッド
        /// ドキュメントを開く
		/// </summary>
		/// <returns>選択したファイルパス。キャンセル時は空</returns>
		public static string FileSelect()
        {
            return FileSelect(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
    }

    /// <summary>
    /// 除外リストのアイテムクラス
    /// </summary>
    internal sealed class ExcludeListItem
    {
        public string FilePath { get; set; } = string.Empty;
    }
}
