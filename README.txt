DYNAMIC ISLAND — PORTABLE WINDOWS EXE
=====================================

This project builds one self-contained Windows x64 executable:
    DynamicIsland.exe

Your PC does NOT need .NET SDK, Python, Visual Studio Code, or CMake.
The .NET SDK is used only on the hosted Windows build runner in GitHub Actions.

HOW TO GET THE EXE
------------------
1. Sign in to GitHub and create a new empty repository (for example: DynamicIsland).
2. Upload the CONTENTS of this folder to the repository. Keep the hidden
   .github/workflows/build-portable.yml file; it is required.
3. Open the repository's Actions tab.
4. Select "Build DynamicIsland.exe" and press "Run workflow".
   A push to main/master also starts a build automatically.
5. Wait for the green successful run.
6. Open that run, find Artifacts, and download "DynamicIsland-Windows-x64".
7. Extract the downloaded artifact and run DynamicIsland.exe.

IMPORTANT
---------
- This is a buildable source project plus an automated build workflow, NOT a
  precompiled EXE. A real Windows build runner must execute the workflow first.
- The app uses Microsoft Edge WebView2. Most current Windows 10/11 systems have
  the Evergreen WebView2 Runtime. If it is missing, install it from:
  https://developer.microsoft.com/microsoft-edge/webview2/
- The executable is self-contained for .NET, but WebView2 Runtime is a separate
  Windows component and is not embedded in the EXE.
- The app is designed for Windows 10/11 x64. ARM64 is not included.
- RAM usage target is 50–60 MB, not a guarantee. WebView2 uses its own processes
  and actual memory use depends on Windows and the installed runtime.

FEATURES IN THIS BUILD
----------------------
- Gaming, Cyberpunk, Liquid Glass, Light, Air, Gradient themes
- Adjustable edge glow color and intensity
- Hover-to-expand six-section menu
- Notes saved locally in WebView2 storage
- Copy text to the Windows clipboard
- Previous / Play-Pause / Next media key controls
- Startup toggle through the current user's Run registry key
- Auto-hide when a foreground window fills the primary display
- Single-file self-contained .NET publish (WebView2 Runtime still required)
