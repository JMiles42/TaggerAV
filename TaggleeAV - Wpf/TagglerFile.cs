using System;
using TagLib;

namespace TagglerAVWpf
{
	public class TagglerFile: IDisposable
	{
		public static implicit operator TagglerFile(File input) => new TagglerFile(input);
		public                          File       _File        { get; private set; }
		public                          TagglerTag Tag          { get; private set; }

		public TagglerFile()
		{
			_File = null;
			Tag   = null;
		}

		public TagglerFile(File input)
		{
			_File = input;
			Tag   = new TagglerTag(this);
		}

		public class TagglerTag
		{
			private TagglerFile owner { get; }
			private File        _File => owner._File;
			public TagglerTag(TagglerFile _owner) => owner = _owner;
			public string   Name     => _File.Name;
			public TagTypes TagTypes => _File.TagTypes;
			public string Title
			{
				get => _File.Tag.Title;
				set => _File.Tag.Title = value;
			}
			public string[] Performers
			{
				get => _File.Tag.Performers;
				set => _File.Tag.Performers = value;
			}
			public string[] PerformersSort
			{
				get
				{
					var sorted = new string[_File.Tag.Performers.Length];
					_File.Tag.Performers.CopyTo(sorted, 0);
					Array.Sort(sorted, StringComparer.InvariantCulture);

					return sorted;
				}
				set => _File.Tag.Performers = value;
			}
			public string[] PerformersRole
			{
				get => _File.Tag.Performers;
				set => _File.Tag.Performers = value;
			}
			public string[] AlbumArtists
			{
				get => _File.Tag.AlbumArtists;
				set => _File.Tag.AlbumArtists = value;
			}
			public string[] AlbumArtistsSort
			{
				get
				{
					var sorted = new string[_File.Tag.AlbumArtists.Length];
					_File.Tag.AlbumArtists.CopyTo(sorted, 0);
					Array.Sort(sorted, StringComparer.InvariantCulture);

					return sorted;
				}
				set => _File.Tag.AlbumArtists = value;
			}
			public string[] Composers
			{
				get => _File.Tag.Composers;
				set => _File.Tag.Composers = value;
			}
			public string[] ComposersSort
			{
				get
				{
					var sorted = new string[_File.Tag.Composers.Length];
					_File.Tag.Composers.CopyTo(sorted, 0);
					Array.Sort(sorted, StringComparer.InvariantCulture);

					return sorted;
				}
				set => _File.Tag.Composers = value;
			}
			public string Album
			{
				get => _File.Tag.Album;
				set => _File.Tag.Album = value;
			}
			public string Comment
			{
				get => _File.Tag.Comment;
				set => _File.Tag.Comment = value;
			}
			public string[] Genres
			{
				get => _File.Tag.Genres;
				set => _File.Tag.Genres = value;
			}
			public string[] GenresSort
			{
				get
				{
					var sorted = new string[_File.Tag.Genres.Length];
					_File.Tag.Genres.CopyTo(sorted, 0);
					Array.Sort(sorted, StringComparer.InvariantCulture);

					return sorted;
				}
				set => _File.Tag.Genres = value;
			}
			public uint Year
			{
				get => _File.Tag.Year;
				set => _File.Tag.Year = value;
			}
			public uint Track
			{
				get => _File.Tag.Track;
				set => _File.Tag.Track = value;
			}
			public uint TrackCount
			{
				get => _File.Tag.TrackCount;
				set => _File.Tag.TrackCount = value;
			}
			public uint Disc
			{
				get => _File.Tag.Disc;
				set => _File.Tag.Disc = value;
			}
			public uint DiscCount
			{
				get => _File.Tag.DiscCount;
				set => _File.Tag.DiscCount = value;
			}
			public string Lyrics
			{
				get => _File.Tag.Lyrics;
				set => _File.Tag.Lyrics = value;
			}
			public string Grouping
			{
				get => _File.Tag.Grouping;
				set => _File.Tag.Grouping = value;
			}
			public uint BeatsPerMinute
			{
				get => _File.Tag.BeatsPerMinute;
				set => _File.Tag.BeatsPerMinute = value;
			}
			public string Conductor
			{
				get => _File.Tag.Conductor;
				set => _File.Tag.Conductor = value;
			}
			public string Copyright
			{
				get => _File.Tag.Copyright;
				set => _File.Tag.Copyright = value;
			}
			public string AmazonId
			{
				get => _File.Tag.AmazonId;
				set => _File.Tag.AmazonId = value;
			}
			public IPicture[] Pictures
			{
				get => _File.Tag.Pictures;
				set => _File.Tag.Pictures = value;
			}
			public IPicture FirstPictures
			{
				get { return FirstInGroup(Pictures); }
			}
			public string JoinedAlbumArtists
			{
				get => _File.Tag.JoinedAlbumArtists;
				set => _File.Tag.AlbumArtists = SplitGroup(value);
			}
			public string JoinedPerformers
			{
				get => _File.Tag.JoinedPerformers;
				set => _File.Tag.Performers = SplitGroup(value);
			}
			public string JoinedPerformersSort
			{
				get => JoinGroup(PerformersSort);
				set => _File.Tag.Performers = SplitGroup(value);
			}
			public string JoinedComposers
			{
				get => _File.Tag.JoinedComposers;
				set => _File.Tag.Composers = SplitGroup(value);
			}
			public string JoinedGenresSort
			{
				get => JoinGroup(GenresSort);
				set => _File.Tag.Genres = SplitGroup(value);
			}
			public string JoinedGenres
			{
				get => _File.Tag.JoinedGenres;
				set => _File.Tag.Genres = SplitGroup(value);
			}
			private static T FirstInGroup<T>(T[] group) where T: class => group == null || group.Length == 0? null : group[0];

			private static string JoinGroup(string[] group)
			{
				if(group == null || group.Length == 0)
					return null;

				return string.Join("; ", group);
			}

			private static string[] SplitGroup(string single)
			{
				if(single == null || single.Length == 0)
					return Array.Empty<string>();

				return single.Trim().Trim(';').Split(';');
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_File?.Dispose();
		}
	}
}