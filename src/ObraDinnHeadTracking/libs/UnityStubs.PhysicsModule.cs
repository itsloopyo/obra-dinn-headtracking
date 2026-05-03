// Unity stub for CI builds — PhysicsModule slice
namespace UnityEngine {
    public class Rigidbody : Component {
        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
    }
    public class Collider : Component {
        public bool enabled { get; set; }
        public bool isTrigger { get; set; }
    }
    public struct RaycastHit {
        public Vector3 point { get; }
        public Vector3 normal { get; }
        public float distance { get; }
        public Collider collider { get; }
        public Transform transform { get; }
    }
    public static class Physics {
        public static bool Raycast(Ray ray, out RaycastHit hitInfo) { hitInfo = default; return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance) { hitInfo = default; return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask) { hitInfo = default; return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance) { hitInfo = default; return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance) => false;
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask) => false;
    }
}
