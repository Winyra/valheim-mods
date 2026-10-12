using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorldCompareScreenshots;

namespace WorldCompareScreenshots.Tests
{
    [TestClass]
    public class WaypointTests
    {
        [TestMethod]
        public void Create_InitializesAllFieldsProperly()
        {
            float posX = 123.45f;
            float posY = -67.89f;
            float posZ = 999.0f;
            float rotX = 0.1f;
            float rotY = 0.2f;
            float rotZ = 0.3f;
            float rotW = 0.9f;
            float fov = 70f;
            string name = "Mead Hall Gate";
            string world = "VikingRealm";

            var wp = ScreenshotWaypoint.Create(posX, posY, posZ, rotX, rotY, rotZ, rotW, fov, name, world, false);

            Assert.IsNotNull(wp.id);
            Assert.AreEqual("Mead Hall Gate", wp.name);
            Assert.AreEqual(posX, wp.posX, 0.001f);
            Assert.AreEqual(posY, wp.posY, 0.001f);
            Assert.AreEqual(posZ, wp.posZ, 0.001f);
            Assert.AreEqual(rotX, wp.rotX, 0.001f);
            Assert.AreEqual(rotY, wp.rotY, 0.001f);
            Assert.AreEqual(rotZ, wp.rotZ, 0.001f);
            Assert.AreEqual(rotW, wp.rotW, 0.001f);
            Assert.AreEqual(70f, wp.fov, 0.01f);
            Assert.IsFalse(wp.isFreeFly);
            Assert.AreEqual("VikingRealm", wp.sourceWorld);
            Assert.IsFalse(string.IsNullOrEmpty(wp.createdDate));
        }

        [TestMethod]
        public void GetEulerAngles_ComputesAnglesCorrectly()
        {
            // Identity rotation (0, 0, 0, 1) -> 0 pitch, 0 yaw, 0 roll
            ScreenshotWaypoint.GetEulerAngles(0f, 0f, 0f, 1f, out float pitch, out float yaw, out float roll);
            Assert.AreEqual(0f, pitch, 0.01f);
            Assert.AreEqual(0f, yaw, 0.01f);
            Assert.AreEqual(0f, roll, 0.01f);

            // 90 degrees around Y (yaw): sin(45 deg) = 0.7071068f, cos(45 deg) = 0.7071068f
            ScreenshotWaypoint.GetEulerAngles(0f, 0.7071068f, 0f, 0.7071068f, out pitch, out yaw, out roll);
            Assert.AreEqual(0f, pitch, 0.01f);
            Assert.AreEqual(90f, yaw, 0.05f);
            Assert.AreEqual(0f, roll, 0.01f);
        }

        [TestMethod]
        public void Json_RoundTripSerialization_PreservesValues()
        {
            var originalList = new List<ScreenshotWaypoint>
            {
                new ScreenshotWaypoint
                {
                    id = "wp-1",
                    name = "Base \"Overlook\" & Docks",
                    posX = 100.5f,
                    posY = 20.25f,
                    posZ = -300.75f,
                    rotX = 0.1f,
                    rotY = 0.2f,
                    rotZ = 0.3f,
                    rotW = 0.9f,
                    pitch = 12.5f,
                    yaw = 45.0f,
                    roll = 0.0f,
                    fov = 65.0f,
                    isFreeFly = true,
                    createdDate = "2026-10-12 18:00:00",
                    sourceWorld = "Valheim_Save_01"
                },
                new ScreenshotWaypoint
                {
                    id = "wp-2",
                    name = "Portal Hub",
                    posX = -50.0f,
                    posY = 10.0f,
                    posZ = 80.0f,
                    rotX = 0.0f,
                    rotY = 0.707f,
                    rotZ = 0.0f,
                    rotW = 0.707f,
                    pitch = 0.0f,
                    yaw = 90.0f,
                    roll = 0.0f,
                    fov = 60.0f,
                    isFreeFly = false,
                    createdDate = "2026-10-12 18:05:00",
                    sourceWorld = "Fresh_Seed_01"
                }
            };

            string json = WaypointJson.Serialize(originalList);
            Assert.IsFalse(string.IsNullOrWhiteSpace(json));

            var deserialized = WaypointJson.Deserialize(json);
            Assert.IsNotNull(deserialized);
            Assert.HasCount(2, deserialized);

            var first = deserialized[0];
            Assert.AreEqual("wp-1", first.id);
            Assert.AreEqual("Base \"Overlook\" & Docks", first.name);
            Assert.AreEqual(100.5f, first.posX, 0.001f);
            Assert.AreEqual(20.25f, first.posY, 0.001f);
            Assert.AreEqual(-300.75f, first.posZ, 0.001f);
            Assert.AreEqual(0.1f, first.rotX, 0.001f);
            Assert.AreEqual(0.2f, first.rotY, 0.001f);
            Assert.AreEqual(0.3f, first.rotZ, 0.001f);
            Assert.AreEqual(0.9f, first.rotW, 0.001f);
            Assert.AreEqual(12.5f, first.pitch, 0.01f);
            Assert.AreEqual(45.0f, first.yaw, 0.01f);
            Assert.AreEqual(65.0f, first.fov, 0.01f);
            Assert.IsTrue(first.isFreeFly);
            Assert.AreEqual("Valheim_Save_01", first.sourceWorld);

            var second = deserialized[1];
            Assert.AreEqual("wp-2", second.id);
            Assert.AreEqual("Portal Hub", second.name);
            Assert.AreEqual(-50.0f, second.posX, 0.001f);
            Assert.IsFalse(second.isFreeFly);
        }

        [TestMethod]
        public void Json_Deserialize_HandlesEmptyAndMalformedInput()
        {
            var emptyList = WaypointJson.Deserialize("");
            Assert.IsEmpty(emptyList);

            var nullList = WaypointJson.Deserialize(null);
            Assert.IsEmpty(nullList);

            var invalidJson = WaypointJson.Deserialize("{ malformed : test }");
            Assert.IsEmpty(invalidJson);

            var emptyWaypoints = WaypointJson.Deserialize("{ \"waypoints\": [] }");
            Assert.IsEmpty(emptyWaypoints);
        }

        [TestMethod]
        public void SanitizeFileName_StripsInvalidCharactersAndFormats()
        {
            Assert.AreEqual("Unnamed", WaypointStore.SanitizeFileName(null));
            Assert.AreEqual("Unnamed", WaypointStore.SanitizeFileName("   "));
            Assert.AreEqual("Base_Overlook", WaypointStore.SanitizeFileName("Base Overlook"));
            Assert.AreEqual("Base_North_Gate", WaypointStore.SanitizeFileName("Base: North/Gate?"));
            Assert.AreEqual("Clean_Name", WaypointStore.SanitizeFileName("Clean___Name"));
            Assert.AreEqual("Invalid_Chars_Test", WaypointStore.SanitizeFileName("Invalid/*:<>|?\"Chars\nTest"));
        }

        [TestMethod]
        public void GenerateScreenshotFileName_ProducesExpectedNamingPattern()
        {
            var wp = new ScreenshotWaypoint
            {
                name = "North Tower View"
            };
            var time = new DateTime(2026, 10, 12, 14, 30, 45);

            string fileName = WaypointStore.GenerateScreenshotFileName(wp, 0, "MyEndgameWorld", time);
            Assert.AreEqual("01_North_Tower_View__MyEndgameWorld__20261012_143045.png", fileName);

            string secondFile = WaypointStore.GenerateScreenshotFileName(wp, 8, "Fresh_Seed", time);
            Assert.AreEqual("09_North_Tower_View__Fresh_Seed__20261012_143045.png", secondFile);

            // Ad-hoc capture (null waypoint)
            string adhoc = WaypointStore.GenerateScreenshotFileName(null, -1, "TestWorld", time);
            Assert.AreEqual("adhoc__TestWorld__20261012_143045.png", adhoc);
        }

        [TestMethod]
        public void GetNextAndPreviousIndex_CyclesCorrectly()
        {
            // Empty
            Assert.AreEqual(0, WaypointStore.GetNextIndex(0, 0));
            Assert.AreEqual(0, WaypointStore.GetPreviousIndex(0, 0));

            // Single item
            Assert.AreEqual(0, WaypointStore.GetNextIndex(0, 1));
            Assert.AreEqual(0, WaypointStore.GetPreviousIndex(0, 1));

            // Three items
            Assert.AreEqual(1, WaypointStore.GetNextIndex(0, 3));
            Assert.AreEqual(2, WaypointStore.GetNextIndex(1, 3));
            Assert.AreEqual(0, WaypointStore.GetNextIndex(2, 3)); // wrap around

            Assert.AreEqual(2, WaypointStore.GetPreviousIndex(0, 3)); // wrap around
            Assert.AreEqual(0, WaypointStore.GetPreviousIndex(1, 3));
            Assert.AreEqual(1, WaypointStore.GetPreviousIndex(2, 3));
        }

        [TestMethod]
        public void FileStorage_SavesAndLoadsWaypointsCorrectly()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "valheim_test_" + Guid.NewGuid().ToString("N"));
            string filePath = Path.Combine(tempDir, "waypoints.json");

            try
            {
                var waypoints = new List<ScreenshotWaypoint>
                {
                    ScreenshotWaypoint.Create(10f, 20f, 30f, 0f, 0f, 0f, 1f, 65f, "Spot 1", "World A", false),
                    ScreenshotWaypoint.Create(40f, 50f, 60f, 0f, 0f, 0f, 1f, 70f, "Spot 2", "World A", true)
                };

                bool saved = WaypointStore.SaveToFile(filePath, waypoints);
                Assert.IsTrue(saved);
                Assert.IsTrue(File.Exists(filePath));

                var loaded = WaypointStore.LoadFromFile(filePath);
                Assert.HasCount(2, loaded);
                Assert.AreEqual("Spot 1", loaded[0].name);
                Assert.AreEqual(10f, loaded[0].posX, 0.001f);
                Assert.AreEqual("Spot 2", loaded[1].name);
                Assert.IsTrue(loaded[1].isFreeFly);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [TestMethod]
        public void ValheimAdapter_SafelyDegradesWhenGameNotLoaded()
        {
            ValheimAdapter.Initialize();
            string world = ValheimAdapter.GetCurrentWorldName();
            Assert.AreEqual("UnknownWorld", world);

            bool isFreeFly = ValheimAdapter.IsFreeFlyActive();
            Assert.IsFalse(isFreeFly);

            bool isTeleporting = ValheimAdapter.IsPlayerTeleporting();
            Assert.IsFalse(isTeleporting);

            // Setting HUD or showing messages should not throw
            ValheimAdapter.SetHudVisible(false);
            ValheimAdapter.SetHudVisible(true);
            ValheimAdapter.ShowGameMessage("Test message");

            // Camera distance and loading screen checks
            float dist = ValheimAdapter.GetCurrentCameraDistance();
            Assert.AreEqual(4f, dist, 0.01f);
            ValheimAdapter.SetCameraDistance(2.5f);

            bool loading = ValheimAdapter.IsLoadingScreenVisible();
            Assert.IsFalse(loading);
            ValheimAdapter.SetLoadingScreenVisible(false);
            ValheimAdapter.SetLoadingScreenVisible(true);

            ValheimAdapter.ResetTeleportCooldown();
        }

        [TestMethod]
        public void Create_WithCameraDistance_InitializesCameraDistanceProperly()
        {
            var wpFirstPerson = ScreenshotWaypoint.Create(10f, 20f, 30f, 0f, 0f, 0f, 1f, 65f, 0f, "First Person Spot", "World", false);
            Assert.AreEqual(0f, wpFirstPerson.cameraDistance, 0.001f);

            var wpThirdPerson = ScreenshotWaypoint.Create(10f, 20f, 30f, 0f, 0f, 0f, 1f, 65f, 5.5f, "Third Person Spot", "World", false);
            Assert.AreEqual(5.5f, wpThirdPerson.cameraDistance, 0.001f);

            var wpDefault = ScreenshotWaypoint.Create(10f, 20f, 30f, 0f, 0f, 0f, 1f, 65f, "Default Distance Spot", "World", false);
            Assert.AreEqual(4f, wpDefault.cameraDistance, 0.001f);
        }

        [TestMethod]
        public void Json_RoundTrip_IncludesCameraDistance()
        {
            var list = new List<ScreenshotWaypoint>
            {
                ScreenshotWaypoint.Create(1f, 2f, 3f, 0f, 0f, 0f, 1f, 75f, 0.5f, "Zoomed In", "WorldX", false)
            };

            string json = WaypointJson.Serialize(list);
            StringAssert.Contains(json, "\"cameraDistance\": 0.5");

            var deserialized = WaypointJson.Deserialize(json);
            Assert.HasCount(1, deserialized);
            Assert.AreEqual(0.5f, deserialized[0].cameraDistance, 0.001f);
        }

        [TestMethod]
        public void Json_Deserialize_LegacyJsonWithoutCameraDistance_DefaultsTo4()
        {
            string legacyJson = @"
            {
              ""waypoints"": [
                {
                  ""id"": ""legacy-1"",
                  ""name"": ""Old Waypoint"",
                  ""posX"": 10.0,
                  ""posY"": 20.0,
                  ""posZ"": 30.0,
                  ""rotX"": 0.0,
                  ""rotY"": 0.0,
                  ""rotZ"": 0.0,
                  ""rotW"": 1.0,
                  ""fov"": 65.0,
                  ""isFreeFly"": false
                }
              ]
            }";

            var list = WaypointJson.Deserialize(legacyJson);
            Assert.HasCount(1, list);
            Assert.AreEqual("Old Waypoint", list[0].name);
            Assert.AreEqual(4f, list[0].cameraDistance, 0.001f);
        }

        [TestMethod]
        public void ValheimAdapter_FirstPersonAndSingletonHelpers_SafelyDegradeWhenGameNotLoaded()
        {
            ValheimAdapter.Initialize();

            // Singletons return null when game assembly is not loaded
            Assert.IsNull(ValheimAdapter.GetGameCameraInstance());
            Assert.IsNull(ValheimAdapter.GetHudInstance());
            Assert.IsNull(ValheimAdapter.GetMessageHudInstance());

            // First person detection and setting degrade safely without throwing
            bool isFirstPerson = ValheimAdapter.IsFirstPersonActive();
            Assert.IsFalse(isFirstPerson);

            ValheimAdapter.SetFirstPersonMode(true);
            ValheimAdapter.SetFirstPersonMode(false);
            ValheimAdapter.SetCameraDistance(0f);
            ValheimAdapter.SetCameraDistance(4f);
        }

        [TestMethod]
        public void Waypoint_FirstPersonDistance_RecognizedCorrectly()
        {
            var fpWaypoint = ScreenshotWaypoint.Create(0, 0, 0, 0, 0, 0, 1, 65f, 0f, "FP", "World", false);
            Assert.IsLessThanOrEqualTo(0.5f, fpWaypoint.cameraDistance);

            var tpWaypoint = ScreenshotWaypoint.Create(0, 0, 0, 0, 0, 0, 1, 65f, 4.5f, "TP", "World", false);
            Assert.IsGreaterThan(0.5f, tpWaypoint.cameraDistance);
        }
    }
}
