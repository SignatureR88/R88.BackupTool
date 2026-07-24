using System.Diagnostics;
using System.IO;
using System.IO.Compression;

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
		/// <exception cref="DirectoryNotFoundException">バックアップ元が存在しない時スローされる</exception>
		/// <exception cref="DriveNotFoundException">バックアップ先のドライブが存在しない時スローされる</exception>
		public void Backup(IProgress<int> progress, string progMsg)
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

				CompressedWorkflow(sourceFullPath, zipPath, progress, progMsg);

			}
			finally
			{

			}
		}


		private static void CompressedWorkflow(string srcDir, string destZip, IProgress<int> progress, string progMsg)
		{
			// ファイル一覧と総バイト数
			var files = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
			long totalBytes = files.Sum(f => new FileInfo(f).Length);
			if(totalBytes == 0)
			{
				progress.Report(100);
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
				progMsg = "コピー中";
				foreach(var f in files)
				{
					var rel = Path.GetRelativePath(srcDir, f);
					var destPath = Path.Combine(tempRoot, rel);
					Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
					CopyFileWithProgress(f, destPath, copyProgress);
				}

				//圧縮フェーズ
				progMsg = "圧縮中";
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
			finally
			{
				try { Directory.Delete(tempRoot, true); } catch { }
			}
		}

		private static void CopyFileWithProgress(string src, string dest, IProgress<long> progress)
		{
			using var inFs = File.OpenRead(src);
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
	}
}
