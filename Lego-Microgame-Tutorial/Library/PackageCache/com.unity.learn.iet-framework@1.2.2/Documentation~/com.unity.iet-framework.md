# About Tutorial Framework

This package is used to display interactive in-Editor tutorials in tutorial projects and project templates. Currently this package is meant to be used only by Unity internally.

# Installing Tutorial Framework

This package is not currently discoverable. To install this package, add the following line to `Packages/manifest.json`: `Packages/manifest.json`: `"com.unity.learn.iet-framework": "1.2.0"`.

# Using Tutorial Framework

To actually develop any tutorials, the Tutorial Authoring Tools package is needed. Install it by adding the following line to `Packages/manifest.json`: `"com.unity.learn.iet-framework.authoring": "0.6.4-preview"`.

# Technical details
## Requirements

This version of Tutorial Framework is compatible with the following versions of the Unity Editor:

* 2019.4 and newer

## Known Issues
- Warning in the Console when using `BuildStartedCriterion` and making a build.
- `TutorialWelcomePage.WindowTitle` cannot be edited at real-time; reopen the `TutorialWelcomePage` in order to see the changes.
- Updating the package leaves Tutorials window sometimes in malformed state. Reimport the package and reopen the window to solve the problem.
