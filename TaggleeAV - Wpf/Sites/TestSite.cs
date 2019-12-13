using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using TagglerAVWpf;

namespace TaggerAV.Sites
{
	public class TestSite: Site
	{
		private bool   is404         = false;
		public  string Html_SiteInfo = null;

		public override void Init(string id, string data, Action<Site> callback = null)
		{
			if(FinishedDownloadingAndProcessingOfInformation)
			{
				callback?.Invoke(this);

				return;
			}

			if(string.IsNullOrEmpty(Html_SiteInfo))
			{
				Html_SiteInfo = data;

				if(string.IsNullOrEmpty(Html_SiteInfo))
				{
					callback?.Invoke(this);

					return;
				}
			}

			ID                                            = id;
			FinishedDownloadingAndProcessingOfInformation = false;

			{
				SiteDocument = new HtmlDocument();
				SiteDocument.LoadHtml(Html_SiteInfo);
				Title = FileProcesser.ProcessTitle(id, FindTitle(SiteDocument).HtmlDecode().Trim());

				{
					var pub = FindPublisherStudio(SiteDocument).HtmlDecode().Trim();
					var lab = FindLabel(SiteDocument).HtmlDecode().Trim();

					if(string.Equals(pub, lab, StringComparison.CurrentCultureIgnoreCase))
						PublisherStudio = pub;
					else
						PublisherStudio = $"{pub}; {lab}".SortString();
				}

				Director     = FindDirector(SiteDocument).HtmlDecode().Trim();
				Genre        = FindGenre(SiteDocument).HtmlDecode().Replace(',', ';').Trim();
				ArtistActor  = FindArtistActor(SiteDocument).HtmlDecode().Trim();
				ThumbnailURL = FindThumbnailURL(SiteDocument).Trim();

				if(DateTime.TryParse(FindYear(SiteDocument).HtmlDecode().Trim(), out var num))
					Year = (uint)num.Year;
			}

			Genre                                         = Genre.SortString();
			ArtistActor                                   = ArtistActor.SortString();
			FinishedDownloadingAndProcessingOfInformation = true;
			callback?.Invoke(this);
		}

		public override void Init(Site other, string data, Action<Site> callback = null)
		{
			Init(other.ID, data, callback);
		}

		private const string CUSTOM_404_ERROR = "404ERROR";

		private string GetRealURL(HtmlDocument doc)
		{
			var node = doc.DocumentNode.SelectSingleNode("//div[@class='video']");

			if(node == null)
				return CUSTOM_404_ERROR;

			return node.GetAttributeValue("id", CUSTOM_404_ERROR);
		}

		private static string FindTitle(HtmlDocument doc)
		{
			var val = doc.DocumentNode.SelectSingleNode("//div[@id='video_title']/h3[@class='post-title text']/a[@href]");

			if(val == null)
				return "";

			return val.InnerText;
		}

		private static string FindYear(HtmlDocument doc)
		{
			var path = "//div[@id='video_date']/table/tr/td[2]";
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindPublisherStudio(HtmlDocument doc)
		{
			var path = "//div[@id='video_maker']/table/tr/td[2]/span/a";
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindLabel(HtmlDocument doc)
		{
			var path = "//div[@id='video_label']/table/tr/td[2]/span/a";
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindDirector(HtmlDocument doc)
		{
			var path = "//div[@id='video_director']/table/tr/td[2]/span/a";
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindGenre(HtmlDocument doc)
		{
			var path    = "//div[@id='video_genres']//span[@id]";
			var nodes   = doc.DocumentNode.SelectNodes(path);
			var strList = new List<string>();

			return NodeListToString(nodes, strList);
		}

		private static string FindArtistActor(HtmlDocument doc)
		{
			var path    = "//div[@id='video_cast']//span[@class='star']/a";
			var nodes   = doc.DocumentNode.SelectNodes(path);
			var strList = new List<string>();

			if(nodes == null)
				return "";

			foreach(var node in nodes)
			{
				var s     = node.InnerText.FixWhitespace();
				var alias = node.ParentNode.ParentNode.ChildNodes.Where(a => a.HasClass("alias")).ToArray();

				if(alias.Length      == 0) { }
				else if(alias.Length == 1)
				{
					s = $"{s} ({alias.First().InnerText.FixWhitespace()})";
				}
				else
				{
					var sb = new StringBuilder();
					sb.Append(s);
					sb.Append(" (");

					for(var i = 0; i < alias.Length; i++)
					{
						sb.Append(alias[i].InnerText.FixWhitespace());

						if(i < alias.Length - 1)
							sb.Append(", ");
					}

					sb.Append(")");
					s = sb.ToString();
				}

				if(s != " ")
					strList.Add(s);
			}

			if(strList.Count == 0)
				return "";

			var builder = new StringBuilder();
			builder.Append(strList[0]);

			for(var i = 1; i < strList.Count; i++)
			{
				builder.Append("; ");
				builder.Append(strList[i]);
			}

			return builder.ToString();
		}

		private static string FindThumbnailURL(HtmlDocument doc)
		{
			var path = "//img[@id='video_jacket_img']";
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
			{
				return "";
			}

			var s = node.GetAttributeValue("src", "");

			if(s[0] == '/')
				s = s.Replace("//", "https://");

			return s;
		}

		public override string GetIDUrl(string id)
		{
			string str = "{0}";

			if(str.Contains("{0}"))
				return string.Format(str, id);

			return "http://www.testwebsite.com/en/search.php?keyword="+ id;
		}

		public override bool   Is404Error()        => is404;
	}

	static class extn
	{
		internal static string HtmlDecode(this string str) => System.Net.WebUtility.HtmlDecode(str);
	}
}