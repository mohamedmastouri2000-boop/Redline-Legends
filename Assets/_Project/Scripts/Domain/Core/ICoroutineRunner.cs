using System.Collections;
using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Lets pure C# services run Unity coroutines through the persistent bootstrap object
    /// without themselves becoming MonoBehaviours.
    /// </summary>
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator routine);
        void Stop(Coroutine coroutine);
    }
}
