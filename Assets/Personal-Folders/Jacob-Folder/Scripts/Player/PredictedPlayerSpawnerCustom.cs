using System.Collections.Generic;
using PurrNet.Logging;
using PurrNet.Packing;
using PurrNet.Pooling;
using PurrNet.Utils;
using Unity.Multiplayer.PlayMode;
using UnityEditor.Build.Content;
using UnityEngine;

namespace PurrNet.Prediction
{
    public class PredictedPlayerSpawnerCustom : DeterministicIdentity<PlayerSpawnerState>
    {
        [SerializeField] private List<GameObject> _playerPrefabs;
        [SerializeField, PurrLock] private bool _destroyOnDisconnect;
        [SerializeField] private List<Transform> T1_spawnPoints = new List<Transform>();
        [SerializeField] private List<Transform> T2_spawnPoints = new List<Transform>();

        // ADDED BY JACOB W

        private (GameManager.Team, string) currentPlayerInfo;
        
        private GameObject GetPlayerPrefab(PlayerID player)
        {
            currentPlayerInfo = GameManager.Instance.GetPlayerSetup(player);

            if (currentPlayerInfo.Item2 == "Mosquito")
            {
                //Debug.Log("Selecting Mosquito for Player");
                return _playerPrefabs[0];
            }
            else if (currentPlayerInfo.Item2 == "Beetle")
            {
                //Debug.Log("Selecting Beetle for Player");
                return _playerPrefabs[1];
            }
            else if (currentPlayerInfo.Item2 == "Butterfly")
            {
                //Debug.Log("Selecting Butterfly for Player");
                return _playerPrefabs[2];
            }

            //Debug.Log("Defaulting to Mosquito for Player");
            return _playerPrefabs[0];
        }

        //

        private void Awake() => CleanupSpawnPoints();

        protected override void LateAwake()
        {
            if (predictionManager.players)
            {
                var players = predictionManager.players.players;
                for (var i = 0; i < players.Count; i++)
                    OnPlayerLoadedScene(players[i]);

                predictionManager.players.onPlayerAdded += OnPlayerLoadedScene;
                predictionManager.players.onPlayerRemoved += OnPlayerUnloadedScene;
            }
        }

        protected override PlayerSpawnerState GetInitialState()
        {
            return new PlayerSpawnerState
            {
                spawnPointIndex = 0,
                values = DisposableList<PlayerWithObject>.Create()
            };
        }

        protected override void Destroyed()
        {
            if (predictionManager && predictionManager.players)
            {
                predictionManager.players.onPlayerAdded -= OnPlayerLoadedScene;
                predictionManager.players.onPlayerRemoved -= OnPlayerUnloadedScene;
            }
        }

        protected override PlayerSpawnerState Interpolate(PlayerSpawnerState from, PlayerSpawnerState to, float t)
            => to;

        private void CleanupSpawnPoints()
        {
            bool hadNullEntry = false;
            for (int i = 0; i < T1_spawnPoints.Count; i++)
            {
                if (!T1_spawnPoints[i])
                {
                    hadNullEntry = true;
                    T1_spawnPoints.RemoveAt(i);
                    i--;
                }
            }

            if (hadNullEntry)
                PurrLogger.LogWarning($"Some spawn points were invalid and have been cleaned up.", this);

            hadNullEntry = false;
            for (int i = 0; i < T2_spawnPoints.Count; i++)
            {
                if (!T2_spawnPoints[i])
                {
                    hadNullEntry = true;
                    T2_spawnPoints.RemoveAt(i);
                    i--;
                }
            }

            if (hadNullEntry)
                PurrLogger.LogWarning($"Some spawn points were invalid and have been cleaned up.", this);
        }

        private void OnPlayerUnloadedScene(PlayerID player)
        {
            if (!_destroyOnDisconnect)
                return;

            if (currentState.TryGetValue(player, out var playerID))
            {
                hierarchy.Delete(playerID);
                currentState.Remove(player);
            }
        }

        private void OnPlayerLoadedScene(PlayerID player)
        {
            //Debug.Log($"[Spawner] Player {player} joining — dumping network state:");
            //GameManager.Instance.DebugLogAllNetworkIdentities();

            if (!enabled)
                return;

            if (currentState.ContainsKey(player))
                return;

            PredictedObjectID? newPlayer;

            CleanupSpawnPoints();

            GameObject playerSelectedPrefab = GetPlayerPrefab(player);

            if (T1_spawnPoints.Count > 0 && currentPlayerInfo.Item1 == GameManager.Team.TEAM1)
            {
                var spawnPoint = T1_spawnPoints[currentState.spawnPointIndex];
                currentState.spawnPointIndex = (currentState.spawnPointIndex + 1) % T1_spawnPoints.Count;
                newPlayer = hierarchy.Create(playerSelectedPrefab, spawnPoint.position, spawnPoint.rotation, player);
            }
            else if (T2_spawnPoints.Count > 0 && currentPlayerInfo.Item1 == GameManager.Team.TEAM2)
            {
                var spawnPoint = T2_spawnPoints[currentState.spawnPointIndex];
                currentState.spawnPointIndex = (currentState.spawnPointIndex + 1) % T2_spawnPoints.Count;
                newPlayer = hierarchy.Create(playerSelectedPrefab, spawnPoint.position, spawnPoint.rotation, player);
            }
            else
            {
                newPlayer = hierarchy.Create(playerSelectedPrefab, owner: player);
            }

            if (!newPlayer.HasValue)
                return;

            currentState[player] = newPlayer.Value;
            predictionManager.SetOwnership(newPlayer, player);
        }
    }
}
