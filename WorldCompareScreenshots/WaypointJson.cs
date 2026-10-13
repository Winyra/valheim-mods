using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldCompareScreenshots
{
    /// <summary>
    /// Lightweight, robust JSON serializer and parser for waypoints.
    /// Operates without external dependencies or native Unity engine bindings.
    /// </summary>
    public static class WaypointJson
    {
        public static string Serialize(List<ScreenshotWaypoint> waypoints)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"waypoints\": [");

            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    var wp = waypoints[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"id\": \"{EscapeString(wp.id)}\",");
                    sb.AppendLine($"      \"name\": \"{EscapeString(wp.name)}\",");
                    sb.AppendLine($"      \"posX\": {wp.posX.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"posY\": {wp.posY.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"posZ\": {wp.posZ.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"rotX\": {wp.rotX.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"rotY\": {wp.rotY.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"rotZ\": {wp.rotZ.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"rotW\": {wp.rotW.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"pitch\": {wp.pitch.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"yaw\": {wp.yaw.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"roll\": {wp.roll.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"fov\": {wp.fov.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"cameraDistance\": {wp.cameraDistance.ToString("G9", CultureInfo.InvariantCulture)},");
                    sb.AppendLine($"      \"isFreeFly\": {(wp.isFreeFly ? "true" : "false")},");
                    sb.AppendLine($"      \"createdDate\": \"{EscapeString(wp.createdDate)}\",");
                    sb.AppendLine($"      \"sourceWorld\": \"{EscapeString(wp.sourceWorld)}\"");
                    sb.Append("    }");
                    if (i < waypoints.Count - 1)
                    {
                        sb.Append(",");
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static List<ScreenshotWaypoint> Deserialize(string json)
        {
            var result = new List<ScreenshotWaypoint>();
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            int index = 0;
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != '{')
                return result;
            index++; // skip '{'

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index] == '}')
                    break;

                string key = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ':')
                    index++; // skip ':'

                SkipWhitespace(json, ref index);
                if (string.Equals(key, "waypoints", StringComparison.OrdinalIgnoreCase))
                {
                    if (index < json.Length && json[index] == '[')
                    {
                        index++; // skip '['
                        while (index < json.Length)
                        {
                            SkipWhitespace(json, ref index);
                            if (index >= json.Length || json[index] == ']')
                            {
                                if (index < json.Length) index++;
                                break;
                            }

                            if (json[index] == '{')
                            {
                                var wp = ParseWaypointObject(json, ref index);
                                if (wp != null)
                                {
                                    result.Add(wp);
                                }
                            }
                            else if (json[index] == ',')
                            {
                                index++;
                            }
                            else
                            {
                                index++;
                            }
                        }
                    }
                }
                else
                {
                    SkipValue(json, ref index);
                }

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                    index++;
            }

            return result;
        }

        private static ScreenshotWaypoint ParseWaypointObject(string json, ref int index)
        {
            if (json[index] != '{') return null;
            index++; // skip '{'

            var wp = new ScreenshotWaypoint();

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length || json[index] == '}')
                {
                    if (index < json.Length) index++;
                    break;
                }

                string key = ReadString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ':')
                    index++;

                SkipWhitespace(json, ref index);
                string val = ReadTokenOrString(json, ref index);

                switch (key.ToLowerInvariant())
                {
                    case "id":
                        wp.id = val;
                        break;
                    case "name":
                        wp.name = val;
                        break;
                    case "posx":
                        wp.posX = ParseFloat(val);
                        break;
                    case "posy":
                        wp.posY = ParseFloat(val);
                        break;
                    case "posz":
                        wp.posZ = ParseFloat(val);
                        break;
                    case "rotx":
                        wp.rotX = ParseFloat(val);
                        break;
                    case "roty":
                        wp.rotY = ParseFloat(val);
                        break;
                    case "rotz":
                        wp.rotZ = ParseFloat(val);
                        break;
                    case "rotw":
                        wp.rotW = ParseFloat(val, 1f);
                        break;
                    case "pitch":
                        wp.pitch = ParseFloat(val);
                        break;
                    case "yaw":
                        wp.yaw = ParseFloat(val);
                        break;
                    case "roll":
                        wp.roll = ParseFloat(val);
                        break;
                    case "fov":
                        wp.fov = ParseFloat(val, 65f);
                        break;
                    case "cameradistance":
                        wp.cameraDistance = ParseFloat(val, 4f);
                        break;
                    case "isfreefly":
                        wp.isFreeFly = bool.TryParse(val, out bool ff) && ff;
                        break;
                    case "createddate":
                        wp.createdDate = val;
                        break;
                    case "sourceworld":
                        wp.sourceWorld = val;
                        break;
                }

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                    index++;
            }

            return wp;
        }

        private static float ParseFloat(string val, float defaultVal = 0f)
        {
            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            return defaultVal;
        }

        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            var sb = new StringBuilder(str.Length + 4);
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private static void SkipWhitespace(string s, ref int index)
        {
            while (index < s.Length && char.IsWhiteSpace(s[index]))
                index++;
        }

        private static string ReadString(string s, ref int index)
        {
            SkipWhitespace(s, ref index);
            if (index >= s.Length || s[index] != '\"') return "";
            index++; // skip opening quote

            var sb = new StringBuilder();
            bool escape = false;
            while (index < s.Length)
            {
                char c = s[index++];
                if (escape)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        default: sb.Append(c); break;
                    }
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '\"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string ReadTokenOrString(string s, ref int index)
        {
            SkipWhitespace(s, ref index);
            if (index >= s.Length) return "";
            if (s[index] == '\"')
                return ReadString(s, ref index);

            int start = index;
            while (index < s.Length && s[index] != ',' && s[index] != '}' && s[index] != ']' && !char.IsWhiteSpace(s[index]))
                index++;

            return s.Substring(start, index - start).Trim();
        }

        private static void SkipValue(string s, ref int index)
        {
            SkipWhitespace(s, ref index);
            if (index >= s.Length) return;

            char c = s[index];
            if (c == '\"')
            {
                ReadString(s, ref index);
            }
            else if (c == '{' || c == '[')
            {
                char open = c;
                char close = c == '{' ? '}' : ']';
                int depth = 0;
                while (index < s.Length)
                {
                    if (s[index] == open) depth++;
                    else if (s[index] == close)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            index++;
                            break;
                        }
                    }
                    index++;
                }
            }
            else
            {
                while (index < s.Length && s[index] != ',' && s[index] != '}' && s[index] != ']')
                    index++;
            }
        }
    }
}
