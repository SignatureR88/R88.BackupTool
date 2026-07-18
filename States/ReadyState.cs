using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	/// <summary>
	/// 準備完了状態クラス
	/// </summary>
	internal class ReadyState : IAppState
	{
		public string StatusMessage => "準備完了";
		public bool IsIntervalCmbEnable => true;
		public bool IsCDTimerVisible => false;
		public bool CanExecuteSetPath() => true;
		public bool CanExecuteBackup() => true;
		public bool CanExecuteStop() => false;
		public bool CanExecuteSaveAppDatas() => true;
		public bool CanExecuteLoadPrevious() => true;
		public bool CanExecuteExit() => true;

		public void ChangeState(MainWindowViewModel context)
		{
			if (context.IsSamePath() || !context.IsFilled() || context.IsUncPath())
			{
				context.CurrentState = new InitialState();
			}
			else
			{
				context.CurrentState = new BackingUpState();
			}
		}
	}
}
