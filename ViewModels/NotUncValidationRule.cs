using System.Globalization;
using System.Windows.Controls;

namespace R88.BackupTool.ViewModels
{
	internal class NotUncValidationRule : ValidationRule
	{
		public override ValidationResult? Validate(object value, CultureInfo cultureInfo)
		{
			var valueString = value.ToString() ?? string.Empty;
			// UNCパスの判定
			if (IsUncPath(valueString))
			{
				return new ValidationResult(false, "UNCパスはサポートされていません。");
			}

			return ValidationResult.ValidResult;
		}

		/// <summary>
		/// 渡されたパスがUNCパスかどうかを判定する
		/// </summary>
		/// <param name="path">対象のパス</param>
		/// <returns>UNCパス->true/ローカルパス->false</returns>
		private static bool IsUncPath(string? path)
		{
			if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri? valueUriResult))
			{
				return valueUriResult.IsAbsoluteUri && valueUriResult.IsUnc;
			}

			return false;
		}

	}
}
