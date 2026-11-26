using System;
using UnityEngine.InputSystem;

public static class MiniGameBridge
{
    public static event Action<int> OnFinished;

    public static void Finish(int winner)
    {
        OnFinished?.Invoke(winner);
    }
}