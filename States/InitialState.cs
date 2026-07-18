using R88.BackupTool.ViewModels;

namespace R88.BackupTool.States
{
	/// <summary>
	/// 初期状態クラス
	/// </summary>
	internal class InitialState : IAppState
	{
		public string StatusMessage => "フォルダが未指定です。";
		public bool IsIntervalCmbEnable => true;
		public bool IsCDTimerVisible => false;
		public bool CanExecuteSetPath() => true;
		public bool CanExecuteBackup() => false;
		public bool CanExecuteStop() => false;
		public bool CanExecuteSaveAppDatas() => true;
		public bool CanExecuteLoadPrevious() => true;
		public bool CanExecuteExit() => true;

		public void ChangeState(MainWindowViewModel context)
		{
			if (context.IsFilled() && !context.IsSamePath() && !context.IsUncPath())
			{
				context.CurrentState = new ReadyState();
			}
		}
	}
}
