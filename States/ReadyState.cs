using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	internal class ReadyState : IAppState
	{
		public string StatusMessage => "準備完了";
		public bool IsIntervalCmbEnabled => true;
		public bool CanExecuteSetPath() => true;
		public bool CanExecuteBackup() => true;
		public bool CanExecuteStop() => false;
		public bool CanExecuteSaveAppDatas() => true;
		public bool CanExecuteLoadPrevious() => true;
		public bool CanExecuteExit() => true;

		public void ChangeState(MainWindowViewModel context)
		{
			if (context.IsSamePath())
			{
				context.CurrentState = new InitialState();
			}
		}
	}
}
