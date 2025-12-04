# AGENTS.md - AirPoseUnityDemo

# Tools

See @.mcp.json for details

## Build/Test Commands

- **Build**: `dotnet build AirPoseUnityDemo.sln` (compile entire project)
- **Run test(s)**:
  - **UnityMCP (recommended)**: `mcp__UnityMCP__run_tests` with mode `"EditMode"` or `"PlayMode"`
  - **Unity Test Runner**: Window → General → Test Runner, select EditMode/PlayMode tabs
  - **Command line**: `/home/peng/Unity/Hub/Editor/2022.3.62f3/Editor/Unity -batchmode -runTests -projectPath "$(pwd)" -testPlatform EditMode -testResults "EditModeResults.xml"`
- **Test files**: Located in `Packages/MAVLinkAPI/Tests/` and `Assets/AirAPI/Tests/`, all end with `Spec` suffix
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
- **Tests**: Place in `Tests/` directory under package/asset root, use NUnit with `[TestFixture]`/`[Test]` attributes, assembly definition must include `"UNITY_INCLUDE_TESTS"` constraint

## Process Management

- **No direct process termination**: Do not use `pkill`, `kill`, `killall`, or similar commands to terminate processes directly. Use proper application shutdown methods instead.
