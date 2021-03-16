// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Globalization;

namespace LEGOModelImporter
{

    public class LXFMLReader
    {

        public static LXFMLDoc.Brick PopulateBrick(XmlNode brickNode)
        {
            var brick = new LXFMLDoc.Brick();
            brick.designId = brickNode.Attributes["designID"].Value.Split(';')[0]; // TODO: Use version.
            brick.refId = Convert.ToInt32(brickNode.Attributes["refID"].Value);
            if (brickNode.Attributes["uuid"] != null)
            {
                brick.uuid = brickNode.Attributes["uuid"].Value;
            }

            string mats = "";

            foreach (System.Xml.XmlNode item in brickNode.SelectNodes("Part"))
            {
                mats += item.Attributes["materials"].Value + ",";
            }

            mats = mats.Remove(mats.Length - 1);

            LXFMLDoc.Brick.Part[] parts = new LXFMLDoc.Brick.Part[brickNode.SelectNodes("Part").Count];

            for (int i = 0; i < brickNode.SelectNodes("Part").Count; i++)
            {
                var partNode = brickNode.SelectNodes("Part")[i];
                parts[i] = new LXFMLDoc.Brick.Part();
                parts[i].refId = Convert.ToInt32(partNode.Attributes["refID"].Value);
                parts[i].brickDesignId = brick.designId;
                parts[i].partDesignId = partNode.Attributes["designID"].Value.Split(';')[0]; // TODO: Use version.
                parts[i].materials = ParseUtils.StringOfMaterialToMaterialArray(partNode.Attributes["materials"].Value); // TODO: Use shader id.

                var decoAttrib = partNode.Attributes["decoration"];
                if (decoAttrib != null)
                {
                    parts[i].decorations = ParseUtils.StringOfDecorationToDecorationArray(decoAttrib.Value);
                }

                var boneNodes = partNode.SelectNodes("Bone");
                parts[i].bones = new LXFMLDoc.Brick.Part.Bone[boneNodes.Count];

                foreach (XmlNode boneNode in boneNodes)
                {
                    var bone = new LXFMLDoc.Brick.Part.Bone();
                    bone.refId = Convert.ToInt32(boneNode.Attributes["refID"].Value);

                    var transformation = new Matrix4x4();
                    var mArr = ParseUtils.StringToFloatArray(boneNode.Attributes["transformation"].Value);

                    for (var j = 0; j < 4; ++j)
                    {
                        transformation.SetRow(j, new Vector4(mArr[j * 3], mArr[j * 3 + 1], mArr[j * 3 + 2], 0));
                    }

                    bone.position = GetPosition(transformation);
                    bone.rotation = GetRotation(transformation);

                    parts[i].bones[0] = bone;
                }
            }

            brick.parts = parts;
            return brick;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lxfmlDoc"></param>
        /// <param name="lxfml"></param>
        /// <returns></returns>
        public static bool ReadLxfml(XmlDocument lxfmlDoc, ref LXFMLDoc lxfml)
        {
            var success = false;

            if (lxfmlDoc != null)
            {
                try
                {
                    success = true;

                    var lxfmlNode = lxfmlDoc.SelectSingleNode("LXFML");
                    var cameraNode = lxfmlNode.SelectSingleNode("Cameras/Camera");

                    if (cameraNode != null)
                    {
                        var lxfmlCamera = new LXFMLDoc.LxfmlCamera();
                        lxfmlCamera.fov = float.Parse(cameraNode.Attributes["fieldOfView"].Value, CultureInfo.InvariantCulture);
                        lxfmlCamera.distance = float.Parse(cameraNode.Attributes["distance"].Value, CultureInfo.InvariantCulture);
                        lxfmlCamera.transformation = ParseUtils.StringToFloatArray(cameraNode.Attributes["transformation"].Value);
                        var transformation = new Matrix4x4();
                        var mArr = ParseUtils.StringToFloatArray(cameraNode.Attributes["transformation"].Value);

                        for (var i = 0; i < 4; ++i)
                        {
                            transformation.SetRow(i, new Vector4(mArr[i * 3], mArr[i * 3 + 1], mArr[i * 3 + 2], 0));
                        }

                        lxfmlCamera.position = GetPosition(transformation);
                        lxfmlCamera.rotation = GetRotation(transformation);
                        lxfml.camera = lxfmlCamera;
                    }

                    var lxfmlNameAttrib = lxfmlNode.Attributes["name"];
                    if (lxfmlNameAttrib != null)
                    {
                        lxfml.name = lxfmlNameAttrib.Value;
                    }

                    var bricksNode = lxfmlNode.SelectSingleNode("Bricks");
                    var brickNodes = bricksNode.SelectNodes("Brick");

                    foreach (XmlNode brickNode in brickNodes)
                    {
                        var brick = PopulateBrick(brickNode);
                        lxfml.bricks.Add(brick);
                    }

                    var groupsNode = lxfmlNode.SelectSingleNode("GroupSystems");

                    if (groupsNode != null)
                    {
                        var brickGroupSystemNode = groupsNode.SelectSingleNode("BrickGroupSystem"); // TODO: Handle PartGroupSystem.
                        if (brickGroupSystemNode != null)
                        {
                            var rootGroupNodes = brickGroupSystemNode.SelectNodes("Group");

                            if (rootGroupNodes.Count > 0)
                            {
                                lxfml.groups = new LXFMLDoc.BrickGroup[rootGroupNodes.Count];
                                var groupCount = 0;

                                foreach (XmlNode rootGroupNode in rootGroupNodes)
                                {
                                    var group = new LXFMLDoc.BrickGroup();
                                    var nameAttribute = rootGroupNode.Attributes["name"];
                                    if (nameAttribute != null)
                                    {
                                        group.name = nameAttribute.Value;
                                    }
                                    group.number = groupCount;

                                    lxfml.groups[groupCount++] = group;

                                    //Sub groups!
                                    if (rootGroupNode.SelectNodes("Group").Count > 0)
                                    {
                                        ReadGroupTreeRecursively(group, group, rootGroupNode);
                                    }
                                    else
                                    {
                                        var brickRefsAttribute = rootGroupNode.Attributes["brickRefs"];

                                        if (brickRefsAttribute != null)
                                        {
                                            group.brickRefs = ParseUtils.StringToIntArray(brickRefsAttribute.Value);
                                            SetGroupBricksFromBrickRefs(lxfml.bricks, group);
                                        }
                                        else
                                        {
                                            group.brickRefs = new int[0];
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e + "\t" + e.StackTrace);
                    success = false;
                }
            }

            return success;
        }

        private static void SetGroupBricksFromBrickRefs(List<LXFMLDoc.Brick> bricks, LXFMLDoc.BrickGroup group)
        {
            for (int i = 0; i < group.brickRefs.Length; ++i)
            {
                group.bricks.Add(bricks.Find((obj) => obj.refId == group.brickRefs[i]));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterParent"></param>
        /// <param name="parent"></param>
        /// <param name="parentNode"></param>
        private static void ReadGroupTreeRecursively(LXFMLDoc.BrickGroup masterParent, LXFMLDoc.BrickGroup parent, XmlNode parentNode)
        {
            parent.brickRefs = ParseUtils.StringToIntArray(parentNode.Attributes["brickRefs"].Value);

            if (masterParent != parent)
            {
                var current = new List<int>(masterParent.brickRefs);
                current.AddRange(parent.brickRefs);
                masterParent.brickRefs = current.ToArray();
            }

            var childNodes = parentNode.SelectNodes("Group");

            if (childNodes.Count > 0)
            {
                var groupCount = 0;
                parent.children = new LXFMLDoc.BrickGroup[childNodes.Count];

                foreach (XmlNode childNode in childNodes)
                {
                    var group = new LXFMLDoc.BrickGroup();
                    parent.children[groupCount++] = group;
                    group.parent = parent;
                    ReadGroupTreeRecursively(masterParent, group, childNode);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private static Vector3 GetPosition(Matrix4x4 m)
        {
            //return new Vector3(-m[3], m[7], m[11]);
            return new Vector3((float)System.Math.Round(-m[3], 2), (float)System.Math.Round(m[7], 2), (float)System.Math.Round(m[11], 2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static Quaternion GetRotation(Matrix4x4 a)
        {
            var q = new Quaternion();
            var trace = a[0, 0] + a[1, 1] + a[2, 2];
            if (trace > 0)
            {
                var s = 0.5f / Mathf.Sqrt(trace + 1.0f);
                q.w = 0.25f / s;
                q.x = (a[2, 1] - a[1, 2]) * s;
                q.y = (a[0, 2] - a[2, 0]) * s;
                q.z = (a[1, 0] - a[0, 1]) * s;
            }
            else
            {
                if (a[0, 0] > a[1, 1] && a[0, 0] > a[2, 2])
                {
                    var s = 2.0f * Mathf.Sqrt(1.0f + a[0, 0] - a[1, 1] - a[2, 2]);
                    q.w = (a[2, 1] - a[1, 2]) / s;
                    q.x = 0.25f * s;
                    q.y = (a[0, 1] + a[1, 0]) / s;
                    q.z = (a[0, 2] + a[2, 0]) / s;
                }
                else if (a[1, 1] > a[2, 2])
                {
                    var s = 2.0f * Mathf.Sqrt(1.0f + a[1, 1] - a[0, 0] - a[2, 2]);
                    q.w = (a[0, 2] - a[2, 0]) / s;
                    q.x = (a[0, 1] + a[1, 0]) / s;
                    q.y = 0.25f * s;
                    q.z = (a[1, 2] + a[2, 1]) / s;
                }
                else
                {
                    var s = 2.0f * Mathf.Sqrt(1.0f + a[2, 2] - a[0, 0] - a[1, 1]);
                    q.w = (a[1, 0] - a[0, 1]) / s;
                    q.x = (a[0, 2] + a[2, 0]) / s;
                    q.y = (a[1, 2] + a[2, 1]) / s;
                    q.z = 0.25f * s;
                }
            }
            q.x = -q.x;
            return q;
        }
    }

}