using System;
using UnityEngine;

namespace WorldCompareScreenshots
{
    [Serializable]
    public class ScreenshotWaypoint
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "";
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW = 1f;
        public float pitch;
        public float yaw;
        public float roll;
        public float fov = 65f;
        public bool isFreeFly;
        public string createdDate = "";
        public string sourceWorld = "";

        public Vector3 Position
        {
            get => new Vector3(posX, posY, posZ);
            set
            {
                posX = value.x;
                posY = value.y;
                posZ = value.z;
            }
        }

        public Quaternion Rotation
        {
            get => new Quaternion(rotX, rotY, rotZ, rotW);
            set
            {
                rotX = value.x;
                rotY = value.y;
                rotZ = value.z;
                rotW = value.w;
            }
        }

        public static void GetEulerAngles(float x, float y, float z, float w, out float pitch, out float yaw, out float roll)
        {
            float norm = (float)Math.Sqrt(x * x + y * y + z * z + w * w);
            if (norm > 0.00001f)
            {
                x /= norm;
                y /= norm;
                z /= norm;
                w /= norm;
            }

            float sinPitch = 2f * (w * x - y * z);
            if (Math.Abs(sinPitch) >= 0.9999f)
            {
                pitch = sinPitch > 0 ? 90f : -90f;
                yaw = (float)(Math.Atan2(2f * (w * y + z * x), 1f - 2f * (x * x + y * y)) * 57.29577951308232);
                roll = 0f;
            }
            else
            {
                pitch = (float)(Math.Asin(sinPitch) * 57.29577951308232);
                yaw = (float)(Math.Atan2(2f * (w * y + z * x), 1f - 2f * (x * x + y * y)) * 57.29577951308232);
                roll = (float)(Math.Atan2(2f * (w * z + x * y), 1f - 2f * (x * x + z * z)) * 57.29577951308232);
            }
        }

        public static ScreenshotWaypoint Create(
            float posX,
            float posY,
            float posZ,
            float rotX,
            float rotY,
            float rotZ,
            float rotW,
            float fov,
            string name,
            string sourceWorld,
            bool isFreeFly)
        {
            GetEulerAngles(rotX, rotY, rotZ, rotW, out float pitch, out float yaw, out float roll);

            return new ScreenshotWaypoint
            {
                id = Guid.NewGuid().ToString("N"),
                name = string.IsNullOrEmpty(name) ? "Waypoint" : name.Trim(),
                posX = posX,
                posY = posY,
                posZ = posZ,
                rotX = rotX,
                rotY = rotY,
                rotZ = rotZ,
                rotW = rotW,
                pitch = pitch,
                yaw = yaw,
                roll = roll,
                fov = fov > 0f ? fov : 65f,
                isFreeFly = isFreeFly,
                createdDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                sourceWorld = sourceWorld ?? ""
            };
        }

        public static ScreenshotWaypoint Create(
            Vector3 position,
            Quaternion rotation,
            float fov,
            string name,
            string sourceWorld,
            bool isFreeFly)
        {
            return Create(
                position.x, position.y, position.z,
                rotation.x, rotation.y, rotation.z, rotation.w,
                fov, name, sourceWorld, isFreeFly);
        }
    }
}
