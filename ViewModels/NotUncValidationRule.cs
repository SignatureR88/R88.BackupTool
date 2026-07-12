using System.Globalization;
using System.Windows.Controls;

namespace R88.BackupTool.ViewModels
{
	/// <summary>
	/// UNCパスでないことを検証するバリデーションルール
	/// </summary>
	internal class NotUncValidationRule : ValidationRule
	{
		public override ValidationResult? Validate(object value, CultureInfo cultureInfo)
		{
			var valueString = value?.ToString() ?? string.Empty;
			// UNCパスの判定
			if (UncPathHelper.IsUnc(valueString))
			{
				return new ValidationResult(false, "UNCパスはサポートされていません。");
			}

			return ValidationResult.ValidResult;
		}
	}
}
