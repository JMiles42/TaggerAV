using System;

namespace TagglerAVWpf.Sites
{
	[Serializable]
	public class SiteSettings
	{
		[Serializable]
		public struct PathSetting
		{
			public bool   Multiple      { get; set; }
			public bool   EnterChildren { get; set; }
			public string NodePath      { get; set; }
		}

		public string Url { get; set; }

		public PathSetting Title           { get; set; }
		public PathSetting Year            { get; set; }
		public PathSetting PublisherStudio { get; set; }
		public PathSetting Director        { get; set; }
		public PathSetting Genre           { get; set; }
		public PathSetting ArtistActor     { get; set; }
		public PathSetting ThumbnailURL    { get; set; }
		public PathSetting ID              { get; set; }
	}
}