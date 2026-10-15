using System;
using GreenHellAI;

internal sealed class FakeTarget : IFocusTarget
{
    public bool TextInputActive { get; set; }
    public bool NativeChatOwnsTextFlag { get; set; }
    public int MoveRequests { get; set; }
    public int RotationRequests { get; set; }
    public int CursorRequests { get; set; }
    public string FailAfter;
    private void Fail(string point) { if (FailAfter == point) throw new Exception(point); }
    public void AddMove() { MoveRequests++; Fail("add-move"); }
    public void AddRotation() { RotationRequests++; Fail("add-rotation"); }
    public void AddCursor() { CursorRequests++; Fail("add-cursor"); }
    public void RemoveMove() { MoveRequests--; Fail("remove-move"); }
    public void RemoveRotation() { RotationRequests--; Fail("remove-rotation"); }
    public void RemoveCursor() { CursorRequests--; Fail("remove-cursor"); }
}

internal static class PolicyTests
{
    private static int _checks;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL: " + name); _checks++; }
    private static void Released(FakeTarget target, string name)
    {
        Check(!target.TextInputActive && target.MoveRequests == 0 &&
            target.RotationRequests == 0 && target.CursorRequests == 0, name);
    }
    private static bool Refused(FakeTarget target)
    {
        try { FocusLease.Acquire(target, null); return false; }
        catch (InvalidOperationException) { return true; }
    }
    public static int Main()
    {
        FakeTarget target = new FakeTarget();
        FocusLease lease = FocusLease.Acquire(target, null);
        Check(lease.IsIntact, "all suppression resources acquired");
        target.MoveRequests++; target.RotationRequests++; target.CursorRequests++;
        lease.Release(); lease.Release();
        Check(!target.TextInputActive && target.MoveRequests == 1 &&
            target.RotationRequests == 1 && target.CursorRequests == 1,
            "release once preserves later owners' counted requests");
        Check(!lease.IsIntact, "released lease no longer intact");
        Check(Refused(new FakeTarget { TextInputActive = true }), "refuse existing text owner");
        Check(Refused(new FakeTarget { NativeChatOwnsTextFlag = true }), "refuse native chat");
        Check(Refused(new FakeTarget { MoveRequests = 1 }), "refuse move owner");
        Check(Refused(new FakeTarget { RotationRequests = 1 }), "refuse rotation owner");
        Check(Refused(new FakeTarget { CursorRequests = 1 }), "refuse cursor owner");

        foreach (string failure in new[] { "add-move", "add-rotation", "add-cursor" })
        {
            target = new FakeTarget { FailAfter = failure };
            bool threw = false;
            try { FocusLease.Acquire(target, null); }
            catch (Exception) { threw = true; }
            Check(threw, "propagate partial acquisition " + failure);
            Released(target, "rollback after mutation " + failure);
        }
        foreach (string failure in new[] { "remove-move", "remove-rotation", "remove-cursor" })
        {
            target = new FakeTarget();
            int reports = 0;
            lease = FocusLease.Acquire(target, delegate { reports++; throw new Exception("logger failure"); });
            target.FailAfter = failure;
            lease.Release(); lease.Release();
            Released(target, "independent cleanup despite error " + failure);
            Check(reports == 1, "cleanup error reported once " + failure);
        }
        target = new FakeTarget(); lease = FocusLease.Acquire(target, null);
        target.NativeChatOwnsTextFlag = true;
        lease.Release();
        Check(target.TextInputActive && target.MoveRequests == 0 && target.CursorRequests == 0,
            "native chat retains shared text flag");
        target = new FakeTarget(); lease = FocusLease.Acquire(target, null);
        target.MoveRequests = target.RotationRequests = target.CursorRequests = 0;
        Check(!lease.IsIntact, "detect externally reset counters");
        lease.Release(); Released(target, "never decrement zero counters");

        ReleaseGate gate = new ReleaseGate();
        Check(!gate.Tick(true) && !gate.Tick(false) && !gate.Tick(true) &&
            !gate.Tick(false) && gate.Tick(false), "require two consecutive neutral frames");
        gate.Reset(); Check(!gate.Tick(false), "new close requires new release gate");

        string rich = MarkdownRichText.Render("**clean water** *hint* <b>unsafe</b> &");
        Check(rich.Contains("<b>clean water</b>") && rich.Contains("<i>hint</i>"), "render supported Markdown subset");
        Check(rich.Contains("&lt;b&gt;unsafe&lt;/b&gt;") && rich.Contains("&amp;"), "escape arbitrary rich text input");

        ChatHistory history = new ChatHistory();
        Check(!history.Send(" \t\r\n", null, 0) && history.Entries.Count == 0, "ignore empty submission");
        Check(history.Send(" hello\r\nworld ", null, 0), "accept local message");
        Check(history.Entries[0] == "You: hello  world" && history.Entries[1].Contains("readings aren't available yet"),
            "normalize multiline input and disclose missing snapshot");
        Snapshot sample = new Snapshot {
            PlayerId = 1, ConditionId = 2, SceneName = "synthetic-test", Health = 25,
            MaxHealth = 80, Energy = 40, MaxEnergy = 100, Stamina = 10,
            MaxStamina = 40, Dream = false, PreDream = false, Arena = false,
            CaptureMilliseconds = 0, CapturedUtc = DateTime.UtcNow, CapturedAt = 5
        };
        history.Send("test", sample, 8.5);
        string reply = history.Entries[3];
        Check(reply.Contains("HP 25/80") && reply.Contains("energy 40/100") &&
            reply.Contains("stamina 10/40") && reply.Contains("3.5s ago"), "format supplied synthetic values and age");
        history.Send(new string('x', 1000), null, 0);
        Check(history.Entries[4].Length == 5 + ChatHistory.MaximumInputLength, "bound submitted input");
        for (int i = 0; i < 100; i++) history.Send("message " + i, null, 0);
        Check(history.Entries.Count == ChatHistory.MaximumEntries &&
            history.Entries[0] == "You: message 80", "bound history to latest 20 exchanges");
        history.Clear(); Check(history.Entries.Count == 0, "clear session history");
        Console.WriteLine("PASS: " + _checks + " policy assertions. Synthetic data only; Unity/game not executed.");
        return 0;
    }
}
