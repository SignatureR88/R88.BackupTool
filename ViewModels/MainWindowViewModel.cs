using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R88.BackupTool.Models;
using R88.BackupTool.States;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;


namespace R88.BackupTool.ViewModels
{
	internal partial class MainWindowViewModel : ObservableValidator
	{
		#region Properties
		[ObservableProperty]
		private string _sourcePath = string.Empty;

		[ObservableProperty]
		[NotifyDataErrorInfo]
		[NotMatchProperty(nameof(SourcePath), ErrorMessage = "バックアップ元と同じパスは指定できません。")]
		private string _destinationPath = string.Empty;

		[ObservableProperty]
		private ObservableCollection<IntervalCmbItems> _intervalCmbSource = [		
			new IntervalCmbItems("5分", TimeSpan.FromMinutes(5)),
			new IntervalCmbItems("10分", TimeSpan.FromMinutes(10)),
			new IntervalCmbItems("20分", TimeSpan.FromMinutes(20)),
			new IntervalCmbItems("30分", TimeSpan.FromMinutes(30)),
			new IntervalCmbItems("45分", TimeSpan.FromMinutes(45)),
			new IntervalCmbItems("1時間", TimeSpan.FromHours(1)),
			new IntervalCmbItems("2時間", TimeSpan.FromHours(2))
		];

		[ObservableProperty]
		private IntervalCmbItems _selectedInterval;

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(AppStatus))]
		[NotifyPropertyChangedFor(nameof(IsIntervalCmbEnabled))]
		[NotifyCanExecuteChangedFor(nameof(SetSourcePathCommand))]
		[NotifyCanExecuteChangedFor(nameof(SetDestinationPathCommand))]
		[NotifyCanExecuteChangedFor(nameof(BackupRunCommand))]
		[NotifyCanExecuteChangedFor(nameof(StopCommand))]
		[NotifyCanExecuteChangedFor(nameof(SaveAppDataCommand))]
		[NotifyCanExecuteChangedFor(nameof(LoadPreviouseCommand))]
		[NotifyCanExecuteChangedFor(nameof(ExitCommand))]
		private IAppState _currentState;
		#endregion

		private readonly BackupToolModel _model;

		private CancellationTokenSource? _cts;

		private int _selectedIndex;

		private TimeSpan _interval;

		private readonly string _appDataFilePath;
		private readonly JsonSerializerOptions _jsonOps = new() { WriteIndented = true };
		

		public MainWindowViewModel()
		{
			_model = new BackupToolModel();
			CurrentState = new InitialState();
			_selectedIndex = 1;
			SelectedInterval = IntervalCmbSource[_selectedIndex];
			_interval = SelectedInterval.Value;
			string _roming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string _productName = Assembly.GetExecutingAssembly().GetName().Name ?? "R88.BackupTool";
			string _appDataFileName = "appdata.json";
			_appDataFilePath = Path.Combine(_roming, _productName, _appDataFileName);
		}

		partial void OnSourcePathChanged(string value)
		{
			// 正規化されたフルパスをバックフィールドに格納する
			try
			{
				string full = string.IsNullOrWhiteSpace(value) ? value : 
					Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				_sourcePath = full;
				_model.SourcePath = full;
			}
			catch
			{
				// Path.GetFullPath が失敗した場合は受け取った値をそのまま使用する
				_sourcePath = value;
				_model.SourcePath = value;
			}

			// DestinationPath のバリデーションは正規化後の SourcePath を使って行う
			ValidateProperty(DestinationPath, nameof(DestinationPath));

			CurrentState.ChangeState(this);
		}

		partial void OnDestinationPathChanged(string value)
		{
			// 正規化されたフルパスをバックフィールドに格納する
			try
			{
				string full = string.IsNullOrWhiteSpace(value) ? value : 
					Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

				_destinationPath = full;
				_model.DestinationPath = full;
			}
			catch
			{
				_destinationPath = value;
				_model.DestinationPath = value;
			}

			ValidateProperty(DestinationPath, nameof(DestinationPath));

			CurrentState.ChangeState(this);
		}

		partial void OnSelectedIntervalChanged(IntervalCmbItems value)
		{
			_interval = SelectedInterval.Value;
			_selectedIndex = IntervalCmbSource.IndexOf(value);
		}

		public bool IsFilled()
		{
			return !string.IsNullOrWhiteSpace(SourcePath) && !string.IsNullOrWhiteSpace(DestinationPath);
		}

		public bool IsSamePath()
		{
			return string.Equals(SourcePath, DestinationPath, StringComparison.OrdinalIgnoreCase);
		}

		public bool IsUnc()
		{
			if (Uri.TryCreate(SourcePath, UriKind.RelativeOrAbsolute, out Uri? sourceUri) &&
				Uri.TryCreate(DestinationPath, UriKind.RelativeOrAbsolute, out Uri? destinationUri))
			{
				return (sourceUri.IsAbsoluteUri && sourceUri.IsUnc) && (destinationUri.IsAbsoluteUri && destinationUri.IsUnc);
			}
			return false;
		}

		#region Commands
		[RelayCommand(CanExecute = nameof(CanExecuteSetPath))]
		public void SetSourcePath()
		{
			SourcePath = BackupToolModel.GetDirectoryPath(SourcePath);
		}

		[RelayCommand(CanExecute = nameof(CanExecuteSetPath))]
		public void SetDestinationPath()
		{
			DestinationPath = BackupToolModel.GetDirectoryPath(DestinationPath);
		}

		[RelayCommand(CanExecute = nameof(CanExecuteBackup))]
		public async Task BackupRun() 
		{
			do
			{
				_cts = new CancellationTokenSource();
				try
				{
					CurrentState.ChangeState(this);
					await Task.Run(() => _model.Backup());
					CurrentState.ChangeState(this);
					await Task.Delay(_interval, _cts.Token);
				}
				catch (TaskCanceledException)
				{
					CurrentState = new ReadyState();
					break;
				}
				catch (DirectoryNotFoundException ex)
				{
					CurrentState = new ReadyState();
					MessageBox.Show(ex.Message, "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}

				catch (DriveNotFoundException ex)
				{
					CurrentState = new ReadyState();
					MessageBox.Show(ex.Message, "Drive Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
				catch (IOException ex)
				{
					CurrentState = new ReadyState();
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
				catch (Exception ex)
				{
					CurrentState = new ReadyState();
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
				finally
				{
					_cts.Dispose();
					_cts = null;
				}
			}while (true);
			
		}

		[RelayCommand(CanExecute = nameof(CanExecuteStop))]
		public void Stop()  => _cts?.Cancel();

		[RelayCommand(CanExecute = nameof(CanExecuteSaveAppDatas))]
		public void SaveAppData()
		{
			_cts?.Cancel();

			var appData = new AppDatas()
			{
				DestinationPath = DestinationPath,
				SourcePath = SourcePath,
				SelectedIndex = _selectedIndex
			};

			var json = JsonSerializer.Serialize(appData, _jsonOps);
			try
			{
				string appDataDir = Path.GetDirectoryName(_appDataFilePath) ?? 
					throw new InvalidOperationException("appDataFilePath が null かディレクトリ部分が取得できません。");
				if(!Directory.Exists(appDataDir))
				{
					Directory.CreateDirectory(appDataDir);
				}
				File.WriteAllText(_appDataFilePath, json);
			}
			catch (IOException ex)
			{
				MessageBox.Show(ex.Message, "File Open Failed", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch(UnauthorizedAccessException ex)
			{
				MessageBox.Show(ex.Message, "Permisson Denied", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteLoadPrevious))]
		public void LoadPreviouse()
		{
			try
			{
				if (File.Exists(_appDataFilePath))
				{
					var json = File.ReadAllText(_appDataFilePath);
					var addData = JsonSerializer.Deserialize<AppDatas>(json);

					if (addData != null)
					{
						DestinationPath = addData.DestinationPath;
						SourcePath = addData.SourcePath;
						if (addData.SelectedIndex >= 0 && addData.SelectedIndex < IntervalCmbSource.Count)
						{
							_selectedIndex = addData.SelectedIndex;
							SelectedInterval = IntervalCmbSource[_selectedIndex];
						}
					}
				}
			}
			catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
			{
				MessageBox.Show(ex.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		[RelayCommand(CanExecute = nameof(CanExecuteExit))]
		public static void Exit() 
		{ 
			Application.Current.Shutdown(); 
		} 

		#endregion

		#region CanExecutes
		public string AppStatus => CurrentState.StatusMessage;
		public bool IsIntervalCmbEnabled => CurrentState.IsIntervalCmbEnabled;
		private bool CanExecuteSetPath() => CurrentState.CanExecuteSetPath();
		private bool CanExecuteBackup() => CurrentState.CanExecuteBackup();
		private bool CanExecuteStop() => CurrentState.CanExecuteStop();
		private bool CanExecuteSaveAppDatas() => CurrentState.CanExecuteSaveAppDatas();
		private bool CanExecuteLoadPrevious() => CurrentState.CanExecuteLoadPrevious();
		private bool CanExecuteExit() => CurrentState.CanExecuteExit();
		#endregion
	}
}
