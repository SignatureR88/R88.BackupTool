using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	internal class WaitingState : IAppState
	{
		public string StatusMessage => "待機中";
		public bool IsIntervalCmbEnabled => false;
		public bool CanExecuteSetPath() => false;
		public bool CanExecuteBackup() => false;
		public bool CanExecuteStop() => true;
		public bool CanExecuteSaveAppDatas() => true;
		public bool CanExecuteLoadPrevious() => false;
		public bool CanExecuteExit() => true;

		public void ChangeState(MainWindowViewModel context)
		{
			context.CurrentState = new BackingUpState();
		}
	}
}
