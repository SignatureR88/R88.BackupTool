using System.ComponentModel.DataAnnotations;

namespace R88.BackupTool.ViewModels
{
	internal class NotMatchPropertyAttribute(string targetProperty) : ValidationAttribute
	{
		/// <summary>
		/// 比較対象のプロパティ名
		/// </summary>
		private readonly string _targetProperty = targetProperty;

		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			var targetPropertyInfo = validationContext.ObjectType.GetProperty(_targetProperty);
			if (targetPropertyInfo == null)
			{
				return new ValidationResult($"プロパティ '{_targetProperty}' が見つかりません。");
			}
			var targetValue = targetPropertyInfo.GetValue(validationContext.ObjectInstance);
			var valueString = value?.ToString();
			var targetValueString = targetValue?.ToString();

			// 空文字やnullの場合はバリデーションをスキップする
			if (string.IsNullOrWhiteSpace(valueString) || string.IsNullOrWhiteSpace(targetValueString))
			{
				return ValidationResult.Success;
			}		

			if (string.Equals(valueString, targetValueString, StringComparison.OrdinalIgnoreCase))
			{
				return new ValidationResult(ErrorMessage);
			}

			return ValidationResult.Success;
		}

	}
}
