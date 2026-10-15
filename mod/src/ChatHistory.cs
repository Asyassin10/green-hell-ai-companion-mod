using System;
using System.Collections.Generic;
using System.Globalization;

namespace GreenHellAI
{
    internal sealed class ChatHistory
    {
        internal const int MaximumInputLength = 512;
        internal const int MaximumEntries = 40;
        private readonly List<string> _entries = new List<string>();
        internal IList<string> Entries { get { return _entries.AsReadOnly(); } }

        internal void Clear() { _entries.Clear(); }

        internal void AddUser(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return;
            text = text.Trim().Replace('\r', ' ').Replace('\n', ' ');
            if (text.Length > MaximumInputLength) text = text.Substring(0, MaximumInputLength);
            Append("You: " + text);
        }

        internal void AddAssistant(string text)
        {
            if (!String.IsNullOrWhiteSpace(text)) Append("Companion: " + text.Trim());
        }

        internal bool Send(string text, Snapshot snapshot, double now)
        {
            if (String.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (text.Length > MaximumInputLength) text = text.Substring(0, MaximumInputLength);
            text = text.Replace('\r', ' ').Replace('\n', ' ');
            AddUser(text);
            if (snapshot == null)
                Append("Companion: I am here. Your latest readings aren't available yet.");
            else
                Append(String.Format(CultureInfo.InvariantCulture,
                    "Companion: Message received. Your latest readings are HP {0:0.###}/{1:0.###}, " +
                    "energy {2:0.###}/{3:0.###}, stamina {4:0.###}/{5:0.###} (updated {6:0.0}s ago).",
                    snapshot.Health, snapshot.MaxHealth, snapshot.Energy, snapshot.MaxEnergy,
                    snapshot.Stamina, snapshot.MaxStamina, Math.Max(0.0, now - snapshot.CapturedAt)));
            return true;
        }

        private void Append(string text)
        {
            _entries.Add(text);
            if (_entries.Count > MaximumEntries) _entries.RemoveAt(0);
        }
    }
}
