using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	internal class InitialState : IAppState
	{
		public string StatusMessage => "フォルダが未指定です。";

		public bool IsIntervalCmbEnabled => true;
		public bool CanExecuteSetPath() => true;
		public bool CanExecuteBackup() => false;
		public bool CanExecuteStop() => false;
		public bool CanExecuteSaveAppDatas() => true;
		public bool CanExecuteLoadPrevious() => true;
		public bool CanExecuteExit() => true;

		public void ChangeState(MainWindowViewModel context)
		{
			if (context.IsFilled() && !context.IsSamePath() && !context.IsUnc())
			{
				context.CurrentState = new ReadyState();
			}
		}
	}
}
