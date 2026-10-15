using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: AssemblyTitle("GreenHellAI Phase 2")]
[assembly: AssemblyDescription("Read-only telemetry and private notebook chat")]
[assembly: AssemblyVersion("0.2.1.0")]
[assembly: AssemblyFileVersion("0.2.1.0")]
[assembly: TargetFramework(".NETFramework,Version=v4.0", FrameworkDisplayName = ".NET Framework 4")]

namespace GreenHellAI
{
    [BepInPlugin(PluginId, PluginName, PluginVersion)]
    [BepInProcess("GH.exe")]
    [DefaultExecutionOrder(-10000)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginId = "local.greenhellai.phase1";
        public const string PluginName = "Green Hell AI Companion";
        public const string PluginVersion = "0.3.0";
        internal const string ExpectedGameHash =
            "40279b7fda17867f4cfc3e3be0e3bbae7df20bdb5b3b3c9fde0a2827d9862838";

        private ConfigEntry<bool> _sampleEnabled;
        private ConfigEntry<double> _interval;
        private readonly GameStateReader _reader = new GameStateReader();
        private bool _compatibleBuild;
        private bool _quitting;
        private double _nextAttempt;
        private double _nextErrorDetail;
        private string _lastState;
        private long _samples;
        private ChatUi _chat;
        private BackendClient _backend;
        private Snapshot _latest;

        internal Snapshot LatestSnapshot { get { return _latest; } }
        internal void InvalidateSnapshot() { _latest = null; }

        internal static double ClockSeconds
        {
            get { return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency; }
        }

        private void Awake()
        {
            try
            {
                _sampleEnabled = Config.Bind<bool>("Telemetry", "Enabled", true,
                    "Read and log HP, energy and stamina only. No gameplay changes.");
                _interval = Config.Bind<double>("Telemetry", "SampleIntervalSeconds", 10.0,
                    new ConfigDescription("Seconds between main-thread probes. Range 2-120.",
                        new AcceptableValueRange<double>(2.0, 120.0)));
                ConfigEntry<bool> backendEnabled = Config.Bind<bool>("Backend", "Enabled", true,
                    "Use the local FastAPI companion service. Credentials remain in the backend environment.");
                ConfigEntry<string> backendUrl = Config.Bind<string>("Backend", "Url", "http://127.0.0.1:8000",
                    "Local FastAPI URL; keep this bound to localhost unless you explicitly secure it.");
                ConfigEntry<int> backendTimeout = Config.Bind<int>("Backend", "TimeoutSeconds", 45,
                    new ConfigDescription("HTTP request timeout.", new AcceptableValueRange<int>(5, 120)));
                if (backendEnabled.Value) _backend = new BackendClient(backendUrl.Value, backendTimeout.Value);

                Logger.LogInfo("[LIFECYCLE] Awake; plugin=" + PluginVersion +
                    "; unity=" + Application.unityVersion +
                    "; process64=" + Environment.Is64BitProcess +
                    "; runtime=" + Environment.Version +
                    "; bepinex=" + typeof(BaseUnityPlugin).Assembly.GetName().Version);

                // File read only. Never open the assembly for writing or load it here.
                string assemblyPath = Path.Combine(Paths.GameRootPath,
                    "GH_Data", "Managed", "Assembly-CSharp.dll");
                string hash;
                using (FileStream stream = File.OpenRead(assemblyPath))
                using (SHA256 sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();

                _compatibleBuild = Environment.Is64BitProcess &&
                    Application.unityVersion == "2021.2.20f1" &&
                    String.Equals(hash, ExpectedGameHash, StringComparison.OrdinalIgnoreCase);

                Logger.LogInfo("[BUILD] Assembly-CSharp SHA256=" + hash);
                if (!_compatibleBuild)
                    Logger.LogError("[BUILD] Unsupported build/platform. Telemetry is disabled. " +
                        "Expected Unity 2021.2.20f1 x64 and SHA256=" + ExpectedGameHash +
                        ". Reinspect/rebuild instead of bypassing this guard.");
                else
                    Logger.LogInfo("[BUILD] Static target matched. Runtime data reading still requires your test.");

                if (_compatibleBuild)
                {
                    try { _chat = new ChatUi(this, Logger, Config); }
                    catch (Exception error) { Logger.LogError("[CHAT-ERROR] UI initialization failed; telemetry retained: " + error); }
                }

                _nextAttempt = ClockSeconds + 2.0;
            }
            catch (Exception exception)
            {
                _compatibleBuild = false;
                Logger.LogError("[STARTUP-ERROR] Telemetry disabled: " + exception);
            }
        }

        internal void RequestAssistant(string message, Snapshot snapshot, Action<string, string> completed)
        {
            if (_backend == null)
            { if (completed != null) completed(null, "backend-disabled"); return; }
            StartCoroutine(_backend.Ask(message, snapshot, completed));
        }

        private void OnEnable()
        {
            _reader.Reset();
            _lastState = null;
            _nextAttempt = ClockSeconds + 2.0;
            Logger.LogInfo("[LIFECYCLE] OnEnable");
        }

        private void Start()
        {
            Logger.LogInfo("[LIFECYCLE] Start; notebook UI and optional local FastAPI Bedrock backend.");
        }

        private void Update()
        {
            // Closed chat checks its toggle only. Active chat checks owner lifetime each frame;
            // the original statistics probe and sample logging remain interval-gated.
            if (_quitting || !_compatibleBuild || _sampleEnabled == null || _interval == null)
                return;

            double now = ClockSeconds;
            // UI failures are isolated from the Phase 1 sampling path.
            if (_chat != null)
                _chat.Tick(now);
            if (now < _nextAttempt)
                return;
            double seconds = _interval.Value;
            if (Double.IsNaN(seconds) || Double.IsInfinity(seconds))
                seconds = 10.0;
            _nextAttempt = now + Math.Max(2.0, Math.Min(120.0, seconds));

            if (!_sampleEnabled.Value)
            {
                _reader.Reset();
                _latest = null;
                SetState("disabled-by-config");
                return;
            }

            try
            {
                Snapshot sample;
                string reason;
                if (!_reader.TryCapture(now, out sample, out reason))
                {
                    _latest = null;
                    SetState(reason);
                    return;
                }

                SetState("ready/player=" + sample.PlayerId + "/condition=" + sample.ConditionId);
                _latest = sample;
                _samples++;
                Logger.LogInfo(String.Format(CultureInfo.InvariantCulture,
                    "[SAMPLE {0}] utc={1:O}; scene={2}; player={3}; condition={4}; " +
                    "hp={5:0.###}/{6:0.###}; energy={7:0.###}/{8:0.###}; " +
                    "stamina={9:0.###}/{10:0.###}; dream={11}; preDream={12}; arena={13}; " +
                    "semantics=effective-getters; captureMs={14:0.###}",
                    _samples, DateTime.UtcNow, sample.SceneName, sample.PlayerId, sample.ConditionId,
                    sample.Health, sample.MaxHealth, sample.Energy, sample.MaxEnergy,
                    sample.Stamina, sample.MaxStamina, sample.Dream, sample.PreDream, sample.Arena,
                    sample.CaptureMilliseconds));
            }
            catch (Exception exception)
            {
                // Reacquire next interval; no saved Unity references or repeated per-frame exceptions.
                _reader.Reset();
                _latest = null;
                SetState("capture-error");
                if (now >= _nextErrorDetail)
                {
                    _nextErrorDetail = now + 60.0;
                    Logger.LogWarning("[CAPTURE-ERROR] Retry at next configured interval; " + exception);
                }
            }
        }

        private void SetState(string state)
        {
            if (String.Equals(_lastState, state, StringComparison.Ordinal))
                return;
            _lastState = state;
            Logger.LogInfo("[STATE] " + state);
        }

        private void OnDisable()
        {
            if (_chat != null) _chat.ForceClose("plugin-disabled");
            _latest = null;
            _reader.Reset();
            _lastState = null;
            Logger.LogInfo("[LIFECYCLE] OnDisable; chat focus released where owners are alive");
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
            if (_chat != null) _chat.ForceClose("application-quit");
            _reader.Reset();
            Logger.LogInfo("[LIFECYCLE] OnApplicationQuit; successful samples=" + _samples);
        }

        private void OnDestroy()
        {
            if (_chat != null) _chat.Dispose();
            _reader.Reset();
            Logger.LogInfo("[LIFECYCLE] OnDestroy; successful samples=" + _samples);
        }

        private void OnGUI()
        {
            if (_chat != null && _compatibleBuild && !_quitting)
                _chat.Draw();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && _chat != null) _chat.ForceClose("application-focus-lost");
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _chat != null) _chat.ForceClose("application-paused");
        }
    }
}
