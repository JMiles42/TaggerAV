using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HtmlAgilityPack;

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

		public abstract void         Init(string     id,    string data, Action<Site> callback = null);
		public abstract void         Init(Site       other, string data, Action<Site> callback = null);
		public abstract string       GetIDUrl(string id);
		public abstract bool         Is404Error();

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

		public void Open()
		{
			Process.Start(GetIDUrl(ID));
		}

		public void Open(string id)
		{
			Process.Start(GetIDUrl(id));
		}

		public string GetIDUrl() => GetIDUrl(ID);
	}
}