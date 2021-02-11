using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.InteractiveTutorials
{
    public abstract class PreprocessBuildCriterion : Criterion, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public abstract void OnPreprocessBuild(BuildReport report);
    }

    // TODO revisit this code, BuildPlayerWindow.RegisterBuildPlayerHandler works only when
    // building from the default build dialog, hence IPreprocessBuildWithReport + SessionState used also.
    public class BuildStartedCriterion : PreprocessBuildCriterion
    {
        bool BuildStarted
        {
            get => SessionState.GetBool("BuildStartedCriterion.BuildStarted", false);
            set => SessionState.SetBool("BuildStartedCriterion.BuildStarted", value);
        }

        public void BuildPlayerCustomHandler(BuildPlayerOptions options)
        {
            BuildStarted = true;
            BuildPipeline.BuildPlayer(options);
        }

        public override void StartTesting()
        {
            BuildStarted = false;
            UpdateCompletion();
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerCustomHandler);
            EditorApplication.update += UpdateCompletion;
        }

        public override void StopTesting()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(null);
            EditorApplication.update -= UpdateCompletion;
        }

        protected override bool EvaluateCompletion()
        {
            return BuildStarted;
        }

        public override bool AutoComplete()
        {
            return true;
        }

        public override void OnPreprocessBuild(BuildReport report)
        {
            BuildStarted = true;
        }
    }
}
