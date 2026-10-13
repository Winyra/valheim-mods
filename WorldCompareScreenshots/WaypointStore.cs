using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WorldCompareScreenshots
{
    public static class WaypointStore
    {
        private static readonly Regex InvalidFileChars = new Regex(@"[\\/:*?""<>|\r\n\t]", RegexOptions.Compiled);

        public static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Unnamed";

            string sanitized = InvalidFileChars.Replace(input.Trim(), "_");
            sanitized = Regex.Replace(sanitized, @"\s+", "_");
            sanitized = Regex.Replace(sanitized, @"_+", "_").Trim('_');

            if (string.IsNullOrEmpty(sanitized))
                return "Unnamed";

            // Limit length to avoid path length limits
            if (sanitized.Length > 64)
                sanitized = sanitized.Substring(0, 64);

            return sanitized;
        }

        public static string GenerateScreenshotFileName(ScreenshotWaypoint waypoint, int index, string worldName, DateTime timestamp)
        {
            string safeWorld = SanitizeFileName(string.IsNullOrWhiteSpace(worldName) ? "UnknownWorld" : worldName);
            string timeStr = timestamp.ToString("yyyyMMdd_HHmmss");

            if (waypoint == null)
            {
                return $"adhoc__{safeWorld}__{timeStr}.png";
            }

            string safeWaypointName = SanitizeFileName(string.IsNullOrWhiteSpace(waypoint.name) ? $"Waypoint_{index + 1}" : waypoint.name);
            int displayIndex = index >= 0 ? index + 1 : 1;
            return $"{displayIndex:D2}_{safeWaypointName}__{safeWorld}__{timeStr}.png";
        }

        public static List<ScreenshotWaypoint> LoadFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return new List<ScreenshotWaypoint>();
                }

                string json = File.ReadAllText(filePath);
                return WaypointJson.Deserialize(json);
            }
            catch (Exception)
            {
                return new List<ScreenshotWaypoint>();
            }
        }

        public static bool SaveToFile(string filePath, List<ScreenshotWaypoint> waypoints)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = WaypointJson.Serialize(waypoints ?? new List<ScreenshotWaypoint>());
                string tempFile = filePath + ".tmp";
                File.WriteAllText(tempFile, json);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempFile, filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int GetNextIndex(int currentIndex, int totalCount)
        {
            if (totalCount <= 0) return 0;
            return (currentIndex + 1) % totalCount;
        }

        public static int GetPreviousIndex(int currentIndex, int totalCount)
        {
            if (totalCount <= 0) return 0;
            return (currentIndex - 1 + totalCount) % totalCount;
        }
    }
}
