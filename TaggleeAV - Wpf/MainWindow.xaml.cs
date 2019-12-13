using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using TaggerAV;
using TaggerAV.Sites;
using TagLib;
using File = TagLib.File;
using TextBox = System.Windows.Controls.TextBox;

namespace TagglerAVWpf
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private          ProgramData             Data               { get; }
		public           TaskScheduler           Sync               { get; set; }
		public           TagglerFile             OpenFile           { get; set; }
		public           Site                    Site               { get; set; }
		public           bool                    MoveFilesAfterSave { get; set; }
		private readonly CancellationTokenSource _close = new CancellationTokenSource();
		private          FileInfo                TempFile { get; set; }

		public MainWindow()
		{
			Data = new ProgramData();
			InitializeComponent();
			Sync = TaskScheduler.FromCurrentSynchronizationContext();
		}

		/// <inheritdoc />
		protected override void OnClosing(CancelEventArgs e)
		{
			OpenFile?.Dispose();

			if((Data.ProcessTasks > 0) || (Data.SaveTasks > 0))
			{
				if(MessageBox.Show("Force Close Application?", "This might corrupt files", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
				{
					e.Cancel = true;
					_close.Cancel();
				}
			}
			else
				_close.Cancel();
		}

		private void OnDropProcess(object sender, DragEventArgs e)
		{
			var fileList = GetFileList(e);

			if((fileList == null) || (fileList.Length == 0))
				return;

			DoProcess(fileList);
		}

		private void DoProcess(IReadOnlyCollection<string> fileList)
		{
			Interlocked.Add(ref Data.ProcessTasks, fileList.Count);
			UpdateProcessSpinner();
			Task.Run(() => Parallel.ForEach(fileList, new ParallelOptions {MaxDegreeOfParallelism = 10}, AutoProcessFile));
		}

		private void AutoProcessFile(string file)
		{
			try
			{
				using(var tfile = File.Create(file))
				{
					FileProcesser.ProcessFile(tfile);
					DoSave(tfile);
				}
			}
			catch(Exception e)
			{
				Console.WriteLine(e);

				if(MoveFilesAfterSave)
					MoveFile(file, "ERROR");
			}
			finally
			{
				Interlocked.Decrement(ref Data.ProcessTasks);
				UpdateProcessSpinner();
			}
		}

		private async void UpdateProcessSpinner()
		{
			if(Sync != TaskScheduler.Current)
				await Task.Factory.StartNew(DoProcessSpinnerUpdate, _close.Token, TaskCreationOptions.None, Sync);
			else
				DoProcessSpinnerUpdate();
		}

		private async void UpdateSavingSpinner()
		{
			if(Sync != TaskScheduler.Current)
				await Task.Factory.StartNew(DoSavingSpinnerUpdate, _close.Token, TaskCreationOptions.None, Sync);
			else
				DoSavingSpinnerUpdate();
		}

		private void DoProcessSpinnerUpdate()
		{
			if(Data.ProcessTasks == 0)
			{
				ProcessingSpinner.Visibility = Visibility.Hidden;
				lblProcessing.Content        = $"Processing Tasks Remaining: {Data.ProcessTasks}";
			}
			else
			{
				ProcessingSpinner.Visibility = Visibility.Visible;
				lblProcessing.Content        = $"Processing Tasks Remaining: {Data.ProcessTasks}";
			}
		}

		private void DoSavingSpinnerUpdate()
		{
			if(Data.SaveTasks == 0)
			{
				SavingSpinner.Visibility = Visibility.Hidden;
				lblSaving.Content        = $"Saving Tasks Remaining: {Data.SaveTasks}";
			}
			else
			{
				SavingSpinner.Visibility = Visibility.Visible;
				lblSaving.Content        = $"Saving Tasks Remaining: {Data.SaveTasks}";
			}
		}

		private readonly Queue<string> FileQue = new Queue<string>();

		private void OnDropOpen(object sender, DragEventArgs e)
		{
			var fileList = GetFileList(e);

			if((fileList == null) || (fileList.Length == 0))
				return;

			if((FileQue.Count != 0) || (OpenFile?._File != null))
			{
				foreach(var s in fileList)
					FileQue.Enqueue(s);

				return;
			}

			foreach(var s in fileList)
				FileQue.Enqueue(s);

			var fileToOpen = FileQue.Dequeue();
			OpenFileInUi(fileToOpen);
		}

		private void OpenFileInUi(string fileToOpen)
		{
			OpenFile?.Dispose();
			tbThumbnail_Url.Text = "";

			try
			{
				OpenFile = File.Create(fileToOpen);
			}
			catch
			{
				OpenFile = null;
			}

			if((OpenFile == null) || (OpenFile._File == null) || (OpenFile.Tag == null))
			{
				OnError(fileToOpen);

				if(MoveFilesAfterSave)
					MoveFile(fileToOpen, "ERROR");

				CheckForFileInQue();

				return;
			}

			Site = new TestSite
			{
					ID = Path.GetFileNameWithoutExtension(fileToOpen)
			};

			NewImageFromFile();
			UpdateUi();

			if(AutoOpen.IsChecked != true)
				return;

			Site.Open();
			UpdateUi();
		}

		private void CheckForFileInQue()
		{
			if((FileQue.Count > 0) || (OpenFile?._File != null))
				OpenFileInUi(FileQue.Dequeue());
		}

		private void UpdateUi()
		{
			UpdateImageUi();
			BindControl(tbArtist_Actor,     TextBox.TextProperty, "JoinedPerformersSort");
			BindControl(tbComment,          TextBox.TextProperty, "Comment");
			BindControl(tbFilepath,         TextBox.TextProperty, "Name", BindingMode.OneWay);
			BindControl(tbGenre,            TextBox.TextProperty, "JoinedGenresSort");
			BindControl(tbPublisher_Studio, TextBox.TextProperty, "JoinedAlbumArtists");
			BindControl(tbTitle,            TextBox.TextProperty, "Title");
			BindControl(tbYear,             TextBox.TextProperty, "Year");
		}

		private void UpdateImageUi()
		{
			imgThumbnail.Source = null;

			if((TempFile == null) || !System.IO.File.Exists(TempFile.FullName))
				return;

			var bit = new BitmapImage();
			bit.BeginInit();
			bit.UriSource   = new Uri(TempFile.FullName);
			bit.CacheOption = BitmapCacheOption.OnLoad;
			bit.EndInit();
			imgThumbnail.Source = bit;
		}

		private void ChangeCoverArt(Stream stream)
		{
			OpenFile.Tag.Pictures = new IPicture[]
			{
					new Picture
					{
							Data        = ByteVector.FromStream(stream),
							Description = "cover.jpg",
							MimeType    = "image/jpeg",
							Type        = PictureType.Other
					},
			};
		}

		private void BindControl(TextBox binder, DependencyProperty dp, string propertyPath, BindingMode mode = BindingMode.TwoWay)
		{
			var myBinding = new Binding
			{
					Source = OpenFile.Tag, Path = new PropertyPath(propertyPath), Mode = mode, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			binder.SetBinding(dp, myBinding);
		}

		private static string[] GetFileList(DragEventArgs e)
		{
			var fileArray = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			var filelist  = new List<string>();

			foreach(var file in fileArray)
			{
				if((System.IO.File.GetAttributes(file) & FileAttributes.Directory) == FileAttributes.Directory)
					filelist.AddRange(DirSearch(file));
				else
					filelist.Add(file);
			}

			return filelist.ToArray();
		}

		private static ICollection<string> DirSearch(string sDir)
		{
			var list = new List<string>();

			try
			{
				foreach(var d in Directory.GetDirectories(sDir))
				{
					foreach(var f in Directory.GetFiles(d))
						list.Add(f);

					list.AddRange(DirSearch(d));
				}

				foreach(var f in Directory.EnumerateFiles(sDir))
					list.Add(f);
			}
			catch
			{
				// ignored
			}

			return list;
		}

		private void OnRefreshThumbnail(object sender, RoutedEventArgs e)
		{
			TempFile = new FileInfo(Path.GetTempFileName());
			UpdateThumbnail(tbThumbnail_Url.Text);
		}

		private void UpdateThumbnail(string url)
		{
			if(string.IsNullOrWhiteSpace(url) || (url == "LOADED FROM FILE") || (url == "N/A"))
			{
				if(url != "LOADED FROM FILE")
					imgThumbnail.Source = null;

				return;
			}

			NewImageFromUrl(url);
		}

		private void OnSaveClicked(object sender, RoutedEventArgs e)
		{
			var file = OpenFile._File;
			DoUnloadFile();
			DoSimpleSave(file);
		}

		private void DoUnloadFile()
		{
			OpenFile             = new TagglerFile();
			imgThumbnail.Source  = null;
			TempFile             = null;
			tbThumbnail_Url.Text = "";
			UpdateUi();
			CheckForFileInQue();
		}

		private void DoUnloadAllFiles()
		{
			OpenFile             = new TagglerFile();
			imgThumbnail.Source  = null;
			TempFile             = null;
			tbThumbnail_Url.Text = "";
			FileQue.Clear();
			UpdateUi();
		}

		private async void DoSimpleSave(File file)
		{
			await Task.Factory.StartNew(() => DoSave(file), _close.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
		}

		private void DoSave(File file)
		{
			Interlocked.Increment(ref Data.SaveTasks);
			UpdateSavingSpinner();
			file.Save();

			if(MoveFilesAfterSave)
			{
				var folderName = "Done";
				var filename   = file.Name;
				file.Dispose();
				MoveFile(filename, folderName);
			}

			Interlocked.Decrement(ref Data.SaveTasks);
			UpdateSavingSpinner();
		}

		private static void MoveFile(string filename, string folderName)
		{
			var currentfile = new FileInfo(filename);
			// ReSharper disable once AssignNullToNotNullAttribute
			var newDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(filename), folderName));

			if(!newDir.Exists)
				newDir.Create();

			var newFile = Path.Combine(newDir.FullName, currentfile.Name);

			if(!currentfile.Exists)
				return;

			try
			{
				currentfile.MoveTo(newFile);
			}
			catch { }
		}

		private void OpenSiteBtn_Click(object sender, RoutedEventArgs e)
		{
			Site.Open();
		}

		private void OnError()
		{
			tbThumbnail_Url.Text = "ERROR";
			MessageBox.Show("File Error", "This file failed to load.", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void OnError(string name)
		{
			tbThumbnail_Url.Text = "ERROR";
			MessageBox.Show("File Error", $"This file failed to load {name}", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private async void PasteHtml(object sender, RoutedEventArgs e)
		{
			if(OpenFile._File == null)
			{
				OnError();

				return;
			}

			var data = Clipboard.GetText();
			await Task.Run(() => Site.Init(Path.GetFileNameWithoutExtension(OpenFile._File.Name), data)).ConfigureAwait(true);
			UpdateFileFromSite(OpenFile, Site);
			UpdateUi();
		}

		private void UpdateFileFromSite(TagglerFile openFile, Site site)
		{
			openFile.Tag.JoinedGenres = openFile.Tag.JoinedGenres.Trim().Trim(';');
			var genres = new List<string>(openFile.Tag.Genres);
			openFile.Tag.JoinedGenres = site.Genre;

			foreach(var tagGenre in openFile.Tag.Genres)
			{
				if(!genres.Contains(tagGenre))
					genres.Add(tagGenre);
			}

			openFile.Tag.Genres               = genres.ToArray();
			openFile.Tag.Title                = site.Title;
			openFile.Tag.Year                 = site.Year;
			openFile.Tag.JoinedAlbumArtists   = site.PublisherStudio;
			openFile.Tag.JoinedPerformers     = site.ArtistActor;
			openFile.Tag.JoinedGenresSort     = openFile.Tag.JoinedGenresSort;
			openFile.Tag.JoinedPerformersSort = openFile.Tag.JoinedPerformersSort;
			tbThumbnail_Url.Text              = site.ThumbnailURL;
			UpdateThumbnail(tbThumbnail_Url.Text);
		}

		private void NewImageFromUrl(string url)
		{
			imgThumbnail.Source = null;
			TempFile            = new FileInfo(Path.GetTempFileName());

			using(var tf = TempFile.Create())
			{
				var request     = System.Net.WebRequest.Create(url);
				var webResponse = request.GetResponse();

				using(var input = webResponse.GetResponseStream())
					input?.CopyTo(tf);
			}

			using(var tf = TempFile.OpenRead())
				ChangeCoverArt(tf);

			UpdateImageUi();
		}

		private void NewImageFromFile()
		{
			imgThumbnail.Source = null;

			if(OpenFile?.Tag?.FirstPictures?.Data != null)
			{
				TempFile = new FileInfo(Path.GetTempFileName());

				using(var tf = TempFile.Create())
				{
					using(var ms = new MemoryStream(OpenFile.Tag.FirstPictures.Data.Data))
						ms.CopyTo(tf);
				}
			}
			else
				TempFile = null;
		}

		private void OpenThumbnail(object sender, RoutedEventArgs e)
		{
			var argument = "/select, \"" + TempFile.FullName + "\"";
			Process.Start("explorer.exe", argument);
		}

		private void UnloadFile(object sender, RoutedEventArgs e) => DoUnloadFile();
		private void UnloadAllFiles(object sender, RoutedEventArgs e) => DoUnloadAllFiles();
	}
}