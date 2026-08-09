namespace R88.BackupTool.ViewModels
{
	/// <summary>
	/// アプリケーションの状態を表すインターフェース
	/// </summary>
	internal interface IAppState
    {
        string StatusMessage { get; }
        bool IsIntervalCmbEnabled { get => true; }
		bool CanExecuteSetPath() => true;
        bool CanExecuteBackup() => true;
        bool CanExecuteStop()=> true;
        bool CanExecuteSaveAppDatas()=> true;
        bool CanExecuteLoadPrevious()=> true;
        bool CanExecuteExit()=> true;
        bool CanExecuteAddItem() => true;
        bool CanExecuteEditItem() => true;
        bool CanExecuteRemoveItem() => true;
		void ChangeState(MainWindowViewModel context);
	}
}
