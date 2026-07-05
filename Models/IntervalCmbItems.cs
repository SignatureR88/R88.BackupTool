namespace R88.BackupTool.Models
{
	/// <summary>
	/// ComboBoxに表示するアイテムのクラス
	/// </summary>
	/// <param name="text">コンボボックスに表示するテキスト</param>
	/// <param name="value">設定値</param>
	internal sealed class IntervalCmbItems(string? text, TimeSpan value)
	{
		public string? Text { get; } = text;

		public TimeSpan Value { get; } = value;
	}
}