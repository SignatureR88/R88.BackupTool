using System.IO;
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
			if (string.Equals(value?.ToString(), targetValue?.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				return new ValidationResult(ErrorMessage);
			}

			return ValidationResult.Success;
		}
	}
}
