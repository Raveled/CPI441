using System.Collections.Generic;
using PurrNet.Logging;
using PurrNet.Packing;
using PurrNet.Pooling;
using PurrNet.Utils;
using UnityEditor.Build.Content;
using UnityEngine;

namespace PurrNet.Prediction
{
    public class PredictedPlayerSpawnerCustom : DeterministicIdentity<PlayerSpawnerState>
    {
        [SerializeField] private List<GameObject> _playerPrefabs;
        [SerializeField, PurrLock] private bool _destroyOnDisconnect;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        // ADDED BY JACOB W

        private GameObject GetPlayerPrefab()
        {
            string selectedCharacter = GameManager.Instance.GetPlayerCharacter();

            if (selectedCharacter == "Mosquito")
            {
                Debug.Log("Selecting Mosquito for Player");
                return _playerPrefabs[0];
            }
            else if (selectedCharacter == "Beetle")
            {
                Debug.Log("Selecting Beetle for Player");
                return _playerPrefabs[1];
            }
            else if (selectedCharacter == "Butterfly")
            {
                Debug.Log("Selecting Butterfly for Player");
                return _playerPrefabs[2];
            }

            Debug.Log("Defaulting to Mosquito for Player");
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
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (!spawnPoints[i])
                {
                    hadNullEntry = true;
                    spawnPoints.RemoveAt(i);
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
            if (!enabled)
                return;

            if (currentState.ContainsKey(player))
                return;

            PredictedObjectID? newPlayer;

            CleanupSpawnPoints();

            GameObject playerSelectedPrefab = GetPlayerPrefab();

            if (spawnPoints.Count > 0)
            {
                var spawnPoint = spawnPoints[currentState.spawnPointIndex];
                currentState.spawnPointIndex = (currentState.spawnPointIndex + 1) % spawnPoints.Count;
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
