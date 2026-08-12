namespace R88.BackupTool.ViewModels
{
	internal sealed class UncPathHelper
	{
		/// <summary>
		/// 渡されたパスがUNCパスかどうかを判定する
		/// </summary>
		/// <param name="path">対象のパス</param>
		/// <returns>UNCパス->true/ローカルパス->false</returns>
		public static bool IsUnc(string? path)
		{
			return Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? uri)
				&& uri != null
				&& uri.IsAbsoluteUri
				&& uri.IsUnc;
		}

	}
}
