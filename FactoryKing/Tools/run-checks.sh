#!/usr/bin/env bash
# Runs every check that does not need the Unity Editor:
#   1. builds the game core exactly as Unity compiles it (netstandard2.1, C# 9, warnings as errors)
#   2. runs the NUnit suite shared with Unity's Test Runner
#   3. compile-checks the Unity runtime scripts against UnityEngine reference assemblies
set -euo pipefail
cd "$(dirname "$0")/DotNet"
dotnet build Core/FactoryKing.Core.csproj -nologo -v q
dotnet test Tests/FactoryKing.Tests.csproj -nologo -v q
dotnet build RuntimeCheck/FactoryKing.RuntimeCheck.csproj -nologo -v q
echo "All Factory King checks passed."
