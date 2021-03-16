using UnityEngine;
using Unity.InteractiveTutorials;

/// <summary>
/// Implement your Tutorial callbacks here.
/// </summary>
[CreateAssetMenu(fileName = DefaultFileName, menuName = "Tutorials/" + DefaultFileName + " Instance")]
public class TutorialCallbacks : ScriptableObject
{
    public const string DefaultFileName = "TutorialCallbacks";

    public static ScriptableObject CreateInstance()
    {
        return ScriptableObjectUtils.CreateAsset<TutorialCallbacks>(DefaultFileName);
    }

    // Example callback for basic UnityEvent
    public void ExampleMethod()
    {
        Debug.Log("ExampleMethod");
    }

    // Example callbacks for ArbitraryCriterion's BoolCallback
    public bool DoesFooExist()
    {
        return GameObject.Find("Foo") != null;
    }

    // Implement the logic to automatically complete the criterion here, if wanted/needed.
    public bool AutoComplete()
    {
        var foo = GameObject.Find("Foo");
        if (!foo)
            foo = new GameObject("Foo");
        return foo != null;
    }
}
