// Unity stub for CI builds - UnityEngine.InputModule.dll placeholder.
//
// OBRA's csproj has a hardcoded reference to UnityEngine.InputModule (the
// EventSystems / new-input-system module that Unity 2017 ships). The legacy
// `Input` class lives in CoreModule, not here - see UnityStubs.CoreModule.cs.
// This file exists only so the assembly resolves at build time; it has no
// types our mod uses.
namespace UnityEngine.EventSystems {
    internal class _UnityInputModuleStubFiller { }
}
