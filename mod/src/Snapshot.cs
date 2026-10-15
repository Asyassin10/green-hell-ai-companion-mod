using System;

namespace GreenHellAI
{
    internal sealed class Snapshot
    {
        internal int PlayerId;
        internal int ConditionId;
        internal string SceneName;
        internal float Health;
        internal float MaxHealth;
        internal float Energy;
        internal float MaxEnergy;
        internal float Stamina;
        internal float MaxStamina;
        internal bool Dream;
        internal bool PreDream;
        internal bool Arena;
        internal double CaptureMilliseconds;
        internal DateTime CapturedUtc;
        internal double CapturedAt;
    }
}
