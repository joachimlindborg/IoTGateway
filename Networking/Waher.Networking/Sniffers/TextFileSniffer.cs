﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Waher.Events;

namespace Waher.Networking.Sniffers
{
	/// <summary>
	/// Outputs sniffed data to a text file.
	/// </summary>
	public class TextFileSniffer : TextWriterSniffer
	{
		private StreamWriter file;
		private readonly string fileName;
		private readonly int deleteAfterDays;

		/// <summary>
		/// Outputs sniffed data to a text file.
		/// </summary>
		/// <param name="FileName">File Name. The following strings will be replaced by current values:
		/// 
		/// %YEAR% = Current year.
		/// %MONTH% = Current month.
		/// %DAY% = Current day.
		/// %HOUR% = Current hour.
		/// %MINUTE% = Current minute.
		/// %SECOND% = Current second.
		/// 
		/// NOTE: Make sure files are stored in a separate folder, as old files will be automatically deleted.
		/// </param>
		/// <param name="BinaryPresentationMethod">How binary data is to be presented.</param>
		public TextFileSniffer(string FileName, BinaryPresentationMethod BinaryPresentationMethod)
			: this(FileName, 7, BinaryPresentationMethod)
		{
		}

		/// <summary>
		/// Outputs sniffed data to a text file.
		/// </summary>
		/// <param name="FileName">File Name. The following strings will be replaced by current values:
		/// 
		/// %YEAR% = Current year.
		/// %MONTH% = Current month.
		/// %DAY% = Current day.
		/// %HOUR% = Current hour.
		/// %MINUTE% = Current minute.
		/// %SECOND% = Current second.
		/// 
		/// NOTE: Make sure files are stored in a separate folder, as old files will be automatically deleted.
		/// </param>
		/// <param name="DeleteAfterDays">Number of days files will be kept. All files older than this
		/// in the corresponding folder will be removed. Default value is 7 days.</param>
		/// <param name="BinaryPresentationMethod">How binary data is to be presented.</param>
		public TextFileSniffer(string FileName, int DeleteAfterDays, BinaryPresentationMethod BinaryPresentationMethod)
			: base(null, BinaryPresentationMethod)
		{
			this.file = null;
			this.output = null;
			this.fileName = FileName;
			this.deleteAfterDays = DeleteAfterDays;

			string FolderName = Path.GetDirectoryName(FileName);

			if (!Directory.Exists(FolderName))
			{
				Log.Informational("Creating folder.", FolderName);
				Directory.CreateDirectory(FolderName);
			}
		}

		/// <summary>
		/// Method is called before writing something to the text file.
		/// </summary>
		protected override void BeforeWrite()
		{
			if (this.file != null)
			{
				try
				{
					this.file.Dispose();
				}
				catch (Exception)
				{
					// Ignore
				}

				this.file = null;
			}

			DateTime TP = DateTime.Now;
			string s = XmlFileSniffer.GetFileName(this.fileName, TP);

			if (File.Exists(s))
				this.output = this.file = File.AppendText(s);
			else
			{
				this.output = this.file = File.CreateText(s);

				string FolderName = Path.GetDirectoryName(s);
				string[] Files = Directory.GetFiles(FolderName, "*.*");

				foreach (string FileName in Files)
				{
					if ((DateTime.Now - File.GetLastWriteTime(FileName)).TotalDays >= this.deleteAfterDays)
					{
						try
						{
							File.Delete(FileName);
						}
						catch (Exception ex)
						{
							Log.Critical(ex);
						}
					}
				}
			}
		}

		/// <summary>
		/// Method is called after writing something to the text file.
		/// </summary>
		protected override void AfterWrite()
		{
			this.file.Dispose();
			this.file = null;
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			if (this.file != null)
			{
				this.file.Flush();
				this.file.Dispose();
				this.file = null;
			}
		}
	}
}
