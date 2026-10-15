using System;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;

namespace GreenHellAI
{
    internal sealed class GameStateReader
    {
        // Plugin-owned scalar identity state only. No Unity object reference survives a probe.
        private int _playerId;
        private int _conditionId;
        private double _stableAfter;

        internal void Reset()
        {
            _playerId = 0;
            _conditionId = 0;
            _stableAfter = 0.0;
        }

        private bool Unavailable(string state, out string reason)
        {
            Reset();
            reason = state;
            return false;
        }

        // Keep target-game types out of the plugin's startup methods and field declarations.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool TryCapture(double now, out Snapshot sample, out string reason)
        {
            sample = null;
            double started = Plugin.ClockSeconds;

            // These accessors return existing instances. In particular, do NOT call
            // GreenHellGame.Instance: its getter can create and initialize the application.
            MainLevel level = MainLevel.Instance;
            if (level == null || !level.isActiveAndEnabled || !level.m_LevelStarted)
                return Unavailable("waiting-for-started-level", out reason);

            LoadingScreen loading = LoadingScreen.Get();
            if (loading == null || loading.m_Active)
                return Unavailable("loading-screen-or-unavailable", out reason);

            if (SaveGame.m_State != SaveGame.State.None)
                return Unavailable("save-load-in-progress", out reason);

            MenuInGameManager menus = MenuInGameManager.Get();
            if (menus == null || menus.IsAnyScreenVisible())
                return Unavailable("in-game-menu-or-unavailable", out reason);

            Player player = Player.Get();
            PlayerConditionModule condition = PlayerConditionModule.Get();
            ScenarioManager scenario = ScenarioManager.Get();
            if (player == null || condition == null || scenario == null ||
                !player.isActiveAndEnabled || !condition.isActiveAndEnabled)
                return Unavailable("player-condition-or-scenario-unavailable", out reason);

            if (condition.gameObject != player.gameObject)
                return Unavailable("condition-does-not-belong-to-local-player", out reason);

            int playerId = player.GetInstanceID();
            int conditionId = condition.GetInstanceID();
            if (_playerId != playerId || _conditionId != conditionId)
            {
                _playerId = playerId;
                _conditionId = conditionId;
                _stableAfter = now + 2.0;
            }
            if (now < _stableAfter)
            {
                reason = "warming-up-current-player";
                return false;
            }

            // All reads occur synchronously on Unity's main thread via Plugin.Update.
            float health = condition.GetHP();
            float maxHealth = condition.GetMaxHP();
            float energy = condition.GetEnergy();
            float maxEnergy = condition.GetMaxEnergy();
            float stamina = condition.GetStamina();
            float maxStamina = condition.GetMaxStamina();
            if (!Finite(health) || !Positive(maxHealth) || !Finite(energy) ||
                !Positive(maxEnergy) || !Finite(stamina) || !Positive(maxStamina))
                return Unavailable("vitals-uninitialized-or-nonfinite", out reason);

            sample = new Snapshot
            {
                PlayerId = playerId,
                ConditionId = conditionId,
                SceneName = SceneManager.GetActiveScene().name,
                Health = health,
                MaxHealth = maxHealth,
                Energy = energy,
                MaxEnergy = maxEnergy,
                Stamina = stamina,
                MaxStamina = maxStamina,
                Dream = scenario.IsDream(),
                PreDream = scenario.IsPreDream(),
                Arena = player.IsInArenaMode(),
                CapturedUtc = DateTime.UtcNow,
                CapturedAt = Plugin.ClockSeconds,
                CaptureMilliseconds = (Plugin.ClockSeconds - started) * 1000.0
            };
            reason = "ready";
            return true;
        }

        private static bool Finite(float value)
        {
            return !Single.IsNaN(value) && !Single.IsInfinity(value);
        }

        private static bool Positive(float value)
        {
            return Finite(value) && value > 0.0f;
        }
    }
}
