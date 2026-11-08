using System.Collections.Generic;
using System.Text;
using FactoryKing.Core.Meta;
using UnityEngine;

namespace FactoryKing.Runtime.Platform
{
    /// <summary>
    /// Development sink: prints consented analytics events to the console. Release builds have no network sink
    /// until a privacy-compliant backend is chosen (then update the privacy manifest and App Privacy answers).
    /// </summary>
    public sealed class DebugAnalyticsSink : IAnalyticsSink
    {
        public void Send(string name, IReadOnlyDictionary<string, string> parameters)
        {
            var sb = new StringBuilder("[Analytics] ").Append(name);
            foreach (var kv in parameters) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
            Debug.Log(sb.ToString());
        }
    }
}
