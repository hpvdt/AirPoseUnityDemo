# AGENTS.md - AirPoseUnityDemo

# Tools

See @.mcp.json for details

## Build/Test Commands

- **Build**: `dotnet build AirPoseUnityDemo.sln` (compile entire project)
- **Run test(s)**: Use UnityMCP
- **Verify often**: Compile project frequently with `dotnet build` to catch errors early

## Architecture

- **Unity 2022.3** project for XR/AR using Nreal Air glasses
- **MAVLinkAPI package**: Core package in `Packages/MAVLinkAPI/` with Runtime, Editor, Tests folders
- **Main namespaces**: `MAVLinkAPI.{API, Comms, Routing, UI, Util, Ext}` - see Runtime/ structure
- **Test suffix**: All test classes end with `Spec` (e.g., `InputWithHistorySpec`)
- **Dependencies**: TextMeshPro, YamlDotNet, NUnit, Newtonsoft.Json
- **Orchestration submodule**: `Packages/MAVLinkAPI/__module~/orchestration/` is Rust-based (see its AGENT.md)

## Code Style & Conventions

- **Null safety**: Use `#nullable enable`, `null!` for Unity-initialized fields, avoid null checks on `[Required]`
  fields
- **Naming**: PascalCase for classes/methods/properties, camelCase for private fields with `_` prefix
- **Imports**: Group System → UnityEngine/TMPro → MAVLinkAPI → third-party
- **asmdef files**: Avoid using GUIDs, use reference names instead
- **Structs**: Avoid non-nullable public fields (compiler can't guarantee null safety)
- **Comments**: Minimal edits, avoid redundant/short comments, preserve existing comments
- **Tests**: Place in `Tests/` directory under package/asset root, use NUnit with `[UnitySetUp]`/`[UnityTest]`
