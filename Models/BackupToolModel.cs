using System.IO;
using System.IO.Compression;

namespace R88.BackupTool.Models
{
	internal class BackupToolModel
	{
		public string SourcePath { get; set; } = string.Empty;

		public string DestinationPath { get; set; } = string.Empty;

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
		/// <exception cref="DirectoryNotFoundException">バックアップ元が存在しない時スローされる</exception>
		/// <exception cref="DriveNotFoundException">バックアップ先のドライブが存在しない時スローされる</exception>
		public void Backup()
		{
			string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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
				string backupName = $"{sourceDirectoryName}_{timeStamp}";
				if (root != null)
				{
					drive = new DriveInfo(root);

					if (!drive.IsReady)
					{
						string driveLetter = drive.Name;
						throw new DriveNotFoundException($@" ""{driveLetter}"" is not found.");
					}
				}
				string backupDirectoryPath = Path.Combine(DestinationPath, $"{backupName}.zip");
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

				string path = Path.Combine(tempDir, sourceDirectoryName);
				CopyDirectory(SourcePath, path);
				ZipFile.CreateFromDirectory(path, backupDirectoryPath, CompressionLevel.Optimal, includeBaseDirectory: false);
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, true);
				}
			}
		}

		/// <summary>
		/// フォルダコピーメソッド
		/// </summary>
		/// <param name="sourceDir">コピー元</param>
		/// <param name="destinationDir">コピー先</param>
		/// <exception cref="DirectoryNotFoundException">コピー元が存在しない例外</exception>
		private static void CopyDirectory(string sourceDir, string destinationDir)
		{
			var dir = new DirectoryInfo(sourceDir);
			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			
			Directory.CreateDirectory(destinationDir);
			
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			foreach (DirectoryInfo subDir in dirs)
			{
				if(subDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
				{
					continue;
				}
				string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
				CopyDirectory(subDir.FullName, newDestinationDir);
			}
			
		}
	}
}
