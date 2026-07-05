using System.IO;
using System.Windows;
using System.IO.Compression;
using System.Diagnostics;

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
			else if(path != string.Empty)
			{
				return path;
			}
			else 
			{
				return string.Empty;
			}
		}

		public void Backup()
		{
			string backupDirectoryName;
			string backupDirectoryPath;
			string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			string? root = Path.GetPathRoot(DestinationPath);
			DriveInfo drive;

			try
			{
				if (!Directory.Exists(SourcePath))
				{
					throw new DirectoryNotFoundException($@" ""{SourcePath}"" is not found.");
				}
				backupDirectoryName = Path.GetFileName(SourcePath) + $"_{timeStamp}";

				if (root != null)
				{
					drive = new DriveInfo(root);

					if (!drive.IsReady)
					{
						string driveLetter = drive.Name;
						throw new DriveNotFoundException($@" ""{driveLetter}"" is not found.");

					}
				}
				backupDirectoryPath = Path.Combine(DestinationPath, $"{backupDirectoryName}.zip");
				if (!Directory.Exists(DestinationPath))
				{
					Directory.CreateDirectory(DestinationPath);
				}
				//ZipFile.CreateFromDirectory(SourcePath, backupDirectoryPath, CompressionLevel.Optimal, includeBaseDirectory: false);
				Debug.Print($"{backupDirectoryPath}");
			}
			catch (DirectoryNotFoundException ex)
			{
				throw new DirectoryNotFoundException(ex.Message);
			}

			catch (DriveNotFoundException ex)
			{
				throw new DriveNotFoundException(ex.Message);

			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
	}
}
