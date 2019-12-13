using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using TaggerAV.Sites;
using TagglerAVWpf;
using TagglerAVWpf.Sites;

namespace TaggerAV
{
	public abstract class Site
	{
		public          bool         FinishedDownloadingAndProcessingOfInformation = false;
		public          HtmlDocument SiteDocument;
		public          string       Title           { get; set; }
		public          uint         Year            { get; set; }
		public          string       PublisherStudio { get; set; }
		public          string       Director        { get; set; }
		public          string       Genre           { get; set; }
		public          string       ArtistActor     { get; set; }
		public          string       ThumbnailURL    { get; set; }
		public          string       ID              { get; set; }
		public abstract string       GetIDUrl(string id);
		public abstract bool         Is404Error();
		public          void         Open()          => Process.Start(GetIDUrl(ID));
		public          void         Open(string id) => Process.Start(GetIDUrl(id));
		public          string       GetIDUrl()      => GetIDUrl(ID);

		public void Init(string id, string htmlText, SiteProfile profile, Action<Site> callback = null)
		{
			SiteDocument = new HtmlDocument();
			SiteDocument.LoadHtml(htmlText);


			Title           = FileProcesser.ProcessTitle(id, FindTitle(SiteDocument, profile.Title).HtmlDecode().Trim());
			PublisherStudio = FindPublisherStudio(SiteDocument, profile.PublisherStudio).HtmlDecode().Trim();
			Director        = FindDirector(SiteDocument, profile.Director).HtmlDecode().Trim();
			Genre           = FindGenre(SiteDocument, profile.Genre).HtmlDecode().Replace(',', ';').Trim();
			ArtistActor     = FindArtistActor(SiteDocument, profile.ArtistActor).HtmlDecode().Trim();
			ThumbnailURL    = FindThumbnailURL(SiteDocument, profile.ThumbnailURL).Trim();

			if(DateTime.TryParse(FindYear(SiteDocument, profile.Year).HtmlDecode().Trim(), out var num))
				Year = (uint)num.Year;

			Genre           = Genre.SortString();
			ArtistActor     = ArtistActor.SortString();
			PublisherStudio = PublisherStudio.SortString();
		}

		private static string GetStringFromNodeSetting(HtmlDocument doc, SiteProfile.NodePath setting)
		{
			var val = doc.DocumentNode.SelectSingleNode(setting.Path);

			if(val == null)
				return "";

			return val.InnerText;
		}

		string GetStringFromNodeSetting(HtmlDocument doc, SiteProfile.NodePathWithChildren setting)
		{
			var strings = new List<string>();

			foreach(var node in setting.NodePaths)
			{
				strings.Add(GetStringFromNodeSetting(doc, node));

				if(setting.ChildrenDepth > 0)
				{
					var cNode = doc.DocumentNode.SelectSingleNode(node.Path);

					for(var i = 0; i < setting.ChildrenDepth; i++)
					{
						cNode = cNode.FirstChild;
						strings.Add(cNode.InnerHtml);
					}
				}
			}

			return strings.Sort();
		}


		private static string FindTitle(HtmlDocument doc, SiteProfile.NodePath setting)
		{
			var val = doc.DocumentNode.SelectSingleNode(setting.Path);

			if(val == null)
				return "";

			return val.InnerText;
		}

		private static string FindYear(HtmlDocument doc, SiteProfile.NodePath setting)
		{
			var path = setting.Path;
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindPublisherStudio(HtmlDocument doc, SiteProfile.NodePathWithChildren setting)
		{
			var path = setting.NodePath;
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindLabel(HtmlDocument doc, SiteProfile.NodePath setting)
		{
			var path = setting.Path;
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindDirector(HtmlDocument doc, SiteProfile.NodePathWithChildren setting)
		{
			var path = setting.NodePath;
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			return node.InnerText.FixWhitespace();
		}

		private static string FindGenre(HtmlDocument doc, SiteProfile.NodePathWithChildren setting)
		{
			var path    = setting.NodePath;
			var nodes   = doc.DocumentNode.SelectNodes(path);
			var strList = new List<string>();

			return NodeListToString(nodes, strList);
		}

		private static string FindArtistActor(HtmlDocument doc, SiteProfile.NodePathWithChildren setting)
		{
			var path    = setting.NodePath;
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
					s = $"{s} ({alias.First().InnerText.FixWhitespace()})";
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

		private static string FindThumbnailURL(HtmlDocument doc, SiteProfile.NodePath setting)
		{
			var path = setting.Path;
			var node = doc.DocumentNode.SelectSingleNode(path);

			if(node == null)
				return "";

			var s = node.GetAttributeValue("src", "");

			if(s[0] == '/')
				s = s.Replace("//", "https://");

			return s;
		}

		public static string NodeListToString(HtmlNodeCollection nodes, List<string> strList)
		{
			if(nodes == null)
				return "";

			foreach(var node in nodes)
			{
				var s = node.InnerText.FixWhitespace();

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
	}
}