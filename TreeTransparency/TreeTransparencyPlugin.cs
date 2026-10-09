using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;   

namespace TreeTransparency
{
    [BepInPlugin("winyra.treetransparency", "TreeTransparency", "1.1.1")]
    public class TreeTransparencyPlugin : BaseUnityPlugin
    {
        private bool _active = false;
        private readonly HashSet<MeshRenderer> _tracked = new();
        private readonly Dictionary<MeshRenderer, Material[]> _originals = new();
        private Coroutine _watcherCoroutine;

        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<float> _alpha;

        // Patterns match against GameObject.name (lowercase).
        // Each pattern catches all variants in that family.
        private static readonly string[] TreePatterns =
        {
            "beech",          // Beech1, Beech_small1/2, Beech_Stub, Beech1_Stub
            "birch",          // Birch1, Birch1_aut, Birch2, Birch2_aut, BirchStub
            "oak1",           // Oak1
            "oakstub",        // OakStub
            "pinetree",       // Pinetree_01, Pinetree_01_Stub
            "firtree",        // FirTree, FirTree_small, FirTree_small_dead, FirTree_Stub, FirTree_big
            "swamptree",      // SwampTree1, SwampTree2, SwampTree2_darkland, SwampTree1_Stub
            "ygga",           // YggaShoot1/2/3, YggaShoot_small1
            "shootstump",     // ShootStump
            "ashlandstree"    // AshlandsTree1/2/3, AshlandsTreeStump1/2/3
        };

        private void Awake()
        {
            _toggleKey = Config.Bind("Hotkeys", "ToggleTreeTransparency",
                new KeyboardShortcut(KeyCode.F9),
                "Key to toggle tree transparency");

            _alpha = Config.Bind("General", "Transparency",
                0.3f,
                new ConfigDescription(
                    "Tree opacity (0.05 = nearly invisible, 1.0 = fully opaque)",
                    new AcceptableValueRange<float>(0.05f, 1.0f)));

            Logger.LogInfo("TreeTransparency loaded. Press " + _toggleKey.Value.MainKey + " to toggle.");
        }

        private void Update()
        {
            if (_toggleKey.Value.IsDown())
                Toggle();
        }

        private bool IsTree(GameObject go)
        {
            string name = go.name.ToLower();
            return TreePatterns.Any(p => name.Contains(p));
        }

        private void Toggle()
        {
            _active = !_active;

            if (_active)
            {
                ApplyToAll();
                _watcherCoroutine = StartCoroutine(WatchForNewTrees());
            }
            else
            {
                if (_watcherCoroutine != null)
                {
                    StopCoroutine(_watcherCoroutine);
                    _watcherCoroutine = null;
                }
                RestoreAll();
            }

            string state = _active ? "ON" : "OFF";
            Logger.LogInfo($"Tree transparency: {state} (alpha={_alpha.Value:F2})");   
        }

        private IEnumerator WatchForNewTrees()
        {
            while (_active)
            {
                ApplyToAll();
                yield return new WaitForSeconds(2f);
            }
        }

        private static Shader _transparentShader;

        private void EnsureShader()
        {
            if (_transparentShader == null)
            {
                _transparentShader = Shader.Find("Particles/Alpha Blended");
                if (_transparentShader == null)
                {
                    // Fallbacks in case it was stripped
                    _transparentShader = Shader.Find("Sprites/Default");
                }
                if (_transparentShader == null)
                {
                    _transparentShader = Shader.Find("UI/Default");
                }
                Logger.LogInfo("Using shader: " + (_transparentShader != null ? _transparentShader.name : "NONE"));
            }
        }   

        private void ApplyToAll()
        {
            EnsureShader();
            if (_transparentShader == null)
            {
                Logger.LogError("Failed to create transparent shader. Aborting.");
                return;
            }

            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
            {
                if (!IsTree(go)) continue;

                var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    if (_tracked.Contains(mr)) continue;

                    var mats = mr.materials;
                    _originals[mr] = mats;
                    _tracked.Add(mr);

                    var transparentMats = new Material[mats.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = new Material(mats[i]);
                        m.shader = _transparentShader;

                        Color c = m.color;
                        c.a = _alpha.Value;
                        m.color = c;

                        transparentMats[i] = m;   
                    }
                    mr.materials = transparentMats;
                }
            }
        }   

        private void RestoreAll()
        {
            foreach (var kvp in _originals)
            {
                if (kvp.Key != null)
                    kvp.Key.materials = kvp.Value;
            }
            _originals.Clear();
            _tracked.Clear();
        }

        private void OnDestroy()
        {
            if (_watcherCoroutine != null)
                StopCoroutine(_watcherCoroutine);
            RestoreAll();
        }
    }
}   