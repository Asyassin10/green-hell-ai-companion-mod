using System;

namespace GreenHellAI
{
    // Engine-independent transaction policy; tests exercise partial acquisition/release failures.
    internal interface IFocusTarget
    {
        bool TextInputActive { get; set; }
        bool NativeChatOwnsTextFlag { get; }
        int MoveRequests { get; }
        int RotationRequests { get; }
        int CursorRequests { get; }
        void AddMove();
        void RemoveMove();
        void AddRotation();
        void RemoveRotation();
        void AddCursor();
        void RemoveCursor();
    }

    internal sealed class FocusLease
    {
        private readonly IFocusTarget _target;
        private readonly Action<string> _report;
        private bool _text;
        private bool _move;
        private bool _rotation;
        private bool _cursor;
        private bool _released;

        private FocusLease(IFocusTarget target, Action<string> report)
        {
            _target = target;
            _report = report;
        }

        internal static FocusLease Acquire(IFocusTarget target, Action<string> report)
        {
            if (target.TextInputActive || target.NativeChatOwnsTextFlag ||
                target.MoveRequests != 0 || target.RotationRequests != 0 || target.CursorRequests != 0)
                throw new InvalidOperationException("Another input/cursor owner is active.");

            FocusLease lease = new FocusLease(target, report);
            try
            {
                lease._text = true;
                target.TextInputActive = true;
                try { target.AddMove(); }
                finally { lease._move = target.MoveRequests > 0; }
                try { target.AddRotation(); }
                finally { lease._rotation = target.RotationRequests > 0; }
                try { target.AddCursor(); }
                finally { lease._cursor = target.CursorRequests > 0; }
                return lease;
            }
            catch
            {
                lease.Release();
                throw;
            }
        }

        internal bool IsIntact
        {
            get
            {
                return !_released && _target.TextInputActive &&
                    _target.MoveRequests > 0 && _target.RotationRequests > 0 &&
                    _target.CursorRequests > 0;
            }
        }

        internal void Release()
        {
            if (_released) return;
            _released = true; // Never repeat an uncertain decrement after an exception.
            if (_cursor) Attempt(delegate { if (_target.CursorRequests > 0) _target.RemoveCursor(); });
            if (_rotation) Attempt(delegate { if (_target.RotationRequests > 0) _target.RemoveRotation(); });
            if (_move) Attempt(delegate { if (_target.MoveRequests > 0) _target.RemoveMove(); });
            if (_text) Attempt(delegate
            {
                // The inspected native chat writes this same flag and clears it on hide.
                // Naming widgets instead own their own counted movement/rotation requests.
                if (!_target.NativeChatOwnsTextFlag) _target.TextInputActive = false;
            });
        }

        private void Attempt(Action action)
        {
            try { action(); }
            catch (Exception error)
            {
                try
                {
                    if (_report != null) _report("Focus cleanup failed: " + error.GetType().Name + ": " + error.Message);
                }
                catch { /* Reporting must not prevent the remaining independent releases. */ }
            }
        }
    }

    internal sealed class ReleaseGate
    {
        private int _neutralFrames;
        internal void Reset() { _neutralFrames = 0; }
        internal bool Tick(bool anyKeyHeld)
        {
            _neutralFrames = anyKeyHeld ? 0 : _neutralFrames + 1;
            return _neutralFrames >= 2;
        }
    }
}
