using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TaggerAV
{
	public static class StringHelpers
	{
		public static string FixWhitespace(this string str) => Regex.Replace(str, @"\s+", " ");

		public static string SortString(this string str)
		{
			if(string.IsNullOrEmpty(str))
				return null;

			var list = str.Trim().Trim(';').Split(';').ToList();

			for(var i = list.Count - 1; i >= 0; i--)
			{
				if(string.IsNullOrEmpty(list[i]))
					list.RemoveAt(i);
			}

			list = list.Select(s => s.Trim()).ToList();
			list.Sort(string.Compare);
			var builder = new StringBuilder();

			foreach(var s in list)
			{
				builder.Append(s);
				builder.Append("; ");
			}

			return builder.ToString().Trim();
		}
	}
}