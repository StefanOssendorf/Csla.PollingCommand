# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

```bash
dotnet build       # build
dotnet test        # all tests
dotnet clean       # clean artifacts
dotnet test --filter "FullyQualifiedName~Ossendorf.Csla.PollingCommand.Tests.Client.DefaultPollingCommandTests"  # single class
```

Uses **MicrosoftTestingPlatform** (MTP) with TUnit — tests run as a console executable, not via traditional test runners. Targets .NET 8.0, 9.0, 10.0. CI runs on every push/PR.

## Architecture Overview

The library implements **client-server command polling** — a pattern where long-running CSLA commands execute asynchronously on the server while the client polls for completion. This avoids HTTP timeout issues in constrained environments.

### High-Level Flow

1. **Client Initiation**: `IPollingCommand.Execute<T>()` → calls `InitiateCommandExecutionCommand` via DataPortal → server queues command → returns correlation ID
2. **Server Execution**: Background service (`CommandExecutionHostedService`) processes queued commands via reflection-based executor (`CommandExecutionProcessor`)
3. **Client Polling**: `PollStateOrResultCommand` repeatedly checks state until `IsFinished` → deserializes result and completes `Task<T>`

### Core Components

#### Client-Side (`src/.../Client/`)
- **`DefaultPollingCommand`**: Implements `IPollingCommand`. Manages initiation and polling loop. Handles parameter serialization/deserialization via CSLA's `ISerializationFormatter`.
- **`PollingOptions`**: Configurable polling interval (milliseconds) per Execute call.

#### Server-Side (`src/.../Server/`)
- **`Commands.cs`**: Central coordinator. Holds:
  - Async channel (`Channel<QueuedCommand>`) for command queue
  - `ConcurrentDictionary` for in-progress tracking
  - `IMemoryCache` for finished command results (TTL-based eviction)
  - Implements 5 internal interfaces: `ICommandStarter`, `IWaitingCommands`, `IProcessingCommands`, `IFinishedCommands`, `IFinishCommands`

- **`CommandExecutionProcessor`**: Executes queued commands. Uses reflection to dynamically invoke `DataPortal_Execute()` on any `CommandBase<T>`. Manages:
  - DI scope per command (ensures proper disposal)
  - Principal/user context deserialization
  - Exception capture via `ExceptionDispatchInfo` (for re-throw on client)
  - Result serialization back to client

- **`InitiateCommandExecutionCommand`**: DataPortal command that receives command type name + serialized parameters from client, queues execution, returns correlation ID.

- **`PollStateOrResultCommand`**: DataPortal command that receives correlation ID, returns processing state and finished result (if ready).

- **`FinishedCommand`**: Wrapper holding serialized result (success) or `ExceptionDispatchInfo` (failure).

- **`CommandExecutionHostedService`**: Runs background loop that processes queued commands indefinitely.

#### Public API (`IServiceCollectionExtensions.cs`)
- **`AddPollingCommandServer()`**: Registers server components. Creates singleton `Commands`, registers `CommandExecutionHostedService`, registers `IMemoryCache` for result caching.
- **`AddPollingCommandClient(TimeSpan interval)`**: Registers `DefaultPollingCommand` as transient `IPollingCommand`.

### Data Flow Diagram

See `docs/img/flow.png` for visual flow of initiation → queueing → execution → polling → result.

### Key Design Decisions

1. **In-Memory Only**: Commands are queued and executed entirely in memory (`Channel<T>`, `ConcurrentDictionary`). On server restart, all in-flight commands are lost.

2. **Parallel Processing**: Commands are dispatched concurrently — `CommandExecutionProcessor` fires `Process(command)` without awaiting, so multiple commands run in parallel via the background service.

3. **Reflection-Based Execution**: Uses `Type.GetType()` to resolve command types from string names, then dynamically invokes `DataPortal_Execute()`. Executor instances are cached by type to avoid repeated reflection.

4. **Result Caching with TTL**: Finished results held in `IMemoryCache` with configurable `FinishedCommandTtl` (default 5 minutes). Expired results are automatically evicted.

5. **Principal Serialization**: User context (`IPrincipal`) is serialized with parameters during initiation, deserialized and set during execution (via `ApplicationContext.User`).

## Critical Files to Understand

| File | Purpose |
|------|---------|
| `src/.../IPollingCommand.cs` | Public API contract |
| `src/.../Client/DefaultPollingCommand.cs` | Client-side initiation + polling loop |
| `src/.../Server/Commands.cs` | In-memory queue, cache, state tracking |
| `src/.../Server/CommandExecutionProcessor.cs` | Reflection-based executor, scope management |
| `src/.../IServiceCollectionExtensions.cs` | DI registration (client & server) |
| `tests/.../EndToEnd/FullWorkflowTests.cs` | Integration tests showing end-to-end flow |

## Dependencies

**CSLA 10.1.0**: Core framework. Provides `CommandBase<T>`, DataPortal routing, serialization, principal/context management.

**Code Generators** (build-time):
- `Csla.Generator.AutoImplementProperties.CSharp`: Auto-implements `[CslaImplementProperties]` partial properties
- `Csla.Generator.AutoSerialization.CSharp`: Auto-generates serialization code
- `Ossendorf.Csla.DataPortalExtensionGenerator`: Custom generator for server-side DataPortal command routes

**Caching**: `Microsoft.Extensions.Caching.Memory.IMemoryCache` for finished command result TTL eviction.

**Testing**: TUnit, FakeItEasy (mocking), Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory for integration tests).

## Important Constraints

- **No Persistence**: Commands do not survive server restart.
- **Parallel Processing**: Commands execute concurrently on the server (fire-and-forget dispatch in the background loop).
- **Transport-Agnostic**: Any CSLA-supported transport (HTTP, gRPC, named pipes, etc.) works transparently.
- **CSLA Commands Only**: Requires `CommandBase<T>` subclasses; works with any result type.

## Development Notes

- **Null Safety**: Nullable reference types enabled. Watch for `!` null-forgiving operators in reflection code (intentional when type safety is guaranteed by CSLA runtime).
- **Source Generators**: Build-time code generation via CSLA generators and custom `DataPortalExtensionGenerator`. Don't manually edit generated code; modify source and regenerate.
- **MinVer Versioning**: Version is auto-calculated from git tags (v-prefixed, semver). Use tags like `v1.2.3` or `v1.2.3-preview.0` to bump version.
- **Central Package Management**: All NuGet versions managed in `Directory.Packages.props`. Update versions there, not in individual project files.

## Maintaining Documentation

### README Images

Store diagram sources (`.dot`, `.py`) in `/docs/`. Generate images to `/docs/img/` and commit both.

**Graphviz diagrams:**
```bash
dot -Tpng docs/img/diagram.dot -o docs/img/diagram.png
```

**Badges:** Use shields.io URLs in markdown. They update automatically with live NuGet/GitHub data—no regeneration needed.

**Update process:** Edit source file → regenerate image → commit both source and `.png`.
