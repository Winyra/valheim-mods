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
        private static FieldInfo _playerTeleportCooldownField;
        private static FieldInfo _playerFlyingField;
        private static FieldInfo _playerGhostModeField;
        private static FieldInfo _playerGodModeField;
        private static MethodInfo _playerSetLookDirMethod;
        private static FieldInfo _playerLookYawField;
        private static FieldInfo _playerLookPitchField;
        private static FieldInfo _playerEyeField;

        // GameCamera reflection
        private static Type _gameCameraType;
        private static FieldInfo _gameCameraInstanceField;
        private static FieldInfo _gameCameraFreeFlyField;
        private static FieldInfo _gameCameraFreeFlyYawField;
        private static FieldInfo _gameCameraFreeFlyPitchField;
        private static FieldInfo _gameCameraYawField;
        private static FieldInfo _gameCameraPitchField;
        private static FieldInfo _gameCameraDistanceField;
        private static FieldInfo _gameCameraCameraField;

        // Hud reflection
        private static Type _hudType;
        private static FieldInfo _hudInstanceField;
        private static FieldInfo _hudRootObjectField;
        private static FieldInfo _hudLoadingScreenField;
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

        private static FieldInfo FindField(Type type, string fieldName, BindingFlags flags)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                var f = current.GetField(fieldName, flags);
                if (f != null) return f;
                current = current.BaseType;
            }
            return null;
        }

        private static MethodInfo FindMethod(Type type, string methodName, BindingFlags flags, Type[] paramTypes = null)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                if (paramTypes != null)
                {
                    var m = current.GetMethod(methodName, flags, null, paramTypes, null);
                    if (m != null) return m;
                }
                else
                {
                    var m = current.GetMethod(methodName, flags);
                    if (m != null) return m;
                }
                current = current.BaseType;
            }
            return null;
        }

        private static MethodInfo FindMethodByFirstParam(Type type, string methodName, BindingFlags flags, Type firstParamType)
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                var methods = current.GetMethods(flags);
                foreach (var m in methods)
                {
                    if (m.Name == methodName)
                    {
                        var pars = m.GetParameters();
                        if (pars.Length > 0 && pars[0].ParameterType == firstParamType)
                            return m;
                    }
                }
                current = current.BaseType;
            }
            return null;
        }

        private static void InitPlayer()
        {
            _playerType = _valheimAssembly.GetType("Player");
            if (_playerType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _playerLocalPlayerField = FindField(_playerType, "m_localPlayer", staticFlags);
            _playerTeleportToMethod = FindMethod(_playerType, "TeleportTo", instanceFlags, new[] { typeof(Vector3), typeof(Quaternion), typeof(bool) });
            _playerIsTeleportingMethod = FindMethod(_playerType, "IsTeleporting", instanceFlags);
            _playerTeleportCooldownField = FindField(_playerType, "m_teleportCooldown", instanceFlags);
            _playerFlyingField = FindField(_playerType, "m_flying", instanceFlags);
            _playerGhostModeField = FindField(_playerType, "m_ghostMode", instanceFlags);
            _playerGodModeField = FindField(_playerType, "m_godMode", instanceFlags);
            _playerSetLookDirMethod = FindMethodByFirstParam(_playerType, "SetLookDir", instanceFlags, typeof(Vector3));
            _playerLookYawField = FindField(_playerType, "m_lookYaw", instanceFlags);
            _playerLookPitchField = FindField(_playerType, "m_lookPitch", instanceFlags);
            _playerEyeField = FindField(_playerType, "m_eye", instanceFlags);
        }

        private static void InitCamera()
        {
            _gameCameraType = _valheimAssembly.GetType("GameCamera");
            if (_gameCameraType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _gameCameraInstanceField = FindField(_gameCameraType, "instance", staticFlags);
            _gameCameraFreeFlyField = FindField(_gameCameraType, "m_freeFly", instanceFlags);
            _gameCameraFreeFlyYawField = FindField(_gameCameraType, "m_freeFlyYaw", instanceFlags);
            _gameCameraFreeFlyPitchField = FindField(_gameCameraType, "m_freeFlyPitch", instanceFlags);
            _gameCameraYawField = FindField(_gameCameraType, "m_yaw", instanceFlags);
            _gameCameraPitchField = FindField(_gameCameraType, "m_pitch", instanceFlags);
            _gameCameraDistanceField = FindField(_gameCameraType, "m_distance", instanceFlags);
            _gameCameraCameraField = FindField(_gameCameraType, "m_camera", instanceFlags);
        }

        private static void InitHud()
        {
            _hudType = _valheimAssembly.GetType("Hud");
            if (_hudType == null) return;

            const BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            _hudInstanceField = FindField(_hudType, "instance", staticFlags);
            _hudRootObjectField = FindField(_hudType, "m_rootObject", instanceFlags);
            _hudLoadingScreenField = FindField(_hudType, "m_loadingScreen", instanceFlags);
            _hudInstanceVisibleField = FindField(_hudType, "m_instanceVisible", staticFlags);
            _hudSetVisibleMethod = FindMethod(_hudType, "SetVisible", instanceFlags, new[] { typeof(bool) });
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
                    if (_playerEyeField != null)
                    {
                        var eye = _playerEyeField.GetValue(player) as Component;
                        if (eye != null) return eye.transform.rotation;
                    }

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

        public static bool IsLoadingScreenVisible()
        {
            Initialize();
            try
            {
                if (_hudInstanceField != null && _hudLoadingScreenField != null)
                {
                    object hudInstance = _hudInstanceField.GetValue(null);
                    if (hudInstance != null)
                    {
                        object loadingScreen = _hudLoadingScreenField.GetValue(hudInstance);
                        if (loadingScreen != null)
                        {
                            var goProp = loadingScreen.GetType().GetProperty("gameObject");
                            object go = goProp?.GetValue(loadingScreen, null);
                            if (go != null)
                            {
                                var activeInHierarchyProp = go.GetType().GetProperty("activeInHierarchy");
                                if (activeInHierarchyProp != null && activeInHierarchyProp.GetValue(go, null) is bool active && !active)
                                    return false;
                            }

                            var alphaProp = loadingScreen.GetType().GetProperty("alpha");
                            if (alphaProp != null && alphaProp.GetValue(loadingScreen, null) is float alpha)
                            {
                                return alpha > 0.05f;
                            }

                            if (go != null)
                            {
                                var activeSelfProp = go.GetType().GetProperty("activeSelf");
                                if (activeSelfProp != null && activeSelfProp.GetValue(go, null) is bool selfActive)
                                    return selfActive;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetLoadingScreenVisible(bool visible)
        {
            Initialize();
            try
            {
                if (_hudInstanceField != null && _hudLoadingScreenField != null)
                {
                    object hudInstance = _hudInstanceField.GetValue(null);
                    if (hudInstance != null)
                    {
                        object loadingScreen = _hudLoadingScreenField.GetValue(hudInstance);
                        if (loadingScreen != null)
                        {
                            if (!visible)
                            {
                                var alphaProp = loadingScreen.GetType().GetProperty("alpha");
                                alphaProp?.SetValue(loadingScreen, 0f, null);

                                var goProp = loadingScreen.GetType().GetProperty("gameObject");
                                object go = goProp?.GetValue(loadingScreen, null);
                                if (go != null)
                                {
                                    var setActive = go.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                                    setActive?.Invoke(go, new object[] { false });
                                }
                            }
                            else
                            {
                                var goProp = loadingScreen.GetType().GetProperty("gameObject");
                                object go = goProp?.GetValue(loadingScreen, null);
                                if (go != null)
                                {
                                    var setActive = go.GetType().GetMethod("SetActive", new[] { typeof(bool) });
                                    setActive?.Invoke(go, new object[] { true });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public static float GetCurrentCameraDistance()
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null && _gameCameraDistanceField != null)
                {
                    var cam = _gameCameraInstanceField.GetValue(null);
                    if (cam != null)
                    {
                        var val = _gameCameraDistanceField.GetValue(cam);
                        if (val is float f) return f;
                    }
                }
            }
            catch { }
            return 4f;
        }

        public static void SetCameraDistance(float distance)
        {
            Initialize();
            try
            {
                if (_gameCameraInstanceField != null && _gameCameraDistanceField != null)
                {
                    var cam = _gameCameraInstanceField.GetValue(null);
                    if (cam != null)
                    {
                        float safeDist = distance < 0f ? 0f : distance;
                        _gameCameraDistanceField.SetValue(cam, safeDist);
                    }
                }
            }
            catch { }
        }

        public static void ResetTeleportCooldown()
        {
            Initialize();
            try
            {
                var player = GetLocalPlayer();
                if (player != null && _playerTeleportCooldownField != null)
                {
                    _playerTeleportCooldownField.SetValue(player, 5.0f);
                }
            }
            catch { }
        }

        public static void ApplyCameraTransform(ScreenshotWaypoint waypoint)
        {
            if (waypoint == null) return;
            Initialize();

            Vector3 targetPos = waypoint.Position;
            Quaternion targetRot = waypoint.Rotation;

            var player = GetLocalPlayer() as Component;
            if (player != null)
            {
                player.transform.position = targetPos;
                ApplyPlayerRotation(player, targetRot);
            }

            ApplyGameCameraTransform(targetPos, targetRot, waypoint.fov, waypoint.cameraDistance);
        }

        private static void ApplyPlayerFlightAndGodMode(Component player, bool enableFly)
        {
            if (!enableFly) return;
            try
            {
                _playerFlyingField?.SetValue(player, true);
                _playerGhostModeField?.SetValue(player, true);
                _playerGodModeField?.SetValue(player, true);
            }
            catch { }
        }

        private static void ResetPlayerPhysics(Component player)
        {
            try
            {
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            catch { }
        }

        private static void ApplyPlayerRotation(Component player, Quaternion targetRot)
        {
            try
            {
                float yaw = targetRot.eulerAngles.y;
                float halfRad = (yaw * (float)Math.PI / 180f) * 0.5f;
                Quaternion yawQuat = new Quaternion(0f, (float)Math.Sin(halfRad), 0f, (float)Math.Cos(halfRad));
                Vector3 forward = targetRot * Vector3.forward;

                player.transform.rotation = yawQuat;

                if (_playerLookYawField != null)
                {
                    if (_playerLookYawField.FieldType == typeof(Quaternion))
                        _playerLookYawField.SetValue(player, yawQuat);
                    else
                        _playerLookYawField.SetValue(player, targetRot.eulerAngles.y);
                }

                if (_playerLookPitchField != null)
                {
                    float pitch = targetRot.eulerAngles.x;
                    if (pitch > 180f) pitch -= 360f;
                    if (pitch < -89f) pitch = -89f;
                    else if (pitch > 89f) pitch = 89f;
                    _playerLookPitchField.SetValue(player, pitch);
                }

                if (_playerSetLookDirMethod != null)
                {
                    var pars = _playerSetLookDirMethod.GetParameters();
                    if (pars.Length == 1)
                        _playerSetLookDirMethod.Invoke(player, new object[] { forward });
                    else if (pars.Length >= 2)
                        _playerSetLookDirMethod.Invoke(player, new object[] { forward, 0f });
                }

                if (_playerEyeField != null)
                {
                    var eye = _playerEyeField.GetValue(player) as Component;
                    if (eye != null) eye.transform.rotation = targetRot;
                }
            }
            catch { }
        }

        private static void ApplyGameCameraTransform(Vector3 targetPos, Quaternion targetRot, float fov, float cameraDistance)
        {
            try
            {
                if (_gameCameraInstanceField != null)
                {
                    var camInstance = _gameCameraInstanceField.GetValue(null) as Component;
                    if (camInstance != null)
                    {
                        camInstance.transform.position = targetPos;
                        camInstance.transform.rotation = targetRot;

                        float yaw = targetRot.eulerAngles.y;
                        float pitch = targetRot.eulerAngles.x;
                        if (pitch > 180f) pitch -= 360f;
                        if (pitch < -89f) pitch = -89f;
                        else if (pitch > 89f) pitch = 89f;

                        _gameCameraFreeFlyYawField?.SetValue(camInstance, yaw);
                        _gameCameraFreeFlyPitchField?.SetValue(camInstance, -pitch);
                        _gameCameraYawField?.SetValue(camInstance, yaw);
                        _gameCameraPitchField?.SetValue(camInstance, pitch);

                        if (_gameCameraDistanceField != null && cameraDistance >= 0f)
                            _gameCameraDistanceField.SetValue(camInstance, cameraDistance);

                        if (_gameCameraCameraField != null)
                        {
                            var cam = _gameCameraCameraField.GetValue(camInstance) as Camera;
                            if (cam != null && fov > 0f) cam.fieldOfView = fov;
                        }
                        return;
                    }
                }
            }
            catch { }

            if (Camera.main != null)
            {
                Camera.main.transform.position = targetPos;
                Camera.main.transform.rotation = targetRot;
                if (fov > 0f) Camera.main.fieldOfView = fov;
            }
        }

        public static bool TeleportToWaypoint(ScreenshotWaypoint waypoint, bool enableFly = true)
        {
            if (waypoint == null) return false;
            Initialize();

            Vector3 targetPos = waypoint.Position;
            Quaternion targetRot = waypoint.Rotation;

            ResetTeleportCooldown();

            bool handled = false;
            var player = GetLocalPlayer() as Component;
            if (player != null)
            {
                try
                {
                    ApplyPlayerFlightAndGodMode(player, enableFly);
                    ResetPlayerPhysics(player);

                    if (_playerTeleportToMethod != null)
                    {
                        _playerTeleportToMethod.Invoke(player, new object[] { targetPos, targetRot, false });
                    }
                    else
                    {
                        player.transform.position = targetPos;
                        player.transform.rotation = targetRot;
                    }

                    ApplyPlayerRotation(player, targetRot);
                    handled = true;
                }
                catch { }
            }

            ApplyGameCameraTransform(targetPos, targetRot, waypoint.fov, waypoint.cameraDistance);
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
