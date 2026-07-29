using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	/// <summary>
	/// バックアップ中状態クラス
	/// </summary>
	internal class BackingUpState : IAppState
	{
		public string StatusMessage => "バックアップ中...";
		public bool IsIntervalCmbEnabled => false;
		public bool CanExecuteSetPath() => false;
		public bool CanExecuteBackup() => false;
		public bool CanExecuteStop() => false;
		public bool CanExecuteSaveAppDatas() => false;
		public bool CanExecuteLoadPrevious() => false;
		public bool CanExecuteExit() => false;
		public bool CanExecuteAddItem() => false;
		public bool CanExecuteEditItem() => false;
		public bool CanExecuteRemoveItem() => false;


		public void ChangeState(MainWindowViewModel context)
		{
			context.CurrentState = new WaitingState();
			context.CurrentSBControl = new CDTimerViewModel();
		}
	}
	
}
