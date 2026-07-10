namespace R88.BackupTool.ViewModels
{
    interface IAppState
    {
        string StatusMessage { get; }
        bool IsIntervalCmbEnabled { get => true; } 
		bool CanExecuteSetPath() => true;
        bool CanExecuteBackup() => true;
        bool CanExecuteStop()=> true;
        bool CanExecuteSaveAppDatas()=> true;
        bool CanExecuteLoadPrevious()=> true;
        bool CanExecuteExit()=> true;
		void ChangeState(MainWindowViewModel context);
	}
}
