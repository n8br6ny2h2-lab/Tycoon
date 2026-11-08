using System;
using System.IO;

namespace FactoryKing.Tests
{
    /// <summary>Finds project files both under the Unity Test Runner (cwd = project root) and dotnet test (cwd = bin).</summary>
    internal static class TestPaths
    {
        public static string ProjectFile(string relative)
        {
            foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, relative);
                    if (File.Exists(candidate)) return candidate;
                    string nested = Path.Combine(dir.FullName, "FactoryKing", relative);
                    if (File.Exists(nested)) return nested;
                    dir = dir.Parent;
                }
            }
            throw new FileNotFoundException("Project file not found: " + relative);
        }
    }
}
