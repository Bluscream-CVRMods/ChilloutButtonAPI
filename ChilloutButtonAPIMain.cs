using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ChilloutButtonAPI.UI;
using Libraries;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using AccessTools = HarmonyLib.AccessTools;
using HarmonyMethod = HarmonyLib.HarmonyMethod;
using Object = UnityEngine.Object;

// For an example on how to use the events, check out https://github.com/Bluscream/CVR-EventLogger/blob/main/Main.cs
// For an example on how to create a menu / button check out https://github.com/Bluscream/CVRMods/blob/patch-1/RestartButton/Main.cs#L41-L47

[assembly: MelonInfo(typeof(ChilloutButtonAPI.ChilloutButtonAPIMain), "ChilloutButtonAPI", "1.8", "Plague")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
namespace ChilloutButtonAPI {
    public class ChilloutButtonAPIMain : MelonMod {
        public static event Action OnInit;

        private static GameObject OurUIParent;
        public static SubMenu MainPage;
        public static bool HasInit = false;

        public static readonly Dictionary<string, Vector3> MenuPositions = new() { { "Left", new Vector3(-.7f, 0.062f, 0f) }, { "Right", new Vector3(0.65f, 0.062f, 0f) } };
        public static readonly Dictionary<string, Quaternion> MenuRotations = new() { { "Left", new Quaternion(0f, 0f, 0f, 0f) }, { "Right", new Quaternion(0f, 0f, 0f, 0f) } };
        public static MelonPreferences_Entry MenuLocationSetting, MenuPositionSetting, MenuRotationSetting, MenuScaleSetting;
        public static MenuPosition oldPos;
        public enum MenuPosition { Left, Right }

        public override void OnApplicationStart() {
            MelonPreferences_Category cat = MelonPreferences.CreateCategory("Button API");
            MenuLocationSetting = cat.CreateEntry<MenuPosition>("menulocation", MenuPosition.Right, "Menu Location", "Presets for Position and Rotation");
            oldPos = (MenuPosition)MenuLocationSetting.BoxedValue;
            // MenuLocationSetting.OnValueChangedUntyped += Menupos_OnValueChangedUntyped;
            MenuPositionSetting = cat.CreateEntry<Vector3>("menuposition", MenuPositions["Right"], "Menu Position");
            MenuRotationSetting = cat.CreateEntry<Quaternion>("menurotation", MenuRotations["Right"], "Menu Rotation");
            MenuScaleSetting = cat.CreateEntry<Vector3>("menuscale", new Vector3(0.0007f, 0.001f, 0.001f), "Menu Scale");
            _ = HarmonyInstance.Patch(AccessTools.Constructor(typeof(PlayerDescriptor)), null, new HarmonyMethod(typeof(ChilloutButtonAPIMain).GetMethod(nameof(OnPlayerJoined), BindingFlags.NonPublic | BindingFlags.Static)));
            _ = HarmonyInstance.Patch(typeof(PuppetMaster).GetMethod(nameof(PuppetMaster.AvatarInstantiated)), new HarmonyMethod(typeof(ChilloutButtonAPIMain).GetMethod(nameof(OnAvatarInstantiated_Pre), BindingFlags.NonPublic | BindingFlags.Static)), new HarmonyMethod(typeof(ChilloutButtonAPIMain).GetMethod(nameof(OnAvatarInstantiated_Post), BindingFlags.NonPublic | BindingFlags.Static)));
            _ = HarmonyInstance.Patch(typeof(CVR_MenuManager).GetMethod(nameof(CVR_MenuManager.ToggleQuickMenu), AccessTools.all), null, new HarmonyMethod(typeof(ChilloutButtonAPIMain).GetMethod(nameof(OnQMStateChange), BindingFlags.NonPublic | BindingFlags.Static))); // Patch Method Setting Bool For QM Status; Use For Our UI To Sync
        }

        public override void OnPreferencesSaved() {
            if (HasInit) {
                try {
                    MenuPosition pos = (MenuPosition)MenuLocationSetting.BoxedValue;
                    if (pos == oldPos) {
                        return;
                    }

                    MelonLogger.Msg("MenuLocationSetting changed from {0} to {1}", oldPos, pos);
                    oldPos = pos;
                    MenuPositionSetting.BoxedValue = MenuPositions[pos.ToString()];
                    MenuRotationSetting.BoxedValue = MenuRotations[pos.ToString()];
                    //MenuPositionSetting.Save();
                    //MenuRotationSetting.Save();
                    MelonPreferences.Save();
                    ApplyUISettings();
                } catch (Exception ex) {
                    MelonLogger.Error($"Failed to set MenuLocationSetting: {ex.Message}");
                }
            }
        }

        private static void ApplyUISettings(bool force = false) {
            if (force || HasInit) {
                OurUIParent.transform.localPosition = (Vector3)MenuPositionSetting.BoxedValue;
                OurUIParent.transform.localRotation = (Quaternion)MenuRotationSetting.BoxedValue;
                OurUIParent.transform.localScale = (Vector3)MenuScaleSetting.BoxedValue;
            }
        }

        //public static void SetLayerRecursively(Transform obj, int newLayer, int match) {
        //    if (!obj.gameObject.name.Equals("SelectRegion")) {
        //        if (obj.gameObject.layer == match || match == -1) {
        //            obj.gameObject.layer = newLayer;
        //        }
        //        foreach (Object @object in obj) {
        //            SetLayerRecursively(@object as Transform, newLayer, match);
        //        }
        //    }
        //}

        private static void OnQMStateChange(bool __0) {
            _ = MelonCoroutines.Start(RunMe());

            IEnumerator RunMe() {
                if (!HasInit) {
                    if (new AssetBundleLib() is var Bundle && Bundle.LoadBundle(Properties.Resources.universal_ui)) // This If Also Checks If It Successfully Loaded As To Prevent Further Exceptions
                    {
                        GameObject obj = Bundle.Load<GameObject>("Universal UI.prefab");

                        Transform QM = GameObject.Find("Cohtml").transform.Find("QuickMenu");

                        OurUIParent = Object.Instantiate(obj);

                        OurUIParent.hideFlags = HideFlags.DontUnloadUnusedAsset;

                        OurUIParent.layer = LayerMask.NameToLayer("UI");
                        var canvas = OurUIParent.GetComponent<Canvas>();
                        MelonLogger.Warning("Sorting Layer: {0} ({1})", canvas.sortingLayerName, canvas.sortingLayerID);
                        MelonLogger.Warning("Sorting Order: {0}", canvas.sortingOrder);
                        MelonLogger.Warning("Sorting Override: {0}", canvas.overrideSorting);
                        canvas.sortingLayerID = 10;
                        canvas.sortingLayerName = "UI";
                        canvas.sortingOrder = 10;
                        canvas.overrideSorting = true;
                        MelonLogger.Warning("Sorting Layer: {0} ({1})", canvas.sortingLayerName, canvas.sortingLayerID);
                        MelonLogger.Warning("Sorting Order: {0}", canvas.sortingOrder);
                        MelonLogger.Warning("Sorting Override: {0}", canvas.overrideSorting);

                        OurUIParent.transform.SetParent(QM);
                        ApplyUISettings(true);

                        OurUIParent.transform.Find("Scroll View/Viewport/Content/Back Button/Text (TMP)").gameObject.SetActive(false);
                        OurUIParent.transform.Find("Scroll View/Viewport/Content/Back Button/Text (TMP) Title").GetComponent<TextMeshProUGUI>().text = "Mod UI";

                        _ = OurUIParent.transform.Find("Scroll View/Viewport/Content/Slider").gameObject.AddComponent<SliderTextUpdater>();
                        _ = OurUIParent.transform.Find("Tooltip").gameObject.AddComponent<TooltipHandler>();

                        MainPage = new SubMenu {
                            gameObject = OurUIParent
                        };


                    } else {
                        MelonLogger.Error($"Failed Loading Bundle: {Bundle.error}");
                    }

                    HasInit = true;

                    OnInit?.Invoke();
                }

                yield return new WaitForSeconds(0.2f);

                if (SubMenu.AllSubMenus.Any(o => o.LastState)) {
                    foreach (SubMenu menu in SubMenu.AllSubMenus) {
                        if (menu.gameObject.activeSelf != __0) {
                            menu.SetActive(__0 && menu.LastState, true);
                        }
                    }
                } else {
                    if (MainPage.gameObject.activeSelf != __0) {
                        MainPage.SetActive(__0);
                    }
                }
            }
        }

        private static void OnPlayerJoined(PlayerDescriptor __instance) {
            _ = MelonCoroutines.Start(RunMe());

            IEnumerator RunMe() {
                yield return new WaitForSeconds(1f);
                __instance.gameObject.AddComponent<ObjectHandler>().OnDestroy_E += () => {
                    OnPlayerLeft(__instance);
                };

                OnPlayerJoin?.Invoke(__instance);

                yield break;
            }
        }

        private static void OnPlayerLeft(PlayerDescriptor __instance) {
            OnPlayerLeave?.Invoke(__instance);
        }

        private static bool OnAvatarInstantiated_Pre(ref PuppetMaster __instance) {
            return OnAvatarInstantiated_Pre_E?.Invoke(__instance, __instance.avatarObject) ?? true;
        }

        private static void OnAvatarInstantiated_Post(PuppetMaster __instance) {
            OnAvatarInstantiated_Post_E?.Invoke(__instance, __instance.avatarObject);
        }

        public static event Func<PuppetMaster, GameObject, bool> OnAvatarInstantiated_Pre_E;
        public static event Action<PuppetMaster, GameObject> OnAvatarInstantiated_Post_E;

        public static event Action<PlayerDescriptor> OnPlayerJoin;
        public static event Action<PlayerDescriptor> OnPlayerLeave;

        public class ObjectHandler : MonoBehaviour {
            public event Action OnStart_E;
            public event Action OnUpdate_E;

            public event Action OnEnable_E;
            public event Action OnDisable_E;
            public event Action OnDestroy_E;

            private void Start() {
                OnStart_E?.Invoke();
            }

            private void Update() {
                OnUpdate_E?.Invoke();
            }

            private void OnEnable() {
                OnEnable_E?.Invoke();
            }

            private void OnDisable() {
                OnDisable_E?.Invoke();
            }

            private void OnDestroy() {
                OnDestroy_E?.Invoke();
            }
        }

        internal class SliderTextUpdater : MonoBehaviour {
            private Slider SliderComp;
            private TextMeshProUGUI TextComp;

            private void Start() {
                SliderComp = transform.Find("Slider").GetComponent<Slider>();
                TextComp = transform.Find("Slider/Text (TMP)").GetComponent<TextMeshProUGUI>();
            }

            private void Update() {
                string val = SliderComp.value.ToString("0.0");

                if (TextComp.text != val) {
                    TextComp.text = val;
                }
            }
        }

        internal class TooltipHandler : MonoBehaviour {
            private TextMeshProUGUI TextComp;
            private GameObject OffsetParent;

            private void Start() {
                TextComp = transform.GetComponentInChildren<TextMeshProUGUI>(true);
                OffsetParent = transform.Find("Offset Parent").gameObject;
            }

            private void Update() {
                if (XRDevice.isPresent) {
                    // VR
                } else {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit)) // idk how the fuck to use this method
                    {
                        ToolTipStore store = hit.transform.GetComponent<ToolTipStore>();

                        if (store != null) {
                            TextComp.text = store.Tooltip;
                            OffsetParent.SetActive(true);
                        } else {
                            OffsetParent.SetActive(false);
                        }
                    } else {
                        OffsetParent.SetActive(false);
                    }
                }
            }
        }

        internal class ToolTipStore : MonoBehaviour {
            public string Tooltip;
        }
    }

    internal static class Ex {
        internal static float GetBiggestVector(this Vector3 vec) {
            return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
        }

        internal static Vector3 Multiply(this Vector3 one, Vector3 two) {
            return new Vector3(one.x * two.x, one.y * two.y, one.z * two.z);
        }
    }
}