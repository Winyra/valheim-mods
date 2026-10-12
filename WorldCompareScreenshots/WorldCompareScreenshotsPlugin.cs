using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace WorldCompareScreenshots
{
    [BepInPlugin("winyra.worldcomparescreenshots", "WorldCompareScreenshots", "1.0.0")]
    public class WorldCompareScreenshotsPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "winyra.worldcomparescreenshots";
        public const string ModName = "WorldCompareScreenshots";
        public const string ModVersion = "1.0.0";

        // Hotkey configs
        private ConfigEntry<KeyboardShortcut> _addWaypointKey;
        private ConfigEntry<KeyboardShortcut> _captureCurrentKey;
        private ConfigEntry<KeyboardShortcut> _captureSequenceKey;
        private ConfigEntry<KeyboardShortcut> _nextWaypointKey;
        private ConfigEntry<KeyboardShortcut> _prevWaypointKey;
        private ConfigEntry<KeyboardShortcut> _toggleMenuKey;

        // Settings configs
        private ConfigEntry<string> _screenshotDirectoryConfig;
        private ConfigEntry<float> _settleDelayConfig;
        private ConfigEntry<bool> _hideHudDuringCapture;
        private ConfigEntry<bool> _enableFlyOnTeleport;
        private ConfigEntry<string> _worldNameOverride;

        // Waypoints state
        private readonly List<ScreenshotWaypoint> _waypoints = new List<ScreenshotWaypoint>();
        private int _selectedWaypointIndex = 0;
        private string _waypointsFilePath;
        private string _resolvedScreenshotDir;

        // Capture state
        private bool _isCapturing = false;
        private Coroutine _activeCaptureCoroutine;

        // GUI state
        private bool _showGui = false;
        private Rect _windowRect = new Rect(40, 40, 480, 520);
        private Vector2 _scrollPosition = Vector2.zero;
        private string _newWaypointNameInput = "";

        // Notification banner
        private string _statusBannerText = "";
        private float _statusBannerTimer = 0f;

        private void Awake()
        {
            // Bind Hotkeys
            _addWaypointKey = Config.Bind("Hotkeys", "AddWaypointKey",
                new KeyboardShortcut(KeyCode.F6),
                "Save current camera and player position as a new comparison waypoint.");

            _captureCurrentKey = Config.Bind("Hotkeys", "CaptureCurrentKey",
                new KeyboardShortcut(KeyCode.F7),
                "Capture screenshot at the current active waypoint.");

            _captureSequenceKey = Config.Bind("Hotkeys", "CaptureSequenceKey",
                new KeyboardShortcut(KeyCode.F8),
                "Capture screenshots for all saved waypoints in sequence.");

            _prevWaypointKey = Config.Bind("Hotkeys", "PrevWaypointKey",
                new KeyboardShortcut(KeyCode.PageUp),
                "Select and teleport to previous waypoint.");

            _nextWaypointKey = Config.Bind("Hotkeys", "NextWaypointKey",
                new KeyboardShortcut(KeyCode.PageDown),
                "Select and teleport to next waypoint.");

            _toggleMenuKey = Config.Bind("Hotkeys", "ToggleMenuKey",
                new KeyboardShortcut(KeyCode.F10),
                "Toggle the in-game waypoint management GUI.");

            // Bind Settings
            _screenshotDirectoryConfig = Config.Bind("Settings", "ScreenshotDirectory",
                "screenshots/WorldCompare",
                "Directory where comparison screenshots are saved (relative to BepInEx root folder or absolute path).");

            _settleDelayConfig = Config.Bind("Settings", "SettleDelaySeconds",
                1.5f,
                new ConfigDescription("Seconds to wait after teleporting before capturing (allows terrain/chunks to render).",
                    new AcceptableValueRange<float>(0.1f, 10f)));

            _hideHudDuringCapture = Config.Bind("Settings", "HideHudDuringCapture",
                true,
                "Temporarily hide the in-game HUD and UI when taking screenshots.");

            _enableFlyOnTeleport = Config.Bind("Settings", "EnableFlyOnTeleport",
                true,
                "Automatically enable player flight/kinematic state on teleport to prevent falling from aerial waypoints.");

            _worldNameOverride = Config.Bind("Settings", "WorldNameOverride",
                "",
                "Optional custom world name tag in screenshot filenames. Leave empty to use Valheim world name.");

            // Resolve file paths
            _waypointsFilePath = Path.Combine(Paths.ConfigPath, "WorldCompareScreenshots_waypoints.json");

            string rawDir = _screenshotDirectoryConfig.Value;
            if (string.IsNullOrWhiteSpace(rawDir))
            {
                rawDir = "screenshots/WorldCompare";
            }

            if (Path.IsPathRooted(rawDir))
            {
                _resolvedScreenshotDir = rawDir;
            }
            else
            {
                string bepinexDir = Paths.BepInExRootPath;
                if (string.IsNullOrEmpty(bepinexDir) || !Directory.Exists(bepinexDir))
                {
                    bepinexDir = Paths.GameRootPath;
                }
                _resolvedScreenshotDir = Path.Combine(bepinexDir, rawDir);
            }

            try
            {
                Directory.CreateDirectory(_resolvedScreenshotDir);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not pre-create screenshot directory '{_resolvedScreenshotDir}': {ex.Message}");
            }

            // Load saved waypoints from disk
            LoadWaypointsFromDisk();

            ValheimAdapter.Initialize();

            Logger.LogInfo($"{ModName} v{ModVersion} loaded.");
            Logger.LogInfo($"Waypoints storage: {_waypointsFilePath}");
            Logger.LogInfo($"Screenshots directory: {_resolvedScreenshotDir}");
            Logger.LogInfo($"Keybinds: Add=[{_addWaypointKey.Value}], Capture=[{_captureCurrentKey.Value}], Sequence=[{_captureSequenceKey.Value}], Menu=[{_toggleMenuKey.Value}]");
        }

        private void Update()
        {
            if (_statusBannerTimer > 0f)
            {
                _statusBannerTimer -= Time.deltaTime;
            }

            // If a capture routine is running, ignore hotkeys
            if (_isCapturing) return;

            if (_toggleMenuKey.Value.IsDown())
            {
                _showGui = !_showGui;
            }

            if (_addWaypointKey.Value.IsDown())
            {
                AddCurrentLocationAsWaypoint();
            }

            if (_captureCurrentKey.Value.IsDown())
            {
                CaptureCurrentWaypoint();
            }

            if (_captureSequenceKey.Value.IsDown())
            {
                CaptureAllSequence();
            }

            if (_nextWaypointKey.Value.IsDown())
            {
                CycleWaypoint(1);
            }

            if (_prevWaypointKey.Value.IsDown())
            {
                CycleWaypoint(-1);
            }
        }

        public string GetEffectiveWorldName()
        {
            if (!string.IsNullOrWhiteSpace(_worldNameOverride.Value))
            {
                return _worldNameOverride.Value.Trim();
            }

            string detected = ValheimAdapter.GetCurrentWorldName();
            return string.IsNullOrEmpty(detected) ? "UnknownWorld" : detected;
        }

        public void AddCurrentLocationAsWaypoint(string customName = null)
        {
            Vector3 pos = ValheimAdapter.GetCurrentCameraPosition();
            Quaternion rot = ValheimAdapter.GetCurrentCameraRotation();
            float fov = ValheimAdapter.GetCurrentFov();
            float cameraDistance = ValheimAdapter.GetCurrentCameraDistance();
            bool isFreeFly = ValheimAdapter.IsFreeFlyActive();
            string worldName = GetEffectiveWorldName();

            string name = customName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Waypoint_{_waypoints.Count + 1}";
            }

            var wp = ScreenshotWaypoint.Create(pos, rot, fov, cameraDistance, name, worldName, isFreeFly);
            _waypoints.Add(wp);
            _selectedWaypointIndex = _waypoints.Count - 1;

            SaveWaypointsToDisk();

            string msg = $"Waypoint saved: {wp.name} at ({pos.x:F1}, {pos.y:F1}, {pos.z:F1}) zoom={cameraDistance:F1}";
            SetStatusBanner(msg);
            Logger.LogInfo(msg);
            ValheimAdapter.ShowGameMessage(msg);
        }

        public void TeleportToSelectedWaypoint()
        {
            if (_waypoints.Count == 0)
            {
                SetStatusBanner("No waypoints saved yet. Press F6 to add one.");
                return;
            }

            if (_selectedWaypointIndex < 0 || _selectedWaypointIndex >= _waypoints.Count)
            {
                _selectedWaypointIndex = 0;
            }

            var wp = _waypoints[_selectedWaypointIndex];
            ValheimAdapter.TeleportToWaypoint(wp, _enableFlyOnTeleport.Value);

            string msg = $"Teleported to [{_selectedWaypointIndex + 1}/{_waypoints.Count}] {wp.name}";
            SetStatusBanner(msg);
            Logger.LogInfo(msg);
            ValheimAdapter.ShowGameMessage(msg);
        }

        public void CycleWaypoint(int delta)
        {
            if (_waypoints.Count == 0)
            {
                SetStatusBanner("No waypoints saved yet.");
                return;
            }

            if (delta > 0)
            {
                _selectedWaypointIndex = WaypointStore.GetNextIndex(_selectedWaypointIndex, _waypoints.Count);
            }
            else
            {
                _selectedWaypointIndex = WaypointStore.GetPreviousIndex(_selectedWaypointIndex, _waypoints.Count);
            }

            TeleportToSelectedWaypoint();
        }

        public void CaptureCurrentWaypoint()
        {
            if (_isCapturing) return;

            ScreenshotWaypoint targetWp = null;
            int idx = -1;

            if (_waypoints.Count > 0)
            {
                if (_selectedWaypointIndex >= 0 && _selectedWaypointIndex < _waypoints.Count)
                {
                    targetWp = _waypoints[_selectedWaypointIndex];
                    idx = _selectedWaypointIndex;
                }
            }

            _activeCaptureCoroutine = StartCoroutine(CaptureSingleRoutine(targetWp, idx));
        }

        public void CaptureAllSequence()
        {
            if (_isCapturing) return;

            if (_waypoints.Count == 0)
            {
                SetStatusBanner("No waypoints saved. Add waypoints with F6 first.");
                return;
            }

            _activeCaptureCoroutine = StartCoroutine(CaptureSequenceRoutine());
        }

        private IEnumerator WaitForTeleportAndSettle(ScreenshotWaypoint wp)
        {
            // Wait for player teleporting state to clear (up to 20s)
            float teleportWait = 0f;
            while (ValheimAdapter.IsPlayerTeleporting() && teleportWait < 20f)
            {
                teleportWait += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // Wait for loading screen / black screen fade to clear (up to 5s)
            float loadingWait = 0f;
            while (ValheimAdapter.IsLoadingScreenVisible() && loadingWait < 5f)
            {
                loadingWait += 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // Ensure loading screen is hidden during screenshot
            ValheimAdapter.SetLoadingScreenVisible(false);

            // Settle delay for terrain meshes, textures, shadows, and LODs to render
            float settle = Mathf.Max(0.5f, _settleDelayConfig.Value);
            yield return new WaitForSecondsRealtime(settle);

            // Re-apply exact waypoint transform, rotation, FOV, and camera distance/zoom
            if (wp != null)
            {
                ValheimAdapter.ApplyCameraTransform(wp);
            }
        }

        private IEnumerator CaptureSingleRoutine(ScreenshotWaypoint wp, int index)
        {
            _isCapturing = true;
            string worldName = GetEffectiveWorldName();
            SetStatusBanner($"Capturing screenshot for {worldName}...");

            if (wp != null)
            {
                ValheimAdapter.TeleportToWaypoint(wp, _enableFlyOnTeleport.Value);
            }

            yield return StartCoroutine(WaitForTeleportAndSettle(wp));

            bool hideHud = _hideHudDuringCapture.Value;
            if (hideHud)
            {
                ValheimAdapter.SetHudVisible(false);
            }

            yield return new WaitForEndOfFrame();

            string fileName = WaypointStore.GenerateScreenshotFileName(wp, index, worldName, DateTime.Now);
            string fullPath = Path.Combine(_resolvedScreenshotDir, fileName);

            CaptureScreenToFile(fullPath);

            if (hideHud)
            {
                ValheimAdapter.SetHudVisible(true);
            }
            ValheimAdapter.SetLoadingScreenVisible(true);

            _isCapturing = false;
            string msg = $"Saved: {fileName}";
            SetStatusBanner(msg);
            Logger.LogInfo($"Screenshot captured: {fullPath}");
            ValheimAdapter.ShowGameMessage(msg);
        }

        private IEnumerator CaptureSequenceRoutine()
        {
            _isCapturing = true;
            string worldName = GetEffectiveWorldName();
            int total = _waypoints.Count;

            Logger.LogInfo($"Starting sequence capture of {total} waypoints for world '{worldName}'...");
            SetStatusBanner($"Starting sequence capture: 0/{total}");

            bool hideHud = _hideHudDuringCapture.Value;

            for (int i = 0; i < total; i++)
            {
                var wp = _waypoints[i];
                _selectedWaypointIndex = i;

                SetStatusBanner($"Capturing [{i + 1}/{total}] {wp.name}...");
                ValheimAdapter.TeleportToWaypoint(wp, _enableFlyOnTeleport.Value);

                yield return StartCoroutine(WaitForTeleportAndSettle(wp));

                if (hideHud)
                {
                    ValheimAdapter.SetHudVisible(false);
                }

                yield return new WaitForEndOfFrame();

                string fileName = WaypointStore.GenerateScreenshotFileName(wp, i, worldName, DateTime.Now);
                string fullPath = Path.Combine(_resolvedScreenshotDir, fileName);

                CaptureScreenToFile(fullPath);

                if (hideHud)
                {
                    ValheimAdapter.SetHudVisible(true);
                }
                ValheimAdapter.SetLoadingScreenVisible(true);

                Logger.LogInfo($"[{i + 1}/{total}] Captured: {fileName}");
                yield return new WaitForSecondsRealtime(0.5f);
            }

            _isCapturing = false;
            string completeMsg = $"Sequence complete: {total} screenshots saved to {_resolvedScreenshotDir}";
            SetStatusBanner(completeMsg);
            Logger.LogInfo(completeMsg);
            ValheimAdapter.ShowGameMessage(completeMsg);
        }

        private void CaptureScreenToFile(string fullPath)
        {
            try
            {
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                int w = Screen.width;
                int h = Screen.height;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                byte[] bytes = ImageConversion.EncodeToPNG(tex);
                Destroy(tex);
                File.WriteAllBytes(fullPath, bytes);

                if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
                {
                    Logger.LogInfo($"Saved screenshot: {fullPath} ({new FileInfo(fullPath).Length} bytes)");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"ReadPixels capture error: {ex.Message}. Falling back to ScreenCapture.");
            }

            try
            {
                ScreenCapture.CaptureScreenshot(fullPath);
                Logger.LogInfo($"Saved screenshot via ScreenCapture to: {fullPath}");
            }
            catch (Exception fallbackEx)
            {
                Logger.LogError($"Failed to capture screenshot: {fallbackEx.Message}");
            }
        }

        private void LoadWaypointsFromDisk()
        {
            _waypoints.Clear();
            var loaded = WaypointStore.LoadFromFile(_waypointsFilePath);
            if (loaded != null && loaded.Count > 0)
            {
                _waypoints.AddRange(loaded);
                _selectedWaypointIndex = Mathf.Clamp(_selectedWaypointIndex, 0, _waypoints.Count - 1);
            }
        }

        private void SaveWaypointsToDisk()
        {
            WaypointStore.SaveToFile(_waypointsFilePath, _waypoints);
        }

        private void SetStatusBanner(string text, float duration = 3.5f)
        {
            _statusBannerText = text;
            _statusBannerTimer = duration;
        }

        private void OnGUI()
        {
            // NEVER draw any UI if currently capturing a screenshot
            if (_isCapturing) return;

            // Notification Banner
            if (_statusBannerTimer > 0f && !string.IsNullOrEmpty(_statusBannerText))
            {
                DrawNotificationBanner();
            }

            // Management GUI Window
            if (_showGui)
            {
                _windowRect = GUI.Window(892100, _windowRect, DrawWindow, "World Compare Screenshots");
            }
        }

        private void DrawNotificationBanner()
        {
            float width = Mathf.Min(600f, Screen.width - 40f);
            float height = 36f;
            float x = (Screen.width - width) / 2f;
            float y = 15f;

            GUI.Box(new Rect(x, y, width, height), "");
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
            labelStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x, y, width, height), _statusBannerText, labelStyle);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            string currentWorld = GetEffectiveWorldName();
            GUILayout.Label($"Current World: <b>{currentWorld}</b> (Total Waypoints: {_waypoints.Count})", GetRichLabelStyle());

            GUILayout.Space(6);

            // Add waypoint bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name:", GUILayout.Width(45));
            _newWaypointNameInput = GUILayout.TextField(_newWaypointNameInput, GUILayout.Width(240));
            if (GUILayout.Button("+ Add Current Spot", GUILayout.ExpandWidth(true)))
            {
                AddCurrentLocationAsWaypoint(_newWaypointNameInput);
                _newWaypointNameInput = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // Action buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Capture All ({_waypoints.Count}) in Sequence [F8]"))
            {
                CaptureAllSequence();
            }
            if (GUILayout.Button("Capture Selected [F7]"))
            {
                CaptureCurrentWaypoint();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Saved Waypoints:</b>", GetRichLabelStyle());

            // Scrollable list of waypoints
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(240));

            if (_waypoints.Count == 0)
            {
                GUILayout.Label("<i>No waypoints saved yet. Walk/fly to a view and click '+ Add Current Spot' or press F6.</i>", GetRichLabelStyle());
            }
            else
            {
                for (int i = 0; i < _waypoints.Count; i++)
                {
                    var wp = _waypoints[i];
                    bool isSelected = (i == _selectedWaypointIndex);

                    GUILayout.BeginHorizontal(GUI.skin.box);

                    string prefix = isSelected ? "<b>▶ " : "  ";
                    string suffix = isSelected ? "</b>" : "";
                    string info = $"{prefix}#{i + 1:D2} {wp.name}{suffix}\n<size=10>({wp.posX:F1}, {wp.posY:F1}, {wp.posZ:F1}) | World: {wp.sourceWorld}</size>";

                    GUILayout.Label(info, GetRichLabelStyle(), GUILayout.Width(220));

                    if (GUILayout.Button("Go", GUILayout.Width(38)))
                    {
                        _selectedWaypointIndex = i;
                        TeleportToSelectedWaypoint();
                    }

                    if (GUILayout.Button("Snap", GUILayout.Width(45)))
                    {
                        _selectedWaypointIndex = i;
                        StartCoroutine(CaptureSingleRoutine(wp, i));
                    }

                    if (GUILayout.Button("X", GUILayout.Width(28)))
                    {
                        _waypoints.RemoveAt(i);
                        SaveWaypointsToDisk();
                        if (_selectedWaypointIndex >= _waypoints.Count)
                        {
                            _selectedWaypointIndex = Mathf.Max(0, _waypoints.Count - 1);
                        }
                        SetStatusBanner($"Removed waypoint #{i + 1}");
                        break;
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);

            // Bottom bar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Waypoints", GUILayout.Width(140)))
            {
                _waypoints.Clear();
                SaveWaypointsToDisk();
                _selectedWaypointIndex = 0;
                SetStatusBanner("All waypoints cleared.");
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close Menu [F10]", GUILayout.Width(130)))
            {
                _showGui = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("<size=10>Hotkeys: F6=Add | F7=Capture Selected | F8=Capture All | PgUp/PgDn=Cycle | F10=Menu</size>", GetRichLabelStyle());

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 25));
        }

        private static GUIStyle _richLabelStyle;
        private static GUIStyle GetRichLabelStyle()
        {
            if (_richLabelStyle == null)
            {
                _richLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };
            }
            return _richLabelStyle;
        }

        private void OnDestroy()
        {
            if (_activeCaptureCoroutine != null)
            {
                StopCoroutine(_activeCaptureCoroutine);
                _activeCaptureCoroutine = null;
            }
        }
    }
}
