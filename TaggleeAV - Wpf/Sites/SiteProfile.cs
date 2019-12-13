using System;
using System.Collections.Generic;

namespace TagglerAVWpf.Sites
{
	[Serializable]
	public class SiteProfile
	{
		[Serializable]
		public struct NodePath
		{
			public string Path { get; set; }
		}

		[Serializable]
		public struct NodePathWithChildren
		{
			public byte           ChildrenDepth { get; set; }
			public bool           Merge         { get; set; }
			public List<NodePath> NodePaths     { get; set; }
		}

		public string               Url             { get; set; }
		public NodePath             Title           { get; set; }
		public NodePath             Year            { get; set; }
		public NodePathWithChildren PublisherStudio { get; set; }
		public NodePathWithChildren Director        { get; set; }
		public NodePathWithChildren Genre           { get; set; }
		public NodePathWithChildren ArtistActor     { get; set; }
		public NodePath             ThumbnailURL    { get; set; }
		public NodePath             ID              { get; set; }
	}
}