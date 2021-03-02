// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{

	public class LXFMLDoc
	{
		public List<Brick> bricks = new List<Brick>();
		public LxfmlCamera camera;
		public BrickGroup[] groups;
		public string name;

		public class BrickGroup
		{
			public string name;
			public int number;
			public BrickGroup[] children;
			public BrickGroup parent;
			public int[] brickRefs;

			// TODO: Fill from part refs!
			public List<Brick> bricks = new List<Brick>();
		}

		public class LxfmlCamera
		{
			public float distance;
			public float fov;
			public float[] transformation;
			public Vector3 position;
			public Quaternion rotation;
		}

		public class Brick
		{
			public string designId;
			public Part[] parts;
			public int refId;
			public string uuid;

			public class Part
			{
				public class Material
				{
					public int colorId;
					public int shaderId;

					public new string ToString() { return $"{colorId}/{shaderId}"; }
				}

				public class Decoration
				{
					public string imageId;
					public string surfaceName;

					public new string ToString() { return $"{imageId}/{surfaceName}"; }
				}

				public Bone[] bones;
				public Decoration[] decorations;
				public string brickDesignId;
				public string partDesignId;
				public Material[] materials;
				public int refId;

				public class Bone
				{
					public Vector3 position;
					public int refId;
					public Quaternion rotation;
				}
			}
		}
	}

}