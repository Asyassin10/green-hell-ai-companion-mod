using System;
using System.Globalization;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace GreenHellAI
{
    internal sealed class ChatUi
    {
        private const int WindowId = 14853201;
        private const string InputName = "GreenHellAI.PrivateChat.Input";
        private readonly Plugin _plugin;
        private readonly ManualLogSource _log;
        private readonly ConfigEntry<bool> _enabled;
        private readonly ConfigEntry<KeyCode> _key;
        private readonly ConfigEntry<int> _fontSize;
        private readonly ChatHistory _history = new ChatHistory();
        private readonly ReleaseGate _releaseGate = new ReleaseGate();
        private GameChatContext _owner;
        private FocusLease _lease;
        private bool _open;
        private bool _closing;
        private bool _faulted;
        private bool _focusInput;
        private bool _closeRequested;
        private bool _clearGuiFocus;
        private int _closedFrame;
        private int _ownedHotControl;
        private int _historyPlayerId;
        private double _nextIntegrityCheck;
        private double _nextSnapshotLabel;
        private string _draft = String.Empty;
        private string _historyText = "Your conversation starts here.";
        private string _snapshotText = "Waiting for your readings...";
        private Vector2 _scroll;
        private NotebookSkin _skin;
        private string _sampleAge = String.Empty;
        private bool _waitingForAssistant;
        private string _typingAnswer;
        private int _typingCount;
        private double _nextTypingAt;

        internal ChatUi(Plugin plugin, ManualLogSource log, ConfigFile config)
        {
            _plugin = plugin;
            _log = log;
            _enabled = config.Bind<bool>("Chat", "Enabled", true, "Private local mock chat only.");
            _key = config.Bind<KeyCode>("Chat", "ToggleKey", KeyCode.F8,
                new ConfigDescription("Use F6, F7, F8, F9, F10 or F11. Opening is refused if mapped in loaded game actions. External overlay shortcuts cannot be detected."));
            _fontSize = config.Bind<int>("Chat", "FontSize", 16,
                new ConfigDescription("IMGUI font size in screen pixels.", new AcceptableValueRange<int>(12, 24)));
            _log.LogInfo("[CHAT] Local mock UI initialized. Default toggle F8 is provisional; runtime bindings are audited before each opening.");
        }

        internal void Tick(double now)
        {
            if (_faulted) return;
            try
            {
                if (!_enabled.Value)
                { ForceClose("disabled-by-config"); return; }

                if (_lease != null)
                {
                    GameChatContext current;
                    string reason;
                    if (!GameChatContext.TryGet(out current, out reason, _owner) || !_owner.IsSameOwner(current))
                    {
                        _plugin.InvalidateSnapshot();
                        ClearHistory();
                        ForceClose("lifecycle-change/" + reason);
                        return;
                    }
                    if (GameChatContext.NativeTextFocus())
                    { ForceClose("yield-to-native-text-input"); return; }
                    if (now >= _nextIntegrityCheck)
                    {
                        _nextIntegrityCheck = now + 0.25;
                        if (!_lease.IsIntact)
                        { ForceClose("focus-lease-changed-externally"); return; }
                        if (_owner.MoveRequests > 1 || _owner.RotationRequests > 1 || _owner.CursorRequests > 1)
                        { ForceClose("yield-to-another-control-owner"); return; }
                    }
                }

                if (!String.IsNullOrEmpty(_typingAnswer) && now >= _nextTypingAt)
                {
                    _typingCount = Math.Min(_typingAnswer.Length, _typingCount + 2);
                    _nextTypingAt = now + 0.025;
                    RefreshHistoryText();
                    if (_typingCount >= _typingAnswer.Length)
                    { _typingAnswer = null; _focusInput = true; }
                }

                if (_closing)
                {
                    // Keep suppression until close-click/key releases have passed through the game input updater.
                    if (Time.frameCount != _closedFrame && _releaseGate.Tick(Input.anyKey))
                        ForceClose("close-input-released");
                    return;
                }

                // GetKeyDown is edge-triggered: one F8 press opens and the next closes.
                // The release gate below prevents a held key from immediately reopening it.
                if (_open && (_closeRequested || Input.GetKeyDown(_key.Value)))
                { RequestClose(); return; }

                if (!_open && Input.GetKeyDown(_key.Value))
                    Open(now);

                if (_open && now >= _nextSnapshotLabel)
                {
                    _nextSnapshotLabel = now + 0.25;
                    UpdateSnapshotLabel(now);
                }
            }
            catch (Exception error)
            {
                ForceClose("ui-tick-error");
                _faulted = true;
                _log.LogError("[CHAT-ERROR] UI disabled for this run; telemetry continues: " + error);
            }
        }

        private void Open(double now)
        {
            if (_key.Value < KeyCode.F6 || _key.Value > KeyCode.F11)
            { _log.LogWarning("[CHAT-BINDING] ToggleKey must be F6 through F11; opening refused."); return; }
            GameChatContext context;
            string reason;
            if (!GameChatContext.TryGet(out context, out reason) || !context.CanOpen(out reason))
            { _log.LogInfo("[CHAT] Opening refused: " + reason); return; }
            if (!context.BindingIsAvailable(_key.Value, out reason))
            {
                _log.LogWarning("[CHAT-BINDING] " + _key.Value + " opening refused: " + reason +
                    ". Choose an unused Chat.ToggleKey or change the game binding; no override is performed.");
                return;
            }
            _log.LogInfo("[CHAT-BINDING] " + _key.Value + ": " + reason + "; external overlays not audited.");
            int playerId = context.Player.GetInstanceID();
            Snapshot sample = _plugin.LatestSnapshot;
            if (_historyPlayerId != playerId)
            { ClearHistory(); _historyPlayerId = playerId; }
            if (sample != null && (sample.PlayerId != playerId || sample.ConditionId != context.Condition.GetInstanceID()))
                _plugin.InvalidateSnapshot();

            _owner = context;
            _lease = FocusLease.Acquire(context, delegate(string message) { _log.LogWarning("[CHAT-FOCUS] " + message); });
            _open = true;
            _focusInput = true;
            _closeRequested = false;
            _clearGuiFocus = false;
            _nextIntegrityCheck = now + 0.25;
            UpdateSnapshotLabel(now);
            _log.LogInfo("[CHAT] Opened private local mock panel; movement/rotation/cursor requests acquired. World is not paused.");
        }

        private void RequestClose()
        {
            if (!_open) return;
            _open = false;
            _closing = true;
            _closeRequested = false;
            _clearGuiFocus = true;
            _closedFrame = Time.frameCount;
            _releaseGate.Reset();
            _log.LogInfo("[CHAT] Closing; release held keys/mouse buttons to restore control.");
        }

        internal void ForceClose(string reason)
        {
            bool wasActive = _open || _closing || _lease != null;
            _open = false;
            _closing = false;
            _closeRequested = false;
            _clearGuiFocus = wasActive || _clearGuiFocus;
            _focusInput = false;
            FocusLease lease = _lease;
            _lease = null;
            _owner = null;
            if (lease != null) lease.Release();
            if (wasActive) _log.LogInfo("[CHAT] Closed: " + reason + "; owned focus requests released where possible.");
        }

        private void ClearHistory()
        {
            _history.Clear();
            _historyText = "Your conversation starts here.";
            _draft = String.Empty;
            _scroll = Vector2.zero;
        }

        private void UpdateSnapshotLabel(double now)
        {
            Snapshot sample = _plugin.LatestSnapshot;
            if (sample == null)
            { _snapshotText = "Waiting for your readings..."; _sampleAge = String.Empty; return; }
            _snapshotText = String.Format(CultureInfo.InvariantCulture,
                "Health     {0:0.#} / {1:0.#}\nEnergy     {2:0.#} / {3:0.#}\nStamina   {4:0.#} / {5:0.#}",
                sample.Health, sample.MaxHealth, sample.Energy, sample.MaxEnergy, sample.Stamina, sample.MaxStamina);
            _sampleAge = String.Format(CultureInfo.InvariantCulture,
                "Updated {0:0}s ago", Math.Max(0.0, now - sample.CapturedAt));
        }

        internal void Dispose()
        {
            ForceClose("notebook-disposed");
            if (_skin != null) { _skin.Dispose(); _skin = null; }
        }

        internal void Draw()
        {
            try
            {
                if (_clearGuiFocus)
                {
                    // Release only this IMGUI field/control; never overwrite another UI's selection.
                    if (GUI.GetNameOfFocusedControl() == InputName)
                        GUI.FocusControl(null);
                    if (_ownedHotControl != 0 && GUIUtility.hotControl == _ownedHotControl)
                        GUIUtility.hotControl = 0;
                    _ownedHotControl = 0;
                    _clearGuiFocus = false;
                }
                if (_faulted) return;
                if (_closing)
                {
                    GUI.Box(new Rect(16, 16, Math.Min(640, Screen.width - 32), 48),
                        "Release held keys to return to your journey.");
                    return;
                }
                if (!_open) return;
                if (Screen.width < 640 || Screen.height < 480)
                { _closeRequested = true; return; }
                if (_skin == null) _skin = new NotebookSkin();
                float width = Math.Min(1120, Screen.width - 32);
                float height = Math.Min(710, Screen.height - 32);
                _skin.Configure(Math.Max(12, (int)Math.Round(_fontSize.Value * Math.Min(1, Math.Min(width / 1000, height / 640)))));
                Rect panel = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
                bool mouseDownInside = Event.current.type == EventType.MouseDown && panel.Contains(Event.current.mousePosition);
                GUI.ModalWindow(WindowId, panel, DrawWindow, String.Empty, _skin.Window);
                if (mouseDownInside) _ownedHotControl = GUIUtility.hotControl;
            }
            catch (ExitGUIException) { throw; }
            catch (Exception error)
            {
                ForceClose("ui-render-error");
                _faulted = true;
                _log.LogError("[CHAT-ERROR] UI disabled for this run; telemetry continues: " + error);
            }
        }

        private void DrawWindow(int id)
        {
            float width = Math.Min(1120, Screen.width - 32);
            float height = Math.Min(710, Screen.height - 32);
            float center = width / 2;
            float left = Math.Max(30, width * 0.04f);
            float right = center + Math.Max(26, width * 0.035f);
            float leftWidth = center - left - 34;
            float rightWidth = width - right - left;
            _skin.DrawBook(width, height);

            GUI.Label(new Rect(left, 32, leftWidth, 28), "GREEN HELL", _skin.Small);
            GUI.Label(new Rect(left, 63, leftWidth, 54), "AI Companion", _skin.Title);
            GUI.Label(new Rect(left + 2, 116, leftWidth, 30), "by yassine-as", _skin.Small);
            float conditionTop = Math.Max(152, Math.Min(192, height * 0.30f));
            NotebookSkin.Line(left, conditionTop - 10, center - 36, conditionTop - 10,
                new Color(0.32f, 0.29f, 0.20f, 0.35f), 1);
            GUI.Label(new Rect(left, conditionTop, leftWidth, 38), "Your condition", _skin.Heading);
            float readingsHeight = _skin.Body.CalcHeight(new GUIContent(_snapshotText), leftWidth);
            GUI.Label(new Rect(left, conditionTop + 44, leftWidth, readingsHeight), _snapshotText, _skin.Body);
            GUI.Label(new Rect(left, conditionTop + 50 + readingsHeight, leftWidth, 30), _sampleAge, _skin.Small);
            float sketchTop = Math.Max(conditionTop + 90 + readingsHeight, height - 210);
            if (height - 68 - sketchTop > 34)
                NotebookSkin.LeafSketch(new Rect(left + leftWidth * 0.20f, sketchTop,
                    leftWidth * 0.60f, height - 68 - sketchTop));
            GUI.Label(new Rect(left, height - 49, leftWidth, 28), "Field notes", _skin.Small);

            GUI.Label(new Rect(right, 34, rightWidth - 82, 42), "Conversation", _skin.Heading);
            if (GUI.Button(new Rect(right + rightWidth - 76, 32, 76, 34), "Close", _skin.Button))
                _closeRequested = true;
            Rect historyRect = new Rect(right, 88, rightWidth, height - 288);
            NotebookSkin.Rule(historyRect, _skin.Body.lineHeight + 8);
            float contentWidth = historyRect.width - 22;
            float contentHeight = Math.Max(historyRect.height,
                _skin.History.CalcHeight(new GUIContent(_historyText), contentWidth - 8) + 12);
            _scroll = GUI.BeginScrollView(historyRect, _scroll, new Rect(0, 0, contentWidth, contentHeight));
            try { GUI.Label(new Rect(4, 4, contentWidth - 8, contentHeight), _historyText, _skin.History); }
            finally { GUI.EndScrollView(); }

            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            { _closeRequested = true; current.Use(); }
            bool inputFocused = GUI.GetNameOfFocusedControl() == InputName;
            if (current.type == EventType.KeyDown && inputFocused &&
                (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter) &&
                String.IsNullOrEmpty(Input.compositionString))
            { Send(); current.Use(); }

            GUI.Label(new Rect(right, height - 188, rightWidth, 30), "Write a message", _skin.Heading);
            Rect writingRect = new Rect(right, height - 150, rightWidth, 78);
            NotebookSkin.Rule(writingRect, _skin.Body.lineHeight + 8);
            GUI.SetNextControlName(InputName);
            _draft = GUI.TextArea(writingRect, _draft, ChatHistory.MaximumInputLength, _skin.Field);
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = previousEnabled && !String.IsNullOrWhiteSpace(_draft);
                if (GUI.Button(new Rect(right + rightWidth - 94, height - 60, 94, 34), "Send", _skin.Button)) Send();
            }
            finally { GUI.enabled = previousEnabled; }
            GUI.Label(new Rect(right, height - 55, rightWidth - 100, 28), "Enter to send", _skin.Small);
            if (_focusInput && current.type == EventType.Repaint)
            { GUI.FocusControl(InputName); _focusInput = false; }
        }

        private void Send()
        {
            // Rechecked on the main thread at submission; never send through native game chat.
            GameChatContext current;
            string reason;
            if (_lease == null || !GameChatContext.TryGet(out current, out reason, _owner) ||
                !_owner.IsSameOwner(current) || GameChatContext.NativeTextFocus())
            { _closeRequested = true; return; }
            if (_waitingForAssistant || String.IsNullOrWhiteSpace(_draft)) return;
            string message = _draft;
            Snapshot sample = _plugin.LatestSnapshot;
            _history.AddUser(message);
            RefreshHistoryText();
            _draft = String.Empty;
            _waitingForAssistant = true;
            _plugin.RequestAssistant(message, sample, delegate(string answer, string error)
            {
                _waitingForAssistant = false;
                string visibleAnswer = !String.IsNullOrEmpty(answer) ? answer : "I’m unavailable right now. Please try again.";
                _history.AddAssistant(visibleAnswer);
                _typingAnswer = visibleAnswer;
                _typingCount = 0;
                _nextTypingAt = Plugin.ClockSeconds;
                RefreshHistoryText();
                _scroll.y = Single.MaxValue;
            });
            _log.LogInfo("[CHAT] Assistant request submitted; message content is not written to the log.");
        }

        private void RefreshHistoryText()
        {
            System.Collections.Generic.List<string> entries = new System.Collections.Generic.List<string>(_history.Entries);
            if (!String.IsNullOrEmpty(_typingAnswer) && entries.Count > 0)
            {
                entries.RemoveAt(entries.Count - 1);
                entries.Add("Companion: " + _typingAnswer.Substring(0, Math.Min(_typingCount, _typingAnswer.Length)));
            }
            System.Text.StringBuilder rendered = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) rendered.Append("\n\n");
                string entry = entries[i] ?? String.Empty;
                const string companion = "Companion: ";
                if (entry.StartsWith(companion, StringComparison.Ordinal))
                    rendered.Append("Companion: ").Append(MarkdownRichText.Render(entry.Substring(companion.Length)));
                else
                    rendered.Append(MarkdownRichText.Render(entry));
            }
            _historyText = rendered.Length == 0 ? "Your conversation starts here." : rendered.ToString();
        }
    }
}
