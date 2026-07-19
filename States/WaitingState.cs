using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	/// <summary>
	/// 待機状態クラス
	/// </summary>
	internal class WaitingState : IAppState
	{
		public string StatusMessage => "次のバックアップまで";
		public bool IsIntervalCmbEnabled => false;
		public bool IsCDTimerVisible => true;
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
