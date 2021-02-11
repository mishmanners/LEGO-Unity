using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.InteractiveTutorials.Tests
{
    public class ProjectResourceTests
    {
        readonly string[] k_UITexturePaths =
        {
            "Packages/com.unity.learn.iet-framework/Editor/UI",
            "Packages/com.unity.learn.iet-framework/Editor/Resources/icons"
        };

        [Test]
        public void CommonResourcesExist()
        {
            Assert.IsTrue(Directory.Exists(TutorialWindow.k_UIAssetPath), $"'{TutorialWindow.k_UIAssetPath}' does not exist");

            Assert.IsTrue(File.Exists(TutorialContainer.k_DefaultLayoutPath), $"'{TutorialContainer.k_DefaultLayoutPath}' does not exist");

            Assert.IsTrue(File.Exists(TutorialProjectSettings.k_DefaultStyleAsset), $"'{TutorialProjectSettings.k_DefaultStyleAsset}' does not exist");

            Assert.IsTrue(File.Exists(TutorialStyles.DefaultDarkStyleFile), $"'{TutorialStyles.DefaultDarkStyleFile}' does not exist");
            Assert.IsTrue(File.Exists(TutorialStyles.DefaultLightStyleFile), $"'{TutorialStyles.DefaultLightStyleFile}' does not exist");
        }

        [Ignore("TODO: problematic with the new docking logic, revisit this.")]
        [Test]
        public void DefaultLayoutContainsTutorialWindow()
        {
            TutorialManager.SaveOriginalWindowLayout();
            TutorialManager.LoadWindowLayout(TutorialContainer.k_DefaultLayoutPath);
            bool hasTutorialWindow =  EditorWindowUtils.FindOpenInstance<TutorialWindow>();
            TutorialManager.RestoreOriginalWindowLayout();
            Assert.IsTrue(hasTutorialWindow, $"{TutorialContainer.k_DefaultLayoutPath} does not contain TutorialWindow.");
        }

        [Test]
        public void UITexturesPathsExist()
        {
            k_UITexturePaths.ToList().ForEach(path =>
                Assert.IsTrue(Directory.Exists(path), $"Path '{path}' does not exist")
            );
        }

        [Test]
        public void UITexturesHaveCorrectTextureType()
        {
            var texturesWithWrongType = AssetDatabase.FindAssets("t:Texture2D", k_UITexturePaths)
                .Select(guid => AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter)
                .Where(importer => importer.textureType != TextureImporterType.GUI)
                .Select(importer => $"\"{importer.assetPath}\"")
                .ToArray();

            Assert.IsEmpty(texturesWithWrongType);
        }
    }
}
