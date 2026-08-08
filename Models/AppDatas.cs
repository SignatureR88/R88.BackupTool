using System.Text.Json.Serialization;

namespace R88.BackupTool.Models
{
	/// <summary>
	/// 設定保存用のクラス
	/// </summary>
	internal class AppDatas
	{
		[JsonPropertyName("srcPath")]
		public string SourcePath { get; set; } = string.Empty;
		[JsonPropertyName("destPath")]
		public string DestinationPath { get; set; } = string.Empty;
		[JsonPropertyName("selectedIndex")]
		public int SelectedIndex { get; set; }
		[JsonPropertyName("exclusionList")]
		public List<ExcludeListItem> ExcludeList { get; set; } = [];
	}
}