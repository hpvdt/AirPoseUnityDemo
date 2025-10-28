# AGENTS.md - AirPoseUnityDemo

## Build/Test Commands
- **Build**: `dotnet build AirPoseUnityDemo.sln` (compile entire project)
- **Run single test**: Unity CLI with `-testFilter "Namespace.Class.Method"` in batch mode
- **Run all tests**: Unity CLI with `-runTests -batchmode -editorTestsResultFile results.xml` (count tests executed and passed)
- **Unity path**: `~/Unity/Hub/Editor/2022.3.62f1/Editor/Unity` (Linux), `C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe` (Windows)
- **Verify often**: Compile project frequently with `dotnet build` to catch errors early

## Architecture
- **Unity 2022.3** project for XR/AR using Nreal Air glasses
- **MAVLinkAPI package**: Core package in `Packages/MAVLinkAPI/` with Runtime, Editor, Tests folders
- **Main namespaces**: `MAVLinkAPI.{API, Comms, Routing, UI, Util, Ext}` - see Runtime/ structure
- **Test suffix**: All test classes end with `Spec` (e.g., `InputWithHistorySpec`)
- **Dependencies**: TextMeshPro, YamlDotNet, NUnit, Newtonsoft.Json
- **Orchestration submodule**: `Packages/MAVLinkAPI/__module~/orchestration/` is Rust-based (see its AGENT.md)

## Code Style & Conventions
- **Null safety**: Use `#nullable enable`, `null!` for Unity-initialized fields, avoid null checks on `[Required]` fields
- **Naming**: PascalCase for classes/methods/properties, camelCase for private fields with `_` prefix
- **Imports**: Group System → UnityEngine/TMPro → MAVLinkAPI → third-party
- **asmdef files**: Avoid using GUIDs, use reference names instead
- **Structs**: Avoid non-nullable public fields (compiler can't guarantee null safety)
- **Comments**: Minimal edits, avoid redundant/short comments, preserve existing comments
- **Tests**: Place in `Tests/` directory under package/asset root, use NUnit with `[UnitySetUp]`/`[UnityTest]`
