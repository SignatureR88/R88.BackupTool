using Alphaleonis.Win32.Vss;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

// System.IOの代わりにAlphaFSのnamespaceを使用
using File = Alphaleonis.Win32.Filesystem.File;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using Path = Alphaleonis.Win32.Filesystem.Path;
using System.Linq.Expressions;

namespace R88.BackupTool.Models
{
	internal class BackupToolModel
	{
		/// <summary>
		/// バックアップ元のパス
		/// </summary>
		public string SourcePath { get; set; } = string.Empty;

		/// <summary>
		/// バックアップ先のパス
		/// </summary>
		public string DestinationPath { get; set; } = string.Empty;

		/// <summary>
		/// 除外対象リスト
		/// </summary>
		public ObservableCollection<ExcludeListItem> ExcludeList;

		public BackupToolModel() 
		{
			ExcludeList = [];
		}

		/// <summary>
		/// OpenFolderDialogを使用してフォルダパスを取得するメソッド
		/// </summary>
		/// <param name="path">元のパス</param>
		/// <returns>ダイアログで選択されたパスを返す。
		/// キャンセル時は元のパスを返し、元のパスがない場合は空文字列を返す。</returns>
		public static string GetDirectoryPath(string path)
		{
			var ofd = new Microsoft.Win32.OpenFolderDialog();

			if (ofd.ShowDialog() == true)
			{
				return ofd.FolderName;
			}
			else if (path != string.Empty)
			{
				return path;
			}
			else
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// バックアップを実行するメソッド
		/// ロック中のファイル対策で一時フォルダに待避後圧縮します
		/// </summary>
		/// <param name="progress">進捗状況を報告するIProgressインターフェース</param>
		/// <param name="progressMessage">進捗メッセージを報告するIProgressインターフェース</param>
		/// <exception cref="DirectoryNotFoundException">バックアップ元が存在しない時スローされる</exception>
		/// <exception cref="DriveNotFoundException">バックアップ先のドライブが存在しない時スローされる</exception>
		public void Backup(IProgress<int> progress, IProgress<string> progressMessage, IProgress<bool> isBusy)
		{
			string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			string? root = Path.GetPathRoot(DestinationPath);
			DriveInfo drive;

			try
			{
				if (!Directory.Exists(SourcePath))
				{
					throw new DirectoryNotFoundException($@" ""{SourcePath}"" is not found.");
				}
				string sourceDirectoryName = Path.GetFileName(SourcePath);
				string zipName = $"{sourceDirectoryName}_{timeStamp}.zip";
				if (root != null)
				{
					drive = new DriveInfo(root);

					if (!drive.IsReady)
					{
						string driveLetter = drive.Name;
						throw new DriveNotFoundException($@" ""{driveLetter}"" is not found.");
					}
				}

				string zipPath = Path.Combine(DestinationPath, $"{zipName}");
				string sourceFullPath = Path.GetFullPath(SourcePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string destinationFullPath = Path.GetFullPath(DestinationPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				if(destinationFullPath.StartsWith(sourceFullPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				{
					throw new IOException("バックアップ先にバックアップ元配下のフォルダが含まれています。");
				}

				if (!Directory.Exists(DestinationPath))
				{
					Directory.CreateDirectory(DestinationPath);
				}

				CreateSnapshot(sourceFullPath, zipPath, progress , progressMessage, isBusy);

			}
			finally
			{

			}
		}

		private void CreateSnapshot(string src, string zipPath, IProgress<int> progress, IProgress<string> progressMessage, IProgress<bool> isBusy)
		{
			// 対象のドライブ文字を取得
			string volumeName = Path.GetPathRoot(src);
			if(!volumeName.EndsWith('\\')) volumeName += "\\";
			try
			{
				progressMessage.Report("VSS 初期化中...");
				IVssFactory vssImplementation = VssFactoryProvider.Default.GetVssFactory();
				using IVssBackupComponents backup = vssImplementation.CreateVssBackupComponents();
				// バックアップの初期化設定
				backup.InitializeForBackup(null);
				backup.GatherWriterMetadata();
				backup.StartSnapshotSet();

				// ボリュームをスナップショットセットに追加
				Guid snapshotId = backup.AddToSnapshotSet(volumeName, Guid.Empty);

				progressMessage.Report("スナップショット作成中...");
				backup.PrepareForBackup();
				backup.DoSnapshotSet();

				VssSnapshotProperties props = backup.GetSnapshotProperties(snapshotId);
				string snapshotDevObj = props.SnapshotDeviceObject;

				string relativePath = src[volumeName.Length..];
				string snapshotFolderPath = Path.Combine(snapshotDevObj, relativePath);

				if (!snapshotFolderPath.EndsWith('\\'))
				{
					snapshotFolderPath += "\\";
				}

				isBusy.Report(false);
				CompressedWorkflow(snapshotFolderPath, zipPath, progress, progressMessage);

				// 後処理
				backup.BackupComplete();
			}
			catch (Exception)
			{
				throw;
			}
			
		}

		private void CompressedWorkflow(string srcDir, string destZip, IProgress<int> progress, IProgress<string> progressMessage)
		{
			// ファイル一覧と総バイト数
			var files = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
			var filteredFiles = ExclusionFilter(files, srcDir);
			long totalBytes = filteredFiles.Sum(f => new FileInfo(f).Length);
			if(filteredFiles.Length == 0)
			{
				progress.Report(100);
				using(ZipFile.Open(destZip, ZipArchiveMode.Create)) { }
				return;
			}

			//一時フォルダの作成
			var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempRoot);

			long copiedBytes = 0;
			long compressedBytes = 0;

			var copyProgress = new Progress<long>(b =>
			{
				copiedBytes += b;
				int percent = (int)((copiedBytes + compressedBytes) * 100 / (2 * totalBytes));
				progress.Report(Math.Min(100, percent));
			});

			var compressProgress = new Progress<long>(b =>
			{
				compressedBytes += b;
				int percent = (int)((copiedBytes + compressedBytes) * 100 / (2 * totalBytes));
				progress.Report(Math.Min(100, percent));
			});

			try
			{
				// コピーフェーズ
				progressMessage.Report("コピー中");
				foreach(var f in filteredFiles)
				{
					var rel = Path.GetRelativePath(srcDir, f);
					var destPath = Path.Combine(tempRoot, rel);
					Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
					CopyFileWithProgress(f, destPath, copyProgress);
				}

				//圧縮フェーズ
				progressMessage.Report("圧縮中");
				var tempFiles = Directory.GetFiles(tempRoot, "*", SearchOption.AllDirectories);
				using var zipFs = new FileStream(destZip, FileMode.Create, FileAccess.Write, FileShare.None);
				using var archive = new ZipArchive(zipFs, ZipArchiveMode.Create);
				foreach(var f in tempFiles)
				{
					var rel = Path.GetRelativePath(tempRoot, f);
					var entry = archive.CreateEntry(rel, CompressionLevel.Optimal);
					using var entryStream = entry.Open();
					using var fs = File.OpenRead(f);
					CopyStreamWithProgress(fs, entryStream, compressProgress);
				}
			}
			catch
			{
				try { File.Delete(destZip); } catch { }
				throw;
			}
			finally
			{
				try { Directory.Delete(tempRoot, true); } catch { }
			}
		}

		private static void CopyFileWithProgress(string src, string dest, IProgress<long> progress)
		{
			using var inFs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var outFs = File.Create(dest);
			CopyStreamWithProgress(inFs, outFs, progress);
		}

		private static void CopyStreamWithProgress(Stream input, Stream output, IProgress<long> progress)
		{
			byte[] buff = new byte[65536];
			int read;
			while((read = input.Read(buff, 0, buff.Length)) > 0)
			{
				output.Write(buff, 0, read);
				progress.Report(read);
			}
		}

		/// <summary>
		/// 除外リストのファイルを削除するメソッド
		/// スナップショットのルート(srcDir)からの相対パスを元の SourcePath に結合して
		/// 除外リストとの比較を行います。
		/// </summary>
		/// <param name="files">ファイル一覧(スナップショット上のパス)</param>
		/// <param name="srcDir">スナップショット上のルートパス</param>
		/// <returns>削除後のファイル一覧</returns>
		private string[] ExclusionFilter(string[] files, string srcDir)
		{
			if (files == null || files.Length == 0) return [];
			if (ExcludeList == null || ExcludeList.Count == 0) return files;

			var list = new List<string>(files);

			// 後ろから走査して安全に削除
			for (int i = list.Count - 1; i >= 0; i--)
			{
				string file = list[i];
				string relPath;
				try
				{
					relPath = Path.GetRelativePath(srcDir, file);
				}
				catch
				{
					// 相対化できない場合は除外しない
					continue;
				}

				// スナップショット上の相対パスを元の SourcePath に結合して比較する
				string originalPath;
				try
				{
					originalPath = Path.GetFullPath(Path.Combine(SourcePath, relPath))
						.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				}
				catch
				{
					continue;
				}

				bool excluded = false;
				foreach (var ex in ExcludeList)
				{
					if (string.Equals(originalPath, ex.FilePath, StringComparison.OrdinalIgnoreCase))
					{
						excluded = true;
						break;
					}
				}

				if (excluded)
				{
					list.RemoveAt(i);
				}
			}

			return [.. list];
		}
	}
}
