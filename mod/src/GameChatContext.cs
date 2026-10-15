using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GreenHellAI
{
    internal sealed class GameChatContext : IFocusTarget
    {
        private static readonly FieldInfo MoveField = typeof(Player).GetField("m_BlockMovesRequestsCount", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RotationField = typeof(Player).GetField("m_BlockRotationRequestsCount", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CursorField = typeof(CursorManager).GetField("m_ShowCursorRequests", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BindingsField = typeof(InputsManager).GetField("m_ActionsByInputAction", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo KeyIndexField = typeof(InputsManager).GetField("m_ActionsByKeyCode", BindingFlags.Instance | BindingFlags.NonPublic);

        internal Player Player;
        internal PlayerConditionModule Condition;
        internal InputsManager Inputs;
        internal CursorManager Cursor;

        internal static bool TryGet(out GameChatContext context, out string reason, GameChatContext previous = null)
        {
            context = null;
            MainLevel level = MainLevel.Instance;
            LoadingScreen loading = LoadingScreen.Get();
            MenuInGameManager menus = MenuInGameManager.Get();
            Player player = Player.Get();
            PlayerConditionModule condition = PlayerConditionModule.Get();
            InputsManager inputs = InputsManager.Get();
            CursorManager cursor = CursorManager.Get();
            if (level == null || !level.isActiveAndEnabled || !level.m_LevelStarted ||
                level.IsPause() || level.m_CutscenePlaying || loading == null || loading.m_Active ||
                menus == null || menus.IsAnyScreenVisible() || SaveGame.m_State != SaveGame.State.None)
            { reason = "menu-loading-save-or-cutscene"; return false; }
            if (player == null || condition == null || inputs == null || cursor == null ||
                !player.isActiveAndEnabled || !condition.isActiveAndEnabled ||
                condition.gameObject != player.gameObject || ScenarioManager.Get() == null ||
                HUDStartSurvivalSplash.Get() == null || HUDPatchPVESplash.Get() == null ||
                HUDManager.Get() == null || HUDManager.Get().m_HUDList == null || player.IsDead())
            { reason = "player-input-or-hud-unavailable"; return false; }
            if (MoveField == null || RotationField == null || CursorField == null)
            { reason = "focus-counter-metadata-unavailable"; return false; }
            context = previous != null && previous.Player == player && previous.Condition == condition &&
                previous.Inputs == inputs && System.Object.ReferenceEquals(previous.Cursor, cursor)
                ? previous : new GameChatContext { Player = player, Condition = condition, Inputs = inputs, Cursor = cursor };
            reason = "ready";
            return true;
        }

        internal bool IsSameOwner(GameChatContext other)
        {
            return other != null && Player == other.Player && Condition == other.Condition &&
                Inputs == other.Inputs && System.Object.ReferenceEquals(Cursor, other.Cursor);
        }

        internal static bool NativeChatActive()
        {
            HUDManager manager = HUDManager.Get();
            if (manager == null || manager.m_HUDList == null) return false;
            foreach (HUDBase item in manager.m_HUDList)
            {
                HUDTextChat chat = item as HUDTextChat;
                if (chat != null && chat.isActiveAndEnabled) return true;
            }
            return false;
        }

        internal static bool NativeTextFocus()
        {
            HUDNameAnimal animal = HUDNameAnimal.Get();
            HUDNameItem item = HUDNameItem.Get();
            if ((animal != null && animal.IsActive()) || (item != null && item.IsActive()) || NativeChatActive())
                return true;
            EventSystem events = EventSystem.current;
            if (events == null || events.currentSelectedGameObject == null) return false;
            GameObject selected = events.currentSelectedGameObject;
            InputField standard = selected.GetComponent<InputField>();
            InputFieldEx custom = selected.GetComponent<InputFieldEx>();
            return (standard != null && standard.isFocused) || (custom != null && custom.isFocused);
        }

        internal bool CanOpen(out string reason)
        {
            if (Inputs.m_TextInputActive || NativeTextFocus())
            { reason = "native-text-input-active"; return false; }
            if (Player.GetMovesBlocked() || Player.GetRotationBlocked() ||
                MoveRequests != 0 || RotationRequests != 0 || CursorRequests != 0)
            { reason = "another-control-or-cursor-owner-active"; return false; }
            reason = "ready";
            return true;
        }

        internal bool BindingIsAvailable(KeyCode key, out string reason)
        {
            if (BindingsField == null || KeyIndexField == null)
            { reason = "binding-metadata-unavailable"; return false; }
            Dictionary<int, InputActionData[]> bindings = BindingsField.GetValue(Inputs) as Dictionary<int, InputActionData[]>;
            Dictionary<int, List<InputActionData>> index = KeyIndexField.GetValue(Inputs) as Dictionary<int, List<InputActionData>>;
            if (bindings == null || bindings.Count == 0 || index == null || index.Count == 0)
            { reason = "bindings-uninitialized"; return false; }
            foreach (InputActionData[] group in bindings.Values)
            {
                if (group == null) continue;
                foreach (InputActionData action in group)
                    if (Conflicts(action, key, out reason)) return false;
            }
            List<InputActionData> indexed;
            if (index.TryGetValue((int)key, out indexed) && indexed != null)
                foreach (InputActionData action in indexed)
                    if (Conflicts(action, key, out reason)) return false;
            reason = "no-current-game-action-mapping";
            return true;
        }

        private static bool Conflicts(InputActionData action, KeyCode key, out string reason)
        {
            reason = null;
            if (action == null || (action.m_KeyCode != key && action.m_Ps4KeyCode != key)) return false;
            reason = "mapped-to-action=" + action.m_Action + "/trigger=" + action.m_TriggerAction;
            return true;
        }

        public bool TextInputActive
        {
            get { return Inputs != null && Inputs.m_TextInputActive; }
            set { if (Inputs != null) Inputs.m_TextInputActive = value; }
        }
        public bool NativeChatOwnsTextFlag { get { return NativeChatActive(); } }
        public int MoveRequests { get { return Player == null ? 0 : (int)MoveField.GetValue(Player); } }
        public int RotationRequests { get { return Player == null ? 0 : (int)RotationField.GetValue(Player); } }
        public int CursorRequests { get { return Cursor == null ? 0 : (int)CursorField.GetValue(Cursor); } }
        public void AddMove() { Player.BlockMoves(); }
        public void RemoveMove() { if (Player != null) Player.UnblockMoves(); }
        public void AddRotation() { Player.BlockRotation(); }
        public void RemoveRotation() { if (Player != null) Player.UnblockRotation(); }
        public void AddCursor() { Cursor.ShowCursor(true, false); }
        public void RemoveCursor() { Cursor.ShowCursor(false, false); }
    }
}
