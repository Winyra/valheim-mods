using System;
using System.Reflection;
using UnityEngine;

namespace WorldCompareScreenshots
{
    /// <summary>
    /// Safe reflection-based adapter for Valheim game runtime APIs.
    /// Operates without compile-time dependency on proprietary assembly_valheim.dll.
    /// Gracefully degrades to standard Unity APIs if run outside Valheim or in test environments.
    /// </summary>
    public static class ValheimAdapter
    {
        private static bool _initialized;
        private static Assembly _valheimAssembly;

        // Player reflection
        private static Type _playerType;
        private static FieldInfo _playerLocalPlayerField;
        private static MethodInfo _playerTeleportToMethod;
        private static MethodInfo _playerIsTeleportingMethod;
        private static FieldInfo _playerFlyingField;
        private static FieldInfo _playerGhostModeField;
        private static FieldInfo _playerGodModeField;
        private static MethodInfo _playerSetLookDirMethod;
        private static FieldInfo _playerLookYawField;
        private static FieldInfo _playerLookPitchField;

        // GameCamera reflection
        private static Type _gameCameraType;
        private static FieldInfo _gameCameraInstanceField;
        private static FieldInfo _gameCameraFreeFlyField;
        private static FieldInfo _gameCameraYawField;
        private static FieldInfo _gameCameraPitchField;
        private static FieldInfo _gameCameraCameraField;

        // Hud reflection
        private static Type _hudType;
        private static FieldInfo _hudInstanceField;
        private static FieldInfo _hudRootObjectField;
        private static FieldInfo _hudInstanceVisibleField;
        private static MethodInfo _hudSetVisibleMethod;

        // ZNet reflection
        private static Type _znetType;
        private static PropertyInfo _znetInstanceProperty;
        private static FieldInfo _znetInstanceField;
        private static MethodInfo _znetGetWorldNameMethod;
        private static FieldInfo _znetWorldField;

        // MessageHud reflection
        private static Type _messageHudType;
        private static FieldInfo _messageHudInstanceField;
        private static MethodInfo _messageHudShowMessageMethod;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = assembly.GetName().Name;
                    if (string.Equals(name, "assembly_valheim", StringComparison.OrdinalIgnoreCase))
                    {
                        _valheimAssembly = assembly;
                        break;
                    }
                }

                if (_valheimAssembly == null)
                {
                    // Attempt to locate Player type from any assembly
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var t = assembly.GetType("Player");
                        if (t != null && t.GetField("m_localPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                        {
                            _valheimAssembly = assembly;
                            break;
                        }
                    }
                }

                if (_valheimAssembly != null)
                {
                    InitPlayer();
                    InitCamera();
                    InitHud();
                    InitZNet();
                    InitMessageHud();
                }
            }
            catch (Exception)
            {
                // Fall back cleanly to Unity APIs
            }
        }

        private static void InitPlayer()
        {
            _playerType = _valheimAssembly.GetType("Player");
            if (_playerType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _playerLocalPlayerField = _playerType.GetField("m_localPlayer", staticFlags);
            _playerTeleportToMethod = _playerType.GetMethod("TeleportTo", instanceFlags, null, new[] { typeof(Vector3), typeof(Quaternion), typeof(bool) }, null);
            _playerIsTeleportingMethod = _playerType.GetMethod("IsTeleporting", instanceFlags);
            _playerFlyingField = _playerType.GetField("m_flying", instanceFlags);
            _playerGhostModeField = _playerType.GetField("m_ghostMode", instanceFlags);
            _playerGodModeField = _playerType.GetField("m_godMode", instanceFlags);
            _playerSetLookDirMethod = _playerType.GetMethod("SetLookDir", instanceFlags, null, new[] { typeof(Vector3) }, null);
            _playerLookYawField = _playerType.GetField("m_lookYaw", instanceFlags);
            _playerLookPitchField = _playerType.GetField("m_lookPitch", instanceFlags);
        }

        private static void InitCamera()
        {
            _gameCameraType = _valheimAssembly.GetType("GameCamera");
            if (_gameCameraType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _gameCameraInstanceField = _gameCameraType.GetField("instance", staticFlags);
            _gameCameraFreeFlyField = _gameCameraType.GetField("m_freeFly", instanceFlags);
            _gameCameraYawField = _gameCameraType.GetField("m_yaw", instanceFlags);
            _gameCameraPitchField = _gameCameraType.GetField("m_pitch", instanceFlags);
            _gameCameraCameraField = _gameCameraType.GetField("m_camera", instanceFlags);
        }

        private static void InitHud()
        {
            _hudType = _valheimAssembly.GetType("Hud");
            if (_hudType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _hudInstanceField = _hudType.GetField("instance", staticFlags);
            _hudRootObjectField = _hudType.GetField("m_rootObject", instanceFlags);
            _hudInstanceVisibleField = _hudType.GetField("m_instanceVisible", staticFlags);
            _hudSetVisibleMethod = _hudType.GetMethod("SetVisible", instanceFlags, null, new[] { typeof(bool) }, null);
        }

        private static void InitZNet()
        {
            _znetType = _valheimAssembly.GetType("ZNet");
            if (_znetType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _znetInstanceProperty = _znetType.GetProperty("instance", staticFlags);
            _znetInstanceField = _znetType.GetField("instance", staticFlags);
            _znetGetWorldNameMethod = _znetType.GetMethod("GetWorldName", instanceFlags);
            _znetWorldField = _znetType.GetField("m_world", instanceFlags);
        }

        private static void InitMessageHud()
        {
            _messageHudType = _valheimAssembly.GetType("MessageHud");
            if (_messageHudType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _messageHudInstanceField = _messageHudType.GetField("instance", staticFlags);
            var methods = _messageHudType.GetMethods(instanceFlags);
            foreach (var m in methods)
            {
                if (m.Name == "ShowMessage" && m.GetParameters().Length >= 2)
                {
                    _messageHudShowMessageMethod = m;
                    break;
                }
            }
        }

        public static bool IsValheimLoaded
        {
            get
            {
                Initialize();
                return _valheimAssembly != null;
            }
        }

        public static object GetLocalPlayer()
        {
            Initialize();
            if (_playerLocalPlayerField != null)
            {
                try
                {
                    return _playerLocalPlayerField.GetValue(null);
                }
                catch { }
            }
            return null;
        }

        public static string GetCurrentWorldName()
        {
            Initialize();
            try
            {
                object znetInstance = null;
                if (_znetInstanceProperty != null)
                    znetInstance = _znetInstanceProperty.GetValue(null, null);
                else if (_znetInstanceField != null)
                    znetInstance = _znetInstanceField.GetValue(null);

                if (znetInstance != null)
                {
                    if (_znetGetWorldNameMethod != null)
                    {
                        var nameObj = _znetGetWorldNameMethod.Invoke(znetInstance, null);
                        if (nameObj is string s && !string.IsNullOrEmpty(s))
                            return s;
                    }

                    if (_znetWorldField != null)
                    {
                        var worldObj = _znetWorldField.GetValue(znetInstance);
                        if (worldObj != null)
                        {
                            var worldNameField = worldObj.GetType().GetField("m_name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (worldNameField != null)
                            {
                                var wName = worldNameField.GetValue(worldObj) as string;
                                if (!string.IsNullOrEmpty(wName))
                                    return wName;
                            }
                        }
                    }
                }
            }
            catch { }

            return "UnknownWorld";
        }

        public static bool IsFreeFlyActive()
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null && _gameCameraFreeFlyField != null)
                {
                    var camInstance = _gameCameraInstanceField.GetValue(null);
                    if (camInstance != null)
                    {
                        var val = _gameCameraFreeFlyField.GetValue(camInstance);
                        if (val is bool b) return b;
                    }
                }
            }
            catch { }
            return false;
        }

        public static Vector3 GetCurrentCameraPosition()
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null)
                {
                    object camInstance = _gameCameraInstanceField.GetValue(null);
                    if (camInstance != null)
                    {
                        var trProp = camInstance.GetType().GetProperty("transform");
                        if (trProp != null)
                        {
                            var tr = trProp.GetValue(camInstance, null);
                            var posProp = tr?.GetType().GetProperty("position");
                            if (posProp != null && posProp.GetValue(tr, null) is Vector3 v)
                                return v;
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (Camera.main != null)
                {
                    return Camera.main.transform.position;
                }
            }
            catch { }

            try
            {
                object player = GetLocalPlayer();
                if (player != null)
                {
                    var trProp = player.GetType().GetProperty("transform");
                    if (trProp != null)
                    {
                        var tr = trProp.GetValue(player, null);
                        var posProp = tr?.GetType().GetProperty("position");
                        if (posProp != null && posProp.GetValue(tr, null) is Vector3 v)
                            return v + Vector3.up * 1.8f;
                    }
                }
            }
            catch { }

            return Vector3.zero;
        }

        public static Quaternion GetCurrentCameraRotation()
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null)
                {
                    object camInstance = _gameCameraInstanceField.GetValue(null);
                    if (camInstance != null)
                    {
                        var trProp = camInstance.GetType().GetProperty("transform");
                        if (trProp != null)
                        {
                            var tr = trProp.GetValue(camInstance, null);
                            var rotProp = tr?.GetType().GetProperty("rotation");
                            if (rotProp != null && rotProp.GetValue(tr, null) is Quaternion q)
                                return q;
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (Camera.main != null)
                {
                    return Camera.main.transform.rotation;
                }
            }
            catch { }

            try
            {
                object player = GetLocalPlayer();
                if (player != null)
                {
                    var trProp = player.GetType().GetProperty("transform");
                    if (trProp != null)
                    {
                        var tr = trProp.GetValue(player, null);
                        var rotProp = tr?.GetType().GetProperty("rotation");
                        if (rotProp != null && rotProp.GetValue(tr, null) is Quaternion q)
                            return q;
                    }
                }
            }
            catch { }

            return Quaternion.identity;
        }

        public static float GetCurrentFov()
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null && _gameCameraCameraField != null)
                {
                    object camInstance = _gameCameraInstanceField.GetValue(null);
                    if (camInstance != null)
                    {
                        object cam = _gameCameraCameraField.GetValue(camInstance);
                        if (cam != null)
                        {
                            var fovProp = cam.GetType().GetProperty("fieldOfView");
                            if (fovProp != null && fovProp.GetValue(cam, null) is float fov)
                                return fov;
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (Camera.main != null)
                {
                    return Camera.main.fieldOfView;
                }
            }
            catch { }

            return 65f;
        }

        public static bool IsPlayerTeleporting()
        {
            Initialize();
            try
            {
                var player = GetLocalPlayer();
                if (player != null && _playerIsTeleportingMethod != null)
                {
                    var val = _playerIsTeleportingMethod.Invoke(player, null);
                    if (val is bool b) return b;
                }
            }
            catch { }
            return false;
        }

        public static bool TeleportToWaypoint(ScreenshotWaypoint waypoint, bool enableFly = true)
        {
            if (waypoint == null) return false;
            Initialize();

            Vector3 targetPos = waypoint.Position;
            Quaternion targetRot = waypoint.Rotation;

            bool handled = false;

            // 1. Position local player if present
            var player = GetLocalPlayer() as Component;
            if (player != null)
            {
                try
                {
                    if (enableFly && _playerFlyingField != null)
                    {
                        _playerFlyingField.SetValue(player, true);
                    }
                    if (enableFly && _playerGhostModeField != null)
                    {
                        _playerGhostModeField.SetValue(player, true);
                    }
                    if (enableFly && _playerGodModeField != null)
                    {
                        _playerGodModeField.SetValue(player, true);
                    }

                    // Reset physics velocity so player does not carry momentum
                    var rb = player.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    // Teleport player
                    if (_playerTeleportToMethod != null)
                    {
                        _playerTeleportToMethod.Invoke(player, new object[] { targetPos, targetRot, true });
                    }
                    else
                    {
                        player.transform.position = targetPos;
                        player.transform.rotation = targetRot;
                    }

                    // Set look angles
                    if (_playerSetLookDirMethod != null)
                    {
                        _playerSetLookDirMethod.Invoke(player, new object[] { targetRot * Vector3.forward });
                    }
                    if (_playerLookYawField != null)
                    {
                        _playerLookYawField.SetValue(player, targetRot.eulerAngles.y);
                    }
                    if (_playerLookPitchField != null)
                    {
                        _playerLookPitchField.SetValue(player, targetRot.eulerAngles.x);
                    }

                    handled = true;
                }
                catch { }
            }

            // 2. Align GameCamera if present
            try
            {
                if (_gameCameraInstanceField != null)
                {
                    var camInstance = _gameCameraInstanceField.GetValue(null) as Component;
                    if (camInstance != null)
                    {
                        camInstance.transform.position = targetPos;
                        camInstance.transform.rotation = targetRot;

                        if (_gameCameraYawField != null)
                            _gameCameraYawField.SetValue(camInstance, targetRot.eulerAngles.y);
                        if (_gameCameraPitchField != null)
                            _gameCameraPitchField.SetValue(camInstance, targetRot.eulerAngles.x);

                        if (_gameCameraCameraField != null)
                        {
                            var cam = _gameCameraCameraField.GetValue(camInstance) as Camera;
                            if (cam != null && waypoint.fov > 0f)
                            {
                                cam.fieldOfView = waypoint.fov;
                            }
                        }
                        handled = true;
                    }
                }
            }
            catch { }

            // 3. Fallback to Camera.main
            if (Camera.main != null)
            {
                Camera.main.transform.position = targetPos;
                Camera.main.transform.rotation = targetRot;
                if (waypoint.fov > 0f)
                {
                    Camera.main.fieldOfView = waypoint.fov;
                }
                handled = true;
            }

            return handled;
        }

        public static void SetHudVisible(bool visible)
        {
            Initialize();
            try
            {
                if (_hudInstanceField != null)
                {
                    var hudInstance = _hudInstanceField.GetValue(null);
                    if (hudInstance != null)
                    {
                        if (_hudSetVisibleMethod != null)
                        {
                            _hudSetVisibleMethod.Invoke(hudInstance, new object[] { visible });
                        }

                        if (_hudRootObjectField != null)
                        {
                            object root = _hudRootObjectField.GetValue(hudInstance);
                            if (root != null)
                            {
                                var setActive = root.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                                setActive?.Invoke(root, new object[] { visible });
                            }
                        }

                        if (_hudInstanceVisibleField != null)
                        {
                            _hudInstanceVisibleField.SetValue(null, visible);
                        }
                    }
                }
            }
            catch { }
        }

        public static void ShowGameMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Initialize();

            try
            {
                if (_messageHudInstanceField != null && _messageHudShowMessageMethod != null)
                {
                    var hud = _messageHudInstanceField.GetValue(null);
                    if (hud != null)
                    {
                        // MessageType enum value: TopLeft = 1, Center = 2
                        var paramTypes = _messageHudShowMessageMethod.GetParameters();
                        if (paramTypes.Length >= 2 && paramTypes[0].ParameterType.IsEnum)
                        {
                            var enumVal = Enum.ToObject(paramTypes[0].ParameterType, 1);
                            _messageHudShowMessageMethod.Invoke(hud, new object[] { enumVal, message, 0, null });
                            return;
                        }
                    }
                }
            }
            catch { }
        }
    }
}
