using System.Collections.Generic;
using System.IO;

namespace TagglerAVWpf
{
	public class FileProcesser
	{
		public static void ProcessFile(TagglerFile tfile)
		{
			var id = Path.GetFileNameWithoutExtension(tfile.Tag.Name);
			tfile.Tag.Title            = ProcessTitle(id, tfile.Tag.Title);
			tfile.Tag.AlbumArtistsSort = tfile.Tag.AlbumArtistsSort.RemoveBadItems();
			tfile.Tag.ComposersSort    = tfile.Tag.ComposersSort.RemoveBadItems();
			tfile.Tag.GenresSort       = tfile.Tag.GenresSort.RemoveBadItems();
			tfile.Tag.PerformersSort   = tfile.Tag.PerformersSort.RemoveBadItems();
		}

		public static string ProcessTitle(string id, string title)
		{
			var t = title;

			if(t is null) { }
			else
			{
				t = t.Trim();

				if(t.StartsWith(id + " | "))
					title = title.Replace(id + " | ", "");
				else if(t.StartsWith(id + " || "))
					title = title.Replace(id + " || ", "");
				else if(t.StartsWith(id + " ||| "))
					title = title.Replace(id + " ||| ", "");
				else if(t.StartsWith($"[{id}]"))
					title = title.Replace($"[{id}]", "");
				else if(t.StartsWith(id))
					title = title.Replace(id, "");

				title = title.Trim();

				if(string.IsNullOrWhiteSpace(title))
					title = id;
				else
					title = $"{id} | {title}";
			}

			return title;
		}
	}

	internal static class Internals
	{
		public static string[] RemoveBadItems(this string[] ary)
		{
			if(ary is null)
				return null;

			var rtnVal = new List<string>();

			foreach(var s in ary)
			{
				if(s.Length == 0)
					continue;

				var sRst = s.Trim().Trim(';');

				if(sRst.Length == 0)
					continue;

				rtnVal.Add(sRst);
			}

			return rtnVal.ToArray();
		}
	}
}