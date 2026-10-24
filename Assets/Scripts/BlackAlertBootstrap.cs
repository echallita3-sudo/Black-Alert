using UnityEngine;

namespace BlackAlert
{
    // Creates the first playable systems-test scene without relying on imported binary assets.
    // The geometry is engineering-only and MUST be replaced with approved high-quality models.
    public static class BlackAlertBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<BattlefieldPrototype>() != null) return;
            var game = new GameObject("BlackAlert Runtime");
            Object.DontDestroyOnLoad(game);
            game.AddComponent<BattlefieldPrototype>();
        }
    }
}
