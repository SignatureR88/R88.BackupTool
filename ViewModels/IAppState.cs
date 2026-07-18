namespace R88.BackupTool.ViewModels
{
	/// <summary>
	/// アプリケーションの状態を表すインターフェース
	/// </summary>
	interface IAppState
    {
        string StatusMessage { get; }
        bool IsIntervalCmbEnable { get => true; }
        bool IsCDTimerVisible { get => true; }
		bool CanExecuteSetPath() => true;
        bool CanExecuteBackup() => true;
        bool CanExecuteStop()=> true;
        bool CanExecuteSaveAppDatas()=> true;
        bool CanExecuteLoadPrevious()=> true;
        bool CanExecuteExit()=> true;
		void ChangeState(MainWindowViewModel context);
	}
}
