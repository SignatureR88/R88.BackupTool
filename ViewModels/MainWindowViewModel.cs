using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R88.BackupTool.Models;
using R88.BackupTool.States;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;


namespace R88.BackupTool.ViewModels
{
	internal sealed partial class MainWindowViewModel : ObservableValidator
	{
		#region Properties
		/// <summary>
		/// バックアップ元フォルダのパス
		/// </summary>
		[ObservableProperty]
		public partial string SourcePath { get; set; } = string.Empty;

		/// <summary>
		/// バックアップ先フォルダのパス
		/// </summary>
		[ObservableProperty]
		[NotifyDataErrorInfo]
		[NotMatchProperty(nameof(SourcePath), ErrorMessage = "バックアップ元と同じパスは指定できません。")]
		public partial string DestinationPath { get; set; } = string.Empty;

		/// <summary>
		/// バックアップ除外リスト
		/// </summary>
		[ObservableProperty]
		public partial ObservableCollection<ExcludeListItem> ExcludeList { get; set; }

		/// <summary>
		/// 除外リストの選択インデックス
		/// </summary>
		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(EditItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
		public partial int ExcludeListSelectedIndex { get; set; }

		/// <summary>
		/// バックアップ間隔リスト
		/// </summary>
		[ObservableProperty]
		public partial ObservableCollection<IntervalCmbItems> IntervalCmbSource { get; set; }

		/// <summary>
		/// 選択したバックアップ間隔
		/// </summary>
		[ObservableProperty]
		public partial IntervalCmbItems SelectedInterval { get; set; }

		/// <summary>
		/// 現在のState
		/// </summary>
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
		[NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(EditItemCommand))]
		[NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
		public partial IAppState CurrentState { get; set; }

		/// <summary>
		/// カウントダウンタイマーの時間
		/// </summary>
		[ObservableProperty]
		public partial string CountDownTimer { get; set; } = TimeSpan.Zero.ToString(@"hh\:mm\:ss");

		/// <summary>
		/// ステータスバーに表示するコントロール
		/// </summary>
		[ObservableProperty]
		public partial ObservableObject? CurrentSBControl { get; set; }

		/// <summary>
		/// バックアップの進捗
		/// </summary>
		[ObservableProperty]
		public partial int ProgressValue { get; set; }

		/// <summary>
		/// バックアップの進捗メッセージ
		/// </summary>
		[ObservableProperty]
		public partial string ProgressMsg { get; set; } = string.Empty;

		/// <summary>
		/// バックアップ準備中かどうか
		/// </summary>
		[ObservableProperty]
		public partial bool ProgressIsBusy { get; set; } = false;
		#endregion

		private readonly BackupToolModel _model;

		private CancellationTokenSource? _cts;

		private int _intervalCmbSelectedIndex;

		private TimeSpan _interval;

		private readonly string _appDataFilePath;
		private readonly JsonSerializerOptions _jsonOps = new() { WriteIndented = true };


		public MainWindowViewModel()
		{
			_model = new BackupToolModel();
			CurrentState = new InitialState();
			ExcludeList = [];
			ExcludeListSelectedIndex = -1;
			IntervalCmbSource = [
				new IntervalCmbItems("5分", TimeSpan.FromMinutes(5)),
				new IntervalCmbItems("10分", TimeSpan.FromMinutes(10)),
				new IntervalCmbItems("20分", TimeSpan.FromMinutes(20)),
				new IntervalCmbItems("30分", TimeSpan.FromMinutes(30)),
				new IntervalCmbItems("45分", TimeSpan.FromMinutes(45)),
				new IntervalCmbItems("1時間", TimeSpan.FromHours(1)),
				new IntervalCmbItems("2時間", TimeSpan.FromHours(2))
			];
			_intervalCmbSelectedIndex = 1;
			SelectedInterval = IntervalCmbSource[_intervalCmbSelectedIndex];
			_interval = SelectedInterval.Value;
			CurrentSBControl = new EmptyViewModel();

			// アプリケーションデータの保存先を決定する
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
				string full = value switch
				{
					_ when string.IsNullOrWhiteSpace(value) => string.Empty,
					_ when (Path.GetDirectoryName(value) == null) => value,
					_ => Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				};

				SourcePath = full;
				_model.SourcePath = full;
			}
			catch
			{
				// Path.GetFullPath が失敗した場合は受け取った値をそのまま使用する
				SourcePath = value;
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
				string full = value switch
				{
					_ when string.IsNullOrWhiteSpace(value) => string.Empty,
					_ when (Path.GetDirectoryName(value) == null) => value,
					_ => Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				};

				DestinationPath = full;
				_model.DestinationPath = full;
			}
			catch
			{
				DestinationPath = value;
				_model.DestinationPath = value;
			}

			ValidateProperty(DestinationPath, nameof(DestinationPath));

			CurrentState.ChangeState(this);
		}

		partial void OnExcludeListChanged(ObservableCollection<ExcludeListItem> value)
		{
			_model.ExcludeList = value;
		}

		partial void OnSelectedIntervalChanged(IntervalCmbItems value)
		{
			_interval = SelectedInterval.Value;
			_intervalCmbSelectedIndex = IntervalCmbSource.IndexOf(value);
		}

		/// <summary>
		/// SourcePath と DestinationPath が両方とも空でないかを確認する
		/// </summary>
		/// <returns>空でない場合はtrue、それ以外の場合はfalse</returns>
		public bool IsFilled()
		{
			return !string.IsNullOrWhiteSpace(SourcePath) && !string.IsNullOrWhiteSpace(DestinationPath);
		}

		/// <summary>
		/// SourcePath と DestinationPath が同じパスかどうかを確認する
		/// </summary>
		/// <returns>同じ場合はtrue、異なる場合はfalse</returns>
		public bool IsSamePath()
		{
			return string.Equals(SourcePath, DestinationPath, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// SourcePath または DestinationPath が UNC パスかどうかを確認する
		/// </summary>
		/// <returns>UNCパスの場合true、その他の場合false</returns>
		public bool IsUncPath()
		{
			return UncPathHelper.IsUnc(SourcePath) || UncPathHelper.IsUnc(DestinationPath);
		}

		/// <summary>
		/// 除外リストのアイテムが選択されているかどうか
		/// </summary>
		/// <returns>選択状態：true。非選択状態：false</returns>
		private bool IsSelected()
		{
			if (ExcludeListSelectedIndex != -1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// 除外リストに重複がないかを調べるメソッド
		/// </summary>
		/// <param name="filePath">比較対象</param>
		/// <param name="excludeList">除外リスト</param>
		/// <returns>重複あり：true。重複なし：false</returns>
		private static bool IsSamePath(string filePath, ObservableCollection<ExcludeListItem> excludeList)
		{
			foreach (var f in excludeList)
			{
				if (filePath.Equals(f.FilePath, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		#region Commands
		/// <summary>
		/// バックアップ元フォルダを選択するコマンド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteSetPath))]
		public void SetSourcePath()
		{
			SourcePath = BackupToolModel.GetDirectoryPath(SourcePath);
		}

		/// <summary>
		/// バックアップ先フォルダを選択するコマンド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteSetPath))]
		public void SetDestinationPath()
		{
			DestinationPath = BackupToolModel.GetDirectoryPath(DestinationPath);
		}

		/// <summary>
		/// 除外リストに追加するコマンド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteAddItem))]
		public void AddItem()
		{
			string filePath = ExcludeListModel.FileSelect(SourcePath);
			if (!string.IsNullOrWhiteSpace(filePath) && !IsSamePath(filePath, ExcludeList))
			{
				ExcludeList.Add(new ExcludeListItem { FilePath = filePath });
			}
		}

		/// <summary>
		/// 除外リストのアイテムを入れ替えるメソッド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExcuteEditItem))]
		public void EditItem()
		{
			string selectItem = ExcludeList[ExcludeListSelectedIndex].FilePath;
			string filePath = ExcludeListModel.FileSelect(selectItem, SourcePath);
			if (!string.IsNullOrWhiteSpace(filePath) && !IsSamePath(filePath, ExcludeList))
			{
				ExcludeList.Insert(ExcludeListSelectedIndex, new ExcludeListItem { FilePath = filePath });
				ExcludeList.RemoveAt(ExcludeListSelectedIndex);
			}
		}

		/// <summary>
		/// 除外リストから削除するメソッド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExcuteRemoveItem))]
		public void RemoveItem()
		{
			ExcludeList.RemoveAt(ExcludeListSelectedIndex);
		}

		/// <summary>
		/// バックアップを実行するコマンド
		/// </summary>
		/// <returns>Taskの終了</returns>
		[RelayCommand(CanExecute = nameof(CanExecuteBackup))]
		public async Task BackupRun()
		{
			CurrentState = new BackingUpState();
			CurrentSBControl = new BackupProgressViewModel();

			do
			{
				_cts = new CancellationTokenSource();
				try
				{
					ProgressIsBusy = true;
					ProgressMsg = "準備中";
					await Task.Run(() => _model.Backup(
						new Progress<int>(p => ProgressValue = p),
						new Progress<string>(m => ProgressMsg = m),
						new Progress<bool>(b => ProgressIsBusy = b))
					);

					ProgressMsg = "完了";
					ProgressValue = 100;

					// WaitStateへ遷移
					CurrentState.ChangeState(this);

					// バックアップが完了したらカウントダウンタイマーを開始する
					int timeleft = (int)_interval.TotalSeconds;
					while (timeleft >= 0)
					{
						CountDownTimer = TimeSpan.FromSeconds(timeleft).ToString(@"hh\:mm\:ss");
						await Task.Delay(1000, _cts.Token);
						timeleft--;
					}
					// BackingUpStateへ遷移
					CurrentState.ChangeState(this);
				}

				catch (TaskCanceledException)
				{
					CurrentSBControl = new EmptyViewModel();
					CurrentState = new ReadyState();
					break;
				}

				catch (Exception ex)
				{
					CurrentSBControl = new EmptyViewModel();
					CurrentState = new ReadyState();
					string title = ex switch
					{
						DirectoryNotFoundException => "Directory Not Found",
						DriveNotFoundException => "Drive Not Found",
						_ => "Error"
					};
					MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
					break;
				}
				finally
				{
					_cts.Dispose();
					_cts = null;
				}
			} while (true);
		}
		
		/// <summary>
		/// バックアップ処理を停止するコマンド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteStop))]
		public void Stop()  => _cts?.Cancel();

		/// <summary>
		/// 設定を保存するコマンド
		/// </summary>
		/// <exception cref="InvalidOperationException">保存用ファイルのパスが不正な場合スローされる</exception>
		[RelayCommand(CanExecute = nameof(CanExecuteSaveAppDatas))]
		public void SaveAppData()
		{
			_cts?.Cancel();

			var appData = new AppDatas()
			{
				DestinationPath = DestinationPath,
				SourcePath = SourcePath,
				SelectedIndex = _intervalCmbSelectedIndex,
				ExcludeList = [.. ExcludeList]
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

		/// <summary>
		/// 設定を読み込むコマンド
		/// </summary>
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
							_intervalCmbSelectedIndex = addData.SelectedIndex;
							SelectedInterval = IntervalCmbSource[_intervalCmbSelectedIndex];
						}
						ExcludeList = [.. addData.ExcludeList];
					}
				}
			}
			catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
			{
				MessageBox.Show(ex.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// アプリを終了するコマンド
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanExecuteExit))]
		public static void Exit() 
		{ 
			Application.Current.Shutdown(); 
		}

		#endregion

		// コマンドの実行可否等を決定するためのメソッド群		
		#region CanExecutes
		public string AppStatus => CurrentState.StatusMessage;
		public bool IsIntervalCmbEnabled => CurrentState.IsIntervalCmbEnabled;
		private bool CanExecuteSetPath() => CurrentState.CanExecuteSetPath();
		private bool CanExecuteBackup() => CurrentState.CanExecuteBackup();
		private bool CanExecuteStop() => CurrentState.CanExecuteStop();
		private bool CanExecuteSaveAppDatas() => CurrentState.CanExecuteSaveAppDatas();
		private bool CanExecuteLoadPrevious() => CurrentState.CanExecuteLoadPrevious();
		private bool CanExecuteExit() => CurrentState.CanExecuteExit();
		private bool CanExecuteAddItem() => CurrentState.CanExecuteAddItem();
		private bool CanExcuteEditItem() => IsSelected() && CurrentState.CanExecuteEditItem();
		private bool CanExcuteRemoveItem() => IsSelected() && CurrentState.CanExecuteRemoveItem();

		#endregion
	}
}
