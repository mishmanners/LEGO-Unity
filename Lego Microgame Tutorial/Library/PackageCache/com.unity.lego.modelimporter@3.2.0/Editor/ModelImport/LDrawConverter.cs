// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using LEGOMaterials;

namespace LEGOModelImporter
{

    public static class LDrawConverter
    {
        private class Brick
        {
            public string designID;
            public List<Part> parts = new List<Part>();
        }

        private class Part
        {
            public Matrix4x4 transformation; // Only output one bone per part.
            public string designID;
            public string materialID;
        }

        private class AssemblyPart
        {
            public Matrix4x4 transformation;
            public string ldrawID;
            public string ldrawMaterialID;
        }

        private class SubModelReference
        {
            public Matrix4x4 transformation;
            public string name;
            public int referenceNumber;
        }

        private class SubModel
        {
            public string name;
            public List<Brick> bricks = new List<Brick>();
            public List<SubModelReference> subModelReferences = new List<SubModelReference>();
        };

        private static List<SubModel> subModels;

        private static int currentBrickRefId;
        private static int currentPartRefId;
        private static SubModel currentlyParsingSubModel;
        private static Dictionary<string, int> subModelReferenceCount;
        private static Dictionary<string, List<int>> brickGroups;
        private static string mainModelName;


        private static Dictionary<string, string> LDrawMaterialToLEGOMaterial;
        private static Dictionary<string, string> LDrawBrickToLEGOBrick;
        private static Dictionary<string, Matrix4x4> LDrawBrickToTransformation;
        private static Dictionary<string, List<AssemblyPart>> LDrawAssembly;

        private static List<string> missingSubModels;
        private static List<string> missingAssemblies;
        private static List<string> missingDecorations;
        private static List<string> missingStickers;
        private static List<string> missingMaterials;
        private static List<string> legacyMaterials;
        private static List<string> missingParts;
        private static List<string> changedParts;
        private static List<string> legacyParts;
        private static List<string> missingTransformations;

        public static XmlDocument ConvertLDrawToLXFML(Stream ldrawInput, string name)
        {
            // Reset state.
            currentBrickRefId = 0;
            currentPartRefId = 0;
            subModels = new List<SubModel>();
            currentlyParsingSubModel = null;
            subModelReferenceCount = new Dictionary<string, int>();
            brickGroups = new Dictionary<string, List<int>>();

            missingSubModels = new List<string>();
            missingAssemblies = new List<string>();
            missingDecorations = new List<string>();
            missingStickers = new List<string>();
            missingMaterials = new List<string>();
            legacyMaterials = new List<string>();
            missingParts = new List<string>();
            changedParts = new List<string>();
            legacyParts = new List<string>();
            missingTransformations = new List<string>();

            // Read LDraw to LXFML mapping file.
            var mappingText = File.ReadAllText("Packages/com.unity.lego.modelimporter/Data/ldraw.xml");
            var mappingXml = new XmlDocument();
            mappingXml.LoadXml(mappingText);
            var mappingRoot = mappingXml.DocumentElement;

            LDrawMaterialToLEGOMaterial = new Dictionary<string, string>();
            var materialNodes = mappingRoot.SelectNodes("Material");
            foreach (XmlNode materialNode in materialNodes)
            {
                if (LDrawMaterialToLEGOMaterial.ContainsKey(materialNode.Attributes["ldraw"].Value))
                {
                    Debug.LogWarning($"Duplicate material mapping: {materialNode.Attributes["ldraw"].Value}. Overwriting old value {LDrawMaterialToLEGOMaterial[materialNode.Attributes["ldraw"].Value]}");
                }
                LDrawMaterialToLEGOMaterial[materialNode.Attributes["ldraw"].Value] = materialNode.Attributes["lego"].Value;
            }

            LDrawBrickToLEGOBrick = new Dictionary<string, string>();
            var brickNodes = mappingRoot.SelectNodes("Brick");
            foreach (XmlNode brickNode in brickNodes)
            {
                if (LDrawBrickToLEGOBrick.ContainsKey(brickNode.Attributes["ldraw"].Value))
                {
                    Debug.LogWarning($"Duplicate brick mapping: {brickNode.Attributes["ldraw"].Value}. Overwriting old value {LDrawBrickToLEGOBrick[brickNode.Attributes["ldraw"].Value]}");
                }
                LDrawBrickToLEGOBrick[brickNode.Attributes["ldraw"].Value] = brickNode.Attributes["lego"].Value;
            }

            LDrawBrickToTransformation = new Dictionary<string, Matrix4x4>();
            var transformationNodes = mappingRoot.SelectNodes("Transformation");
            foreach (XmlNode transformationNode in transformationNodes)
            {
                Matrix4x4 transformation = Matrix4x4.TRS(
                    new Vector3(
                        float.Parse(transformationNode.Attributes["tx"].Value, CultureInfo.InvariantCulture),
                        float.Parse(transformationNode.Attributes["ty"].Value, CultureInfo.InvariantCulture),
                        float.Parse(transformationNode.Attributes["tz"].Value, CultureInfo.InvariantCulture)
                        ),
                    Quaternion.AngleAxis(float.Parse(transformationNode.Attributes["angle"].Value, CultureInfo.InvariantCulture) * Mathf.Rad2Deg,
                    new Vector3(
                        float.Parse(transformationNode.Attributes["ax"].Value, CultureInfo.InvariantCulture),
                        float.Parse(transformationNode.Attributes["ay"].Value, CultureInfo.InvariantCulture),
                        float.Parse(transformationNode.Attributes["az"].Value, CultureInfo.InvariantCulture)
                        )),
                    Vector3.one
                    );

                if (!LDrawBrickToTransformation.ContainsKey(transformationNode.Attributes["ldraw"].Value))
                {
                    LDrawBrickToTransformation.Add(transformationNode.Attributes["ldraw"].Value, transformation);

                    // Also add transformation to mapped ID if not present already.
                    if (LDrawBrickToLEGOBrick.ContainsKey(transformationNode.Attributes["ldraw"].Value) &&
                        !LDrawBrickToTransformation.ContainsKey(LDrawBrickToLEGOBrick[transformationNode.Attributes["ldraw"].Value] + ".dat"))
                    {
                        LDrawBrickToTransformation.Add(LDrawBrickToLEGOBrick[transformationNode.Attributes["ldraw"].Value] + ".dat", transformation);
                    }
                }
                else
                {
                    // Check if existing transformation is the same.
                    var existingTransformation = LDrawBrickToTransformation[transformationNode.Attributes["ldraw"].Value];
                    if (!Mathf.Approximately(existingTransformation.m00, transformation.m00) || !Mathf.Approximately(existingTransformation.m10, transformation.m10) ||
                        !Mathf.Approximately(existingTransformation.m20, transformation.m20) || !Mathf.Approximately(existingTransformation.m30, transformation.m30) ||
                        !Mathf.Approximately(existingTransformation.m01, transformation.m01) || !Mathf.Approximately(existingTransformation.m11, transformation.m11) ||
                        !Mathf.Approximately(existingTransformation.m21, transformation.m21) || !Mathf.Approximately(existingTransformation.m31, transformation.m31) ||
                        !Mathf.Approximately(existingTransformation.m02, transformation.m02) || !Mathf.Approximately(existingTransformation.m12, transformation.m12) ||
                        !Mathf.Approximately(existingTransformation.m22, transformation.m22) || !Mathf.Approximately(existingTransformation.m32, transformation.m32) ||
                        !Mathf.Approximately(existingTransformation.m03, transformation.m03) || !Mathf.Approximately(existingTransformation.m13, transformation.m13) ||
                        !Mathf.Approximately(existingTransformation.m23, transformation.m23) || !Mathf.Approximately(existingTransformation.m33, transformation.m33)
                        )
                    {
                        Debug.LogWarning($"Duplicate transformation: {transformationNode.Attributes["ldraw"].Value}. Values differ! Keeping old value");
                    }

                }
            }

            // Read LDraw assembly mapping file.
            var assemblyMappingText = File.ReadAllText("Packages/com.unity.lego.modelimporter/Data/ldrawassembly.xml");
            var assemblyMappingXml = new XmlDocument();
            assemblyMappingXml.LoadXml(assemblyMappingText);
            var assemblyMappingRoot = assemblyMappingXml.DocumentElement;

            LDrawAssembly = new Dictionary<string, List<AssemblyPart>>();
            var assemblyNodes = assemblyMappingRoot.SelectNodes("Assembly");
            foreach (XmlNode assemblyNode in assemblyNodes)
            {
                var partList = new List<AssemblyPart>();

                var partNodes = assemblyNode.SelectNodes("Part");
                foreach (XmlNode partNode in partNodes)
                {
                    Matrix4x4 transformation = Matrix4x4.TRS(
                    new Vector3(
                        float.Parse(partNode.Attributes["tx"].Value, CultureInfo.InvariantCulture),
                        float.Parse(partNode.Attributes["ty"].Value, CultureInfo.InvariantCulture),
                        float.Parse(partNode.Attributes["tz"].Value, CultureInfo.InvariantCulture)
                        ),
                    Quaternion.AngleAxis(float.Parse(partNode.Attributes["angle"].Value, CultureInfo.InvariantCulture) * Mathf.Rad2Deg,
                    new Vector3(
                        float.Parse(partNode.Attributes["ax"].Value, CultureInfo.InvariantCulture),
                        float.Parse(partNode.Attributes["ay"].Value, CultureInfo.InvariantCulture),
                        float.Parse(partNode.Attributes["az"].Value, CultureInfo.InvariantCulture)
                        )),
                    Vector3.one
                    );

                    var part = new AssemblyPart()
                    {
                        transformation = transformation,
                        ldrawID = partNode.Attributes["ldraw"].Value,
                        ldrawMaterialID = partNode.Attributes["ldrawMaterial"].Value
                    };

                    partList.Add(part);
                }

                LDrawAssembly.Add(assemblyNode.Attributes["ldraw"].Value, partList);
            }

            // Parse LDraw stream.
            var reader = new StreamReader(ldrawInput);

            while (reader.Peek() > 0)
            {
                ParseLDrawLine(reader.ReadLine());
            }

            // Build LXFML file.
            var doc = new XmlDocument();

            doc.CreateXmlDeclaration("1.0", "UTF-8", "no");

            var LXFML = doc.CreateElement("LXFML");
            LXFML.SetAttribute("versionMajor", "5");
            LXFML.SetAttribute("versionMinor", "6");
            LXFML.SetAttribute("name", name);
            doc.AppendChild(LXFML);

            var meta = doc.CreateElement("Meta");
            LXFML.AppendChild(meta);

            var application = doc.CreateElement("Application");
            application.SetAttribute("name", "LDraw Converter");
            application.SetAttribute("versionMajor", "0");
            application.SetAttribute("versionMinor", "1");
            meta.AppendChild(application);

            var bricks = doc.CreateElement("Bricks");
            LXFML.AppendChild(bricks);

            OutputBrickLXFML(subModels[0], 0, Matrix4x4.identity, doc, bricks);

            // Add groups.
            var groupSystems = doc.CreateElement("GroupSystems");
            LXFML.AppendChild(groupSystems);

            var brickGroupSystem = doc.CreateElement("BrickGroupSystem");
            brickGroupSystem.SetAttribute("isHierarchical", "false"); // Assume non-hierarchical grouping.
            brickGroupSystem.SetAttribute("isUnique", "true");
            groupSystems.AppendChild(brickGroupSystem);

            OutputGroupLXFML(brickGroups, doc, brickGroupSystem);

            //Debug.Log(doc.OuterXml);

            return doc;
        }

        public static Dictionary<string, List<string>> GetErrors()
        {
            var errors = new Dictionary<string, List<string>>();

            if (missingSubModels.Count > 0)
            {
                errors.Add("The following groups were referenced but missing from the file. They cannot be imported.", missingSubModels);
            }
            if (missingAssemblies.Count > 0)
            {
                errors.Add("The following multi-part bricks are not supported. Replace with individual bricks, or add them to ldrawassembly.xml.", missingAssemblies);
            }
            if (missingDecorations.Count > 0)
            {
                errors.Add("The following bricks contain decorations. Importing decorations is not supported. Add them manually after import.", missingDecorations);
            }
            if (missingStickers.Count > 0)
            {
                errors.Add("The following bricks contain stickers. Stickers are not supported.", missingStickers);
            }
            if (missingMaterials.Count > 0)
            {
                errors.Add("The following materials are not supported. Use alternative materials, or add mapping to known materials in ldraw.xml.", missingMaterials);
            }
            if (legacyMaterials.Count > 0)
            {
                errors.Add("The following materials are legacy materials. Use alternative materials if possible.", legacyMaterials);
            }
            if (missingParts.Count > 0)
            {
                errors.Add("The following bricks are not supported. Use alternative bricks, or add mapping to known bricks in ldraw.xml.", missingParts);
            }
            if (changedParts.Count > 0)
            {
                errors.Add("The following bricks are not supported. They will be changed to alternative bricks.", changedParts);
            }
            if (legacyParts.Count > 0)
            {
                errors.Add("The following bricks are legacy bricks. Colliders and connectivity information might not be available. Use alternative parts if possible.", legacyParts);
            }

            if (missingTransformations.Count > 0)
            {
                errors.Add("The following bricks might be placed incorrectly. Use alternative bricks, or add transformation information to ldraw.xml.", missingTransformations);
            }

            return errors;
        }

        private static void ParseLDrawLine(string line)
        {
            // Bricks and submodel references start with a 1
            if (line.Length > 0 && line[0] == '1')
            {
                var tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // First 3 columns are rotation. Flip the rotation on the x-axis.
                var matrix = new Matrix4x4(
                        new Vector4(float.Parse(tokens[5], CultureInfo.InvariantCulture), float.Parse(tokens[6], CultureInfo.InvariantCulture), float.Parse(tokens[7], CultureInfo.InvariantCulture), 0.0f),
                        new Vector4(float.Parse(tokens[8], CultureInfo.InvariantCulture), float.Parse(tokens[9], CultureInfo.InvariantCulture), float.Parse(tokens[10], CultureInfo.InvariantCulture), 0.0f),
                        new Vector4(float.Parse(tokens[11], CultureInfo.InvariantCulture), float.Parse(tokens[12], CultureInfo.InvariantCulture), float.Parse(tokens[13], CultureInfo.InvariantCulture), 0.0f),
                        new Vector4(0.0f, 0.0f, 0.0f, 1.0f)
                        );
                var rotation = matrix.rotation;
                rotation.x *= -1.0f;

                // Final column is translation. Convert from LDraw units to cm and flip y- and z-axes.
                var position = new Vector4(float.Parse(tokens[2], CultureInfo.InvariantCulture) * 0.04f, float.Parse(tokens[3], CultureInfo.InvariantCulture) * -0.04f, float.Parse(tokens[4], CultureInfo.InvariantCulture) * -0.04f, 1.0f);

                // Construct the modified matrix.
                var transformation = Matrix4x4.TRS(position, rotation, Vector3.one);

                // Parse out material ID.
                var materialID = tokens[1];
                if (LDrawMaterialToLEGOMaterial.ContainsKey(tokens[1]))
                {
                    materialID = LDrawMaterialToLEGOMaterial[tokens[1]];
                }

                // Check if this line references a brick or a submodel.
                if (tokens.Length == 15 && tokens[14].EndsWith(".dat", StringComparison.InvariantCulture))
                {
                    // ------
                    // Brick.
                    // ------

                    var brick = new Brick();

                    var ldrawID = tokens[14];

                    // Naming convention found at: https://www.bricklink.com/help.asp?helpID=168

                    // 1. Peel off ".dat".
                    var fullName = ldrawID.Substring(0, ldrawID.Length - 4);

                    // 2. Peel off anything from 'c' onwards and check if it includes a number. (Assembly constant + assembly sequential #)
                    var assemblySplit = Regex.Split(fullName, "(c[0-9]+)");

                    // 3. If something was peeled off, it is an assembly, so process each of the parts directly.
                    if (assemblySplit.Length > 1)
                    {
                        if (LDrawAssembly.ContainsKey(ldrawID))
                        {
                            foreach (var part in LDrawAssembly[ldrawID])
                            {
                                AddPartToBrick(brick, part, transformation, materialID, fullName);
                            }
                        }
                        else
                        {
                            missingAssemblies.Add(fullName);
                            return;
                        }
                    }
                    else
                    {
                        // Not an assembly, so the brick has just the one part.
                        AddPartToBrick(brick, assemblySplit[0], transformation, materialID, fullName);
                    }

                    // Ensure that we have a current submodel. This is not guaranteed if no models are included in the file.
                    if (currentlyParsingSubModel == null)
                    {
                        // First submodel is the main model - make up a unique name to avoid conflict with actual submodels.
                        var subModelName = "Main model " + Guid.NewGuid();
                        mainModelName = subModelName;

                        currentlyParsingSubModel = new SubModel();
                        currentlyParsingSubModel.name = subModelName;
                        subModels.Add(currentlyParsingSubModel);
                    }

                    currentlyParsingSubModel.bricks.Add(brick);
                }
                else
                {
                    // ---------
                    // Submodel.
                    // ---------
                    var subModelName = string.Join(" ", tokens, 14, tokens.Length - 14);

                    // Keep track of how many times the given submodel has been referenced. This is needed later when assigning bricks to groups.
                    if (!subModelReferenceCount.ContainsKey(subModelName))
                    {
                        subModelReferenceCount[subModelName] = 0;
                    }
                    var subModelReferenceNumber = subModelReferenceCount[subModelName]++;

                    var subModelReference = new SubModelReference();
                    subModelReference.name = subModelName;
                    subModelReference.referenceNumber = subModelReferenceNumber;
                    subModelReference.transformation = transformation;
                    currentlyParsingSubModel.subModelReferences.Add(subModelReference);
                }

            }
            else if (line.Length > 0 && line.StartsWith("0 Name:", System.StringComparison.InvariantCulture))
            {
                // ----------
                // Submodel.
                // ----------
                // TODO: Submodel where a brick has LDraw material 16 ("current" or "main") will not result in correct material for brick as LDraw material is currently ignored for submodel lines.

                var subModelName = line.Substring(7).Trim();

                // First submodel is the main model - make up a unique name to avoid conflict with actual submodels.
                if (currentlyParsingSubModel == null)
                {
                    subModelName = "Main model " + Guid.NewGuid();
                    mainModelName = subModelName;
                }

                if (subModels.Find(x => x.name == subModelName) == null)
                {
                    currentlyParsingSubModel = new SubModel();
                    currentlyParsingSubModel.name = subModelName;
                    subModels.Add(currentlyParsingSubModel);
                }
                else
                {
                    Debug.LogWarning($"Multiple submodels with the same name {subModelName}. Ignoring duplicates.");
                }
            }
        }

        // Add the given assembly part to the given brick.
        private static void AddPartToBrick(Brick brick, AssemblyPart part, Matrix4x4 transformation, string materialID, string fullName)
        {
            // Peel off ".dat".
            var partLdrawID = part.ldrawID.Substring(0, part.ldrawID.Length - 4);

            // LDraw material 16 means "current" or "main" color, so if the part has that LDraw material use the materialID passed from the brick.
            if (part.ldrawMaterialID != "16")
            {
                if (LDrawMaterialToLEGOMaterial.ContainsKey(part.ldrawMaterialID))
                {
                    materialID = LDrawMaterialToLEGOMaterial[part.ldrawMaterialID];
                }
                else
                {
                    materialID = part.ldrawMaterialID;
                }
            }

            AddPartToBrick(brick, partLdrawID, transformation * part.transformation, materialID, fullName);
        }

        private static void AddPartToBrick(Brick brick, string ldrawID, Matrix4x4 transformation, string materialID, string fullName)
        {
            // Report if material is missing.
            var materialExistence = MaterialUtility.CheckIfMaterialExists(materialID);
            if (materialExistence == MaterialUtility.MaterialExistence.None)
            {
                missingMaterials.Add($"Brick ID {ldrawID}\tMaterial ID {materialID}");
            } else if (materialExistence == MaterialUtility.MaterialExistence.Legacy)
            {
                legacyMaterials.Add($"Brick ID {ldrawID}\tMaterial ID {materialID}");
            }

            // 1. Peel off anything from 'p' onwards and check if it includes a number. (Pattern constant + pattern sequential #)
            var patternSplit = Regex.Split(ldrawID, "(p[a-z]*[0-9]*)");

            // 2. If something was peeled off, make a note of it. We cannot map the pattern sequential id to a decoration imageId and we also don't know the surfaceName.
            if (patternSplit.Length > 1)
            {
                //Debug.Log("PATTERN: " + patternSplit[0] + patternSplit[1]);
                missingDecorations.Add($"Brick ID {fullName}");
            }

            // 3. Peel off anything from 'd' onwards and check if it includes a number. (Sticker + sticker number)
            var stickerSplit = Regex.Split(patternSplit[0], "(d[0-9]+)");

            // 4. If something was peeled off, make a note of it.
            if (stickerSplit.Length > 1)
            {
                //Debug.Log("STICKER: " + stickerSplit[0] + stickerSplit[1]);
                missingStickers.Add($"Brick ID {fullName}");
            }

            // 5. Reassemble remaining id + ".dat" and use as ldrawID.
            var designID = stickerSplit[0];
            ldrawID = designID + ".dat";

            // Apply mapping to another designID.
            if (LDrawBrickToLEGOBrick.ContainsKey(ldrawID))
            {
                designID = LDrawBrickToLEGOBrick[ldrawID];
            }

            // 6. If a mesh does not exist with exact designID, make a note of it and try to peel off any trailing letters.
            var partExistenceResult = PartUtility.CheckIfPartExists(designID);
            if (partExistenceResult.existence == PartUtility.PartExistence.None)
            {
                var versionSplit = Regex.Split(designID, "([a-z]+)");
                designID = versionSplit[0];

                // 6b. If there was something to peel off, look again with new designID.
                if (versionSplit.Length > 1)
                {
                    partExistenceResult = PartUtility.CheckIfPartExists(designID);
                    if (partExistenceResult.existence == PartUtility.PartExistence.None)
                    {
                        // Missing part.
                        missingParts.Add($"Brick ID {fullName}");
                    }
                    else
                    {
                        // Changed part.
                        changedParts.Add($"Brick ID {fullName}\tChanges to {designID}");

                        // Legacy part.
                        if (partExistenceResult.existence == PartUtility.PartExistence.Legacy)
                        {
                            // FIXME Check if colliders and connectivity info are available.
                            legacyParts.Add($"Brick ID {fullName}");
                        }
                    }
                }
                else
                {
                    // Missing part.
                    missingParts.Add($"Brick ID {fullName}");
                }
            }
            else if (partExistenceResult.existence == PartUtility.PartExistence.Legacy)
            {
                // Legacy part.
                // FIXME Check if colliders and connectivity info are available.
                legacyParts.Add($"Brick ID {fullName}");
            }

            // Reconstruct potentially changed ldrawID.
            ldrawID = designID + ".dat";

            // Apply transformation for ldrawID.
            if (LDrawBrickToTransformation.ContainsKey(ldrawID))
            {
                transformation *= LDrawBrickToTransformation[ldrawID];
            }
            else if (partExistenceResult.existence != PartUtility.PartExistence.None)
            {
                missingTransformations.Add($"Brick ID {fullName}");
            }

            var part = new Part()
            {
                transformation = transformation,
                designID = designID,
                materialID = materialID
            };

            brick.parts.Add(part);

            // Assign design ID of part to brick. This is incorrect for multi-part bricks but we do not know the correct design ID.
            brick.designID = designID;
        }

        private static void OutputBrickLXFML(SubModel subModel, int subModelReferenceNumber, Matrix4x4 currentTransformation, XmlDocument doc, XmlElement bricks)
        {
            foreach (var subModelBrick in subModel.bricks)
            {
                var brick = doc.CreateElement("Brick");
                brick.SetAttribute("refID", currentBrickRefId.ToString());
                brick.SetAttribute("designID", subModelBrick.designID + ";A"); // Assume first version.

                bricks.AppendChild(brick);

                foreach (var submodelPart in subModelBrick.parts)
                {
                    var transformation = currentTransformation * submodelPart.transformation;

                    var part = doc.CreateElement("Part");
                    part.SetAttribute("refID", currentPartRefId.ToString());
                    part.SetAttribute("designID", submodelPart.designID + ";A"); // Assume first version.
                    part.SetAttribute("materials", submodelPart.materialID + ":0"); // Assume Shiny Plastic shaderId.
                    brick.AppendChild(part);

                    var bone = doc.CreateElement("Bone");
                    bone.SetAttribute("refID", currentPartRefId.ToString()); // Assume one-bone parts.
                    bone.SetAttribute("transformation",
                        transformation.m00.ToString(CultureInfo.InvariantCulture) + "," + transformation.m10.ToString(CultureInfo.InvariantCulture) + "," + transformation.m20.ToString(CultureInfo.InvariantCulture) + "," +
                        transformation.m01.ToString(CultureInfo.InvariantCulture) + "," + transformation.m11.ToString(CultureInfo.InvariantCulture) + "," + transformation.m21.ToString(CultureInfo.InvariantCulture) + "," +
                        transformation.m02.ToString(CultureInfo.InvariantCulture) + "," + transformation.m12.ToString(CultureInfo.InvariantCulture) + "," + transformation.m22.ToString(CultureInfo.InvariantCulture) + "," +
                        transformation.m03.ToString(CultureInfo.InvariantCulture) + "," + transformation.m13.ToString(CultureInfo.InvariantCulture) + "," + transformation.m23.ToString(CultureInfo.InvariantCulture)
                        );
                    part.AppendChild(bone);

                    currentPartRefId++;
                }

                // Store brick ref id in brick group for use when outputting groups later.
                // Use submodel reference number to retrieve the correct group.
                var brickGroupName = subModel.name + "_" + subModelReferenceNumber;
                if (!brickGroups.ContainsKey(brickGroupName))
                {
                    brickGroups[brickGroupName] = new List<int>();
                }
                brickGroups[brickGroupName].Add(currentBrickRefId);

                currentBrickRefId++;
            }

            foreach (var subModelReference in subModel.subModelReferences)
            {
                var transformation = currentTransformation * subModelReference.transformation;
                var referencedSubModel = subModels.Find(x => x.name.Equals(subModelReference.name, StringComparison.OrdinalIgnoreCase));
                if (referencedSubModel != null)
                {
                    OutputBrickLXFML(referencedSubModel, subModelReference.referenceNumber, transformation, doc, bricks);
                }
                else
                {
                    missingSubModels.Add(subModelReference.name);
                }
            }
        }

        private static void OutputGroupLXFML(Dictionary<string, List<int>> brickGroups, XmlDocument doc, XmlElement brickGroupSystem)
        {
            foreach (var brickGroup in brickGroups)
            {
                if (brickGroup.Value.Count > 0)
                {
                    var group = doc.CreateElement("Group");
                    // Peel off the submodel reference number off the group name again.
                    var groupName = Regex.Split(brickGroup.Key, "(_[0-9]+)")[0];
                    group.SetAttribute("name", groupName == mainModelName ? "Main model" : groupName);
                    group.SetAttribute("transformation", "1,0,0,0,1,0,0,0,1,0,0,0");
                    group.SetAttribute("pivot", "0,0,0");
                    var brickRefs = string.Join(",", brickGroup.Value);
                    group.SetAttribute("brickRefs", brickRefs);
                    brickGroupSystem.AppendChild(group);
                }
            }
        }
    }

}