using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Events/On Player Spawn")]
public class GameEvent : ScriptableObject
{
    public event Action<GameObject, GameMode> OnPlayerSpawn;

    public void PlayerSpawned(GameObject player, GameMode mode)
    {
        OnPlayerSpawn?.Invoke(player, mode);
    }
}