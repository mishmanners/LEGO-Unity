// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;

namespace LEGOModelImporter
{

    public static class ParseUtils
    {

        // Reads out color and shader ids from material list.
        public static LXFMLDoc.Brick.Part.Material[] StringOfMaterialToMaterialArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new LXFMLDoc.Brick.Part.Material[] { };
            }
            var split = str.Split(',');
            var result = new LXFMLDoc.Brick.Part.Material[split.Length];

            for (var i = 0; i < split.Length; ++i)
            {
                var components = split[i].Split(':');
                var material = new LXFMLDoc.Brick.Part.Material { colorId = Convert.ToInt32(components[0]), shaderId = Convert.ToInt32(components[1]) };
                result[i] = material;
            }

            return result;
        }

        // Reads out image ids and surface names from decoration list.
        public static LXFMLDoc.Brick.Part.Decoration[] StringOfDecorationToDecorationArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new LXFMLDoc.Brick.Part.Decoration[] { };
            }
            var split = str.Split(',');
            var result = new LXFMLDoc.Brick.Part.Decoration[split.Length];

            for (var i = 0; i < split.Length; ++i)
            {
                var components = split[i].Split(':');
                var decoration = new LXFMLDoc.Brick.Part.Decoration { imageId = components[0], surfaceName = components[1] };
                result[i] = decoration;
            }

            return result;
        }

        public static int[] StringToIntArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new int[] { };
            }
            var split = str.Split(',');
            var result = new int[split.Length];

            for (var i = 0; i < split.Length; ++i) result[i] = Convert.ToInt32(split[i]);

            return result;
        }

        public static float[] StringToFloatArray(string str)
        {
            var split = str.Split(',');
            var result = new float[split.Length];

            for (var i = 0; i < split.Length; ++i) result[i] = Convert.ToSingle(split[i], System.Globalization.CultureInfo.InvariantCulture);

            return result;
        }
    }

}