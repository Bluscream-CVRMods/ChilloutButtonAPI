using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ChilloutButtonAPI {
    public static class Extensions {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Behaviour {
            T comp;

            try {
                comp = obj.GetComponent<T>();

                if (comp == null) {
                    comp = obj.AddComponent<T>();
                }
            } catch {
                comp = obj.AddComponent<T>();
            }

            return comp;
        }

        public static T GetOrAddComponent<T>(this Transform obj) where T : Behaviour {
            T comp;

            try {
                comp = obj.gameObject.GetComponent<T>();

                if (comp == null) {
                    comp = obj.gameObject.AddComponent<T>();
                }
            } catch {
                comp = obj.gameObject.AddComponent<T>();
            }

            return comp;
        }

        public static T[] GetAllInstancesOfCurrentScene<T>(bool includeInactive = false, Func<T, bool> Filter = null) where T : Behaviour {
            GameObject[] AllRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            T[] GrabbedObjects = AllRootObjects.SelectMany(o => o.GetComponentsInChildren<T>(includeInactive)).ToArray();

            if (Filter != null) {
                GrabbedObjects = GrabbedObjects.Where(Filter).ToArray();
            }

            return GrabbedObjects;
        }

        [Obsolete]
        public static T[] GetAllInstancesOfAllScenes<T>(bool includeInactive = false, Func<T, bool> Filter = null) where T : Behaviour {
            IEnumerable<GameObject> AllRootObjects = SceneManager.GetAllScenes().SelectMany(o => o.GetRootGameObjects());

            T[] GrabbedObjects = AllRootObjects.SelectMany(o => o.GetComponentsInChildren<T>(includeInactive)).ToArray();

            if (Filter != null) {
                GrabbedObjects = GrabbedObjects.Where(Filter).ToArray();
            }

            return GrabbedObjects;
        }

        public static GameObject FindObject(this GameObject parent, string name) {
            Transform[] array = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in array) {
                if (transform.name == name) {
                    return transform.gameObject;
                }
            }

            return null;
        }

        public static string GetPath(this GameObject gameObject) {
            string text = "/" + gameObject.name;
            while (gameObject.transform.parent != null) {
                gameObject = gameObject.transform.parent.gameObject;
                text = "/" + gameObject.name + text;
            }

            return text;
        }

        public static void DestroyChildren(this Transform transform, Func<Transform, bool> exclude, bool DirectChildrenOnly = false) {
            foreach (Transform child in (DirectChildrenOnly ? transform.GetChildren() : transform.GetComponentsInChildren<Transform>(true)).Where(o => o != transform)) {
                if (child != null) {
                    if (exclude == null || !exclude(child)) {
                        Object.Destroy(child.gameObject);
                    }
                }
            }
        }

        public static void DestroyChildren(this Transform transform, bool DirectChildrenOnly = false) {
            transform.DestroyChildren(null, DirectChildrenOnly);
        }

        public static Vector3 SetX(this Vector3 vector, float x) {
            return new Vector3(x, vector.y, vector.z);
        }

        public static Vector3 SetY(this Vector3 vector, float y) {
            return new Vector3(vector.x, y, vector.z);
        }

        public static Vector3 SetZ(this Vector3 vector, float z) {
            return new Vector3(vector.x, vector.y, z);
        }

        public static float RoundAmount(this float i, float nearestFactor) {
            return (float)Math.Round(i / nearestFactor) * nearestFactor;
        }

        public static Vector3 RoundAmount(this Vector3 i, float nearestFactor) {
            return new Vector3(i.x.RoundAmount(nearestFactor), i.y.RoundAmount(nearestFactor), i.z.RoundAmount(nearestFactor));
        }

        public static Vector2 RoundAmount(this Vector2 i, float nearestFactor) {
            return new Vector2(i.x.RoundAmount(nearestFactor), i.y.RoundAmount(nearestFactor));
        }

        public static string ReplaceFirst(this string text, string search, string replace) {
            int num = text.IndexOf(search);
            return num < 0 ? text : text.Substring(0, num) + replace + text.Substring(num + search.Length);
        }

        public static ColorBlock SetColor(this ColorBlock block, Color color) {
            ColorBlock result = default;
            result.colorMultiplier = block.colorMultiplier;
            result.disabledColor = Color.grey;
            result.highlightedColor = color;
            result.normalColor = color / 1.5f;
            result.pressedColor = Color.white;
            result.selectedColor = color / 1.5f;
            return result;
        }

        public static void DelegateSafeInvoke(this Delegate @delegate, params object[] args) {
            Delegate[] invocationList = @delegate.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++) {
                try {
                    _ = invocationList[i].DynamicInvoke(args);
                } catch (Exception ex) {
                    MelonLogger.Error("Error while executing delegate:\n" + ex);
                }
            }
        }

        public static string ToEasyString(this TimeSpan timeSpan) {
            return Mathf.FloorToInt(timeSpan.Ticks / 864000000000L) > 0
                ? $"{timeSpan:%d} days"
                : Mathf.FloorToInt(timeSpan.Ticks / 36000000000L) > 0
                ? $"{timeSpan:%h} hours"
                : Mathf.FloorToInt(timeSpan.Ticks / 600000000) > 0 ? $"{timeSpan:%m} minutes" : $"{timeSpan:%s} seconds";
        }

        public static Quaternion LookAtThisWithoutRef(this Transform transform, Vector3 FromThisPosition) {
            GameObject obj = new("TempObj");
            obj.transform.position = FromThisPosition;

            obj.transform.LookAt(transform);

            Quaternion rot = obj.transform.localRotation;

            Object.Destroy(obj);

            return rot;
        }

        public static Quaternion FlipX(this Quaternion rot) {
            return new Quaternion(-rot.x, rot.y, rot.z, rot.w);
        }

        public static Quaternion FlipY(this Quaternion rot) {
            return new Quaternion(rot.x, -rot.y, rot.z, rot.w);
        }

        public static Quaternion FlipZ(this Quaternion rot) {
            return new Quaternion(rot.x, rot.y, -rot.z, rot.w);
        }

        public static Quaternion Combine(this Quaternion rot1, Quaternion rot2) {
            return new Quaternion(rot1.x + rot2.x, rot1.y + rot2.y, rot1.z + rot2.z, rot1.w + rot2.w);
        }

        public static Transform[] GetChildren(this Transform transform) {
            List<Transform> Children = new();

            for (int i = 0; i < transform.childCount; i++) {
                Children.Add(transform.GetChild(i));
            }

            return Children.ToArray();
        }

        public static Transform[] GetAllChildren(this Transform transform) {
            List<Transform> Children = new();

            void GetChildrenR(Transform trans) {
                for (int i = 0; i < trans.childCount; i++) {
                    Children.Add(trans.GetChild(i));

                    _ = GetChildren(trans.GetChild(i));
                }
            }

            GetChildrenR(transform);

            return Children.ToArray();
        }

        public static string GetPath(this Transform transform) {
            string path = $"{transform.name}";

            Transform CurrentObj = transform;

            while (CurrentObj.parent != null) {
                CurrentObj = CurrentObj.parent;

                path = $"{CurrentObj.name}/" + path;
            }

            return path;
        }
    }
}
