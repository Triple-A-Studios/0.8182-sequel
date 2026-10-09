using UnityEngine;

namespace Opoint8182.Common
{
    public interface ICrashSource
    {
        int ScoreValue { get; }

        /// <summary>Where the crash happened, for effects that anchor to the impact. Read synchronously from the crash event: the source may destroy itself right after raising it.</summary>
        Vector3 WorldPosition { get; }
    }
}
