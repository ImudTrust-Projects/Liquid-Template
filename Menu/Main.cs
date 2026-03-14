using BepInEx;
using g3;
using GorillaExtensions;
using GorillaLocomotion;
using HarmonyLib;
using Pathfinding;
using Photon.Pun;
using PlayFab.DataModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using liquidclient.Classes;
using liquidclient.Notifications;
using liquidclient.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR.InteractionSystem;
using static liquidclient.Menu.Buttons;
using static liquidclient.Settings;
using Object = UnityEngine.Object;

namespace liquidclient.Menu
{
    [HarmonyPatch(typeof(GTPlayer), "LateUpdate")]
    public class Main : MonoBehaviour
    {
        private static bool hasInitGUI = false;

        // Constant
        public static void Prefix()
        {
            // Initialize Menu
            try
            {
                bool toOpen = (!rightHanded && ControllerInputPoller.instance.leftControllerSecondaryButton) || (rightHanded && ControllerInputPoller.instance.rightControllerSecondaryButton);
                bool keyboardOpen = UnityInput.Current.GetKey(keyboardButton);

                if (menu == null)
                {
                    if (toOpen || keyboardOpen)
                    {
                        CreateMenu();
                        //RecenterMenu(rightHanded);
                        RecenterMenu(rightHanded, keyboardOpen);

                        //MenuAudioHandler.PlayMenuOpen();
                        if (reference == null)
                            CreateReference(rightHanded);
                    }
                }
                else
                {
                    if (toOpen || keyboardOpen)
                        //RecenterMenu(rightHanded);
                        RecenterMenu(rightHanded, keyboardOpen);
                    else
                    {
                        //if (TPC != null) TPC.gameObject.SetActive(true);
                        GameObject.Find("Shoulder Camera").transform.Find("CM vcam1").gameObject.SetActive(true);

                        //MenuAudioHandler.PlayMenuClose();
                        Rigidbody comp = menu.AddComponent(typeof(Rigidbody)) as Rigidbody;
                        comp.linearVelocity = (rightHanded ? GTPlayer.Instance.LeftHand.velocityTracker : GTPlayer.Instance.RightHand.velocityTracker).GetAverageVelocity(true, 0);

                        UnityEngine.Object.Destroy(menu, 2f);
                        menu = null;

                        UnityEngine.Object.Destroy(reference);
                        reference = null;


                    }
                }
            }
            catch (Exception exc)
            {
                Debug.LogError(string.Format("{0} // Error initializing at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }

            // Constant
            try
            {
                if (fpsObject != null)
                    fpsObject.text = "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();

                foreach (ButtonInfo button in buttons
                    .SelectMany(list => list)
                    .Where(button => button.enabled && button.method != null))
                {
                    try
                    {
                        button.method.Invoke();
                    }
                    catch (Exception exc)
                    {
                        Debug.LogError(string.Format("{0} // Error with mod {1} at {2}: {3}", PluginInfo.Name, button.buttonText, exc.StackTrace, exc.Message));
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.LogError(string.Format("{0} // Error with executing mods at {1}: {2}", PluginInfo.Name, exc.StackTrace, exc.Message));
            }
        }


        private static readonly Dictionary<string, GameObject> objectPool = new Dictionary<string, GameObject>();

        public static GameObject GetObject(string find)
        {
            if (objectPool.TryGetValue(find, out GameObject go))
                return go;

            GameObject tgo = GameObject.Find(find);
            if (tgo != null)
                objectPool.Add(find, tgo);

            return tgo;
        }

        // Functions
        public static void CreateMenu()
        {
            menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menu.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menu.GetComponent<BoxCollider>());
            UnityEngine.Object.Destroy(menu.GetComponent<Renderer>());
            menu.transform.localScale = menuSize; // THIS IS THE FIX - use the constant normal size!

            menuBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(menuBackground.GetComponent<Rigidbody>());
            UnityEngine.Object.Destroy(menuBackground.GetComponent<BoxCollider>());
            menuBackground.transform.parent = menu.transform;
            menuBackground.transform.rotation = Quaternion.identity;
            menuBackground.transform.localScale = Settings.menubackgroundSize; // This changes with mode
            menuBackground.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            menuBackground.transform.position = new Vector3(0.05f, 0f, 0f);
            RoundObj(menuBackground, 0.03f);

            canvasObject = new GameObject();
            canvasObject.transform.parent = menu.transform;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasScaler.dynamicPixelsPerUnit = 1000f;

            Text text = new GameObject
            {
                transform =
        {
            parent = canvasObject.transform
        }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = PluginInfo.Name;
            text.fontSize = 1;
            text.color = textColors[0];
            text.supportRichText = true;
            text.fontStyle = FontStyle.Italic;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.28f, 0.05f);
            component.position = new Vector3(0.06f, 0f, 0.185f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            if (fpsCounter)
            {
                fpsObject = new GameObject
                {
                    transform =
            {
                parent = canvasObject.transform
            }
                }.AddComponent<Text>();
                fpsObject.font = currentFont;
                fpsObject.text = currentCategory + "|" + pageNumber + "FPS: " + Mathf.Ceil(1f / Time.unscaledDeltaTime).ToString();
                fpsObject.color = textColors[0];
                fpsObject.fontSize = 1;
                fpsObject.supportRichText = true;
                fpsObject.fontStyle = FontStyle.Italic;
                fpsObject.alignment = TextAnchor.MiddleCenter;
                fpsObject.horizontalOverflow = HorizontalWrapMode.Overflow;
                fpsObject.resizeTextForBestFit = true;
                fpsObject.resizeTextMinSize = 0;
                RectTransform component2 = fpsObject.GetComponent<RectTransform>();
                component2.localPosition = Vector3.zero;
                component2.sizeDelta = new Vector2(0.28f, 0.02f);
                component2.position = new Vector3(0.06f, 0f, 0.140f);
                component2.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (disconnectButton)
            {
                GameObject disconnectbutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UnityInput.Current.GetKey(KeyCode.Q))
                {
                    disconnectbutton.layer = 2;
                }
                UnityEngine.Object.Destroy(disconnectbutton.GetComponent<Rigidbody>());
                disconnectbutton.GetComponent<BoxCollider>().isTrigger = true;
                disconnectbutton.transform.parent = menu.transform;
                disconnectbutton.transform.rotation = Quaternion.identity;
                disconnectbutton.transform.localScale = disconnectButtonSize;
                disconnectbutton.transform.localPosition = new Vector3(0.56f, 0f, 0.62f);
                disconnectbutton.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                disconnectbutton.AddComponent<liquidclient.Components.Button>().relatedText = "Disconnect";
                RoundObj(disconnectbutton, 0.03f);

                /*GameObject disconnectbuttonOutline = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UnityInput.Current.GetKey(keyboardButton))
                {
                    disconnectbutton.layer = 2;
                }
                UnityEngine.Object.Destroy(disconnectbuttonOutline.GetComponent<Rigidbody>());
                disconnectbuttonOutline.GetComponent<BoxCollider>().isTrigger = true;
                disconnectbuttonOutline.transform.parent = menu.transform;
                disconnectbuttonOutline.transform.rotation = Quaternion.identity;
                disconnectbuttonOutline.transform.localScale = new Vector3(0.089f, 0.91f, 0.091f);
                disconnectbuttonOutline.transform.localPosition = new Vector3(0.56f, 0f, 0.62f);
                disconnectbuttonOutline.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;*/
                RoundObj(disconnectbutton, 0.03f);

                Text discontext = new GameObject
                {
                    transform =
            {
                parent = canvasObject.transform
            }
                }.AddComponent<Text>();
                discontext.text = "Disconect";
                discontext.font = currentFont;
                discontext.fontSize = 1;
                discontext.color = textColors[0];
                discontext.alignment = TextAnchor.MiddleCenter;
                discontext.resizeTextForBestFit = true;
                discontext.resizeTextMinSize = 0;

                RectTransform rectt = discontext.GetComponent<RectTransform>();
                rectt.localPosition = Vector3.zero;
                rectt.sizeDelta = new Vector2(0.1f, 0.03f);
                rectt.localPosition = new Vector3(0.064f, 0f, 0.238f);
                rectt.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (homeButton && currentCategory != 0)
            {
                GameObject homebutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UnityInput.Current.GetKey(KeyCode.Q))
                {
                    homebutton.layer = 2;
                }
                UnityEngine.Object.Destroy(homebutton.GetComponent<Rigidbody>());
                homebutton.GetComponent<BoxCollider>().isTrigger = true;
                homebutton.transform.parent = menu.transform;
                homebutton.transform.rotation = Quaternion.identity;
                homebutton.transform.localScale = homeButtonSize;
                homebutton.transform.localPosition = new Vector3(0.56f, 0f, -0.433f);
                homebutton.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
                homebutton.AddComponent<Components.Button>().relatedText = "Home";
                RoundObj(homebutton);

                Text hometext = new GameObject
                {
                    transform =
            {
                parent = canvasObject.transform
            }
                }.AddComponent<Text>();
                hometext.text = "⌂";
                hometext.font = currentFont;
                hometext.fontSize = 1;
                hometext.color = textColors[0];
                hometext.alignment = TextAnchor.MiddleCenter;
                hometext.resizeTextForBestFit = true;
                hometext.resizeTextMinSize = 0;

                RectTransform rectt = hometext.GetComponent<RectTransform>();
                rectt.localPosition = Vector3.zero;
                rectt.sizeDelta = new Vector2(0.1f, 0.03f);
                rectt.localPosition = new Vector3(0.065f, 0f, -0.157f);
                rectt.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }

            if (settingsButton)
            {
                GameObject settingsbutton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UnityInput.Current.GetKey(KeyCode.Q))
                {
                    settingsbutton.layer = 2;
                }
                UnityEngine.Object.Destroy(settingsbutton.GetComponent<Rigidbody>());
                settingsbutton.GetComponent<BoxCollider>().isTrigger = true;
                settingsbutton.transform.parent = menu.transform;
                settingsbutton.transform.rotation = Quaternion.identity;
                settingsbutton.transform.localScale = settingsButtonSize;
                settingsbutton.transform.localPosition = new Vector3(0.56f, -0.600f, -0.500f);
                settingsbutton.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
                settingsbutton.AddComponent<Components.Button>().relatedText = "Settings";
                RoundObj(settingsbutton);

                /*GameObject settingsbuttonOutline = GameObject.CreatePrimitive(PrimitiveType.Cube);
                if (!UnityInput.Current.GetKey(keyboardButton))
                {
                    settingsbutton.layer = 2;
                }
                UnityEngine.Object.Destroy(settingsbuttonOutline.GetComponent<Rigidbody>());
                settingsbuttonOutline.GetComponent<BoxCollider>().isTrigger = true;
                settingsbuttonOutline.transform.parent = menu.transform;
                settingsbuttonOutline.transform.rotation = Quaternion.identity;
                settingsbuttonOutline.transform.localScale = new Vector3(0.056f, 0.12f, 0.09f);
                settingsbuttonOutline.transform.localPosition = new Vector3(0.56f, -0.6025f, -0.502f);
                settingsbuttonOutline.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;*/
                RoundObj(settingsbutton);

                Text settingstext = new GameObject
                {
                    transform =
            {
                parent = canvasObject.transform
            }
                }.AddComponent<Text>();
                settingstext.text = "⋮";
                settingstext.font = currentFont;
                settingstext.fontSize = 1;
                settingstext.color = Color.white;
                settingstext.alignment = TextAnchor.MiddleCenter;
                settingstext.resizeTextForBestFit = true;
                settingstext.resizeTextMinSize = 0;

                RectTransform rectt = settingstext.GetComponent<RectTransform>();
                rectt.localPosition = Vector3.zero;
                rectt.sizeDelta = new Vector2(0.05f, 0.020f);
                rectt.localPosition = new Vector3(0.064f, -0.180f, -0.190f);
                rectt.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
            }
            
            
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UnityInput.Current.GetKey(KeyCode.Q))
            {
                gameObject.layer = 2;
            }
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(0.06f, 0.250f, 0.08f);
            gameObject.transform.localPosition = new Vector3(0.56f, 0.295f, -0.433f);
            gameObject.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            gameObject.AddComponent<Components.Button>().relatedText = "PreviousPage";
            RoundObj(gameObject);

            text = new GameObject
            {
                transform =
        {
            parent = canvasObject.transform
        }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = "<";
            text.fontSize = 1;
            text.color = textColors[0];
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.2f, 0.03f);
            component.localPosition = new Vector3(0.06f, 0.090f, -0.160f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UnityInput.Current.GetKey(KeyCode.Q))
            {
                gameObject.layer = 2;
            }
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(0.06f, 0.250f, 0.08f);
            gameObject.transform.localPosition = new Vector3(0.56f, -0.295f, -0.433f);
            gameObject.GetComponent<Renderer>().material.color = buttonColors[0].colors[0].color;
            gameObject.AddComponent<Components.Button>().relatedText = "NextPage";
            RoundObj(gameObject);

            text = new GameObject
            {
                transform =
        {
            parent = canvasObject.transform
        }
            }.AddComponent<Text>();
            text.font = currentFont;
            text.text = ">";
            text.fontSize = 1;
            text.color = textColors[0];
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(0.2f, 0.03f);
            component.localPosition = new Vector3(0.06f, -0.090f, -0.160f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));

            // Mod Buttons
            ButtonInfo[] activeButtons = buttons[currentCategory].Skip(pageNumber * buttonsPerPage).Take(buttonsPerPage).ToArray();
            for (int i = 0; i < activeButtons.Length; i++)
                CreateButton(i * 0.1f, activeButtons[i]);
        }

        public static void CreateButton(float offset, ButtonInfo method)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!UnityInput.Current.GetKey(KeyCode.Q))
            {
                gameObject.layer = 2;
            }
            UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
            gameObject.GetComponent<BoxCollider>().isTrigger = true;
            gameObject.transform.parent = menu.transform;
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = new Vector3(0.09f, 0.8f, 0.08f);
            gameObject.transform.localPosition = new Vector3(0.56f, 0f, 0.28f - offset);
            gameObject.GetComponent<Renderer>().material.color = buttonColors[method.enabled ? 1 : 0].colors[0].color;
            gameObject.AddComponent<Components.Button>().relatedText = method.buttonText;
            RoundObj(gameObject);



            Text text = new GameObject
            {
                transform =
                {
                    parent = canvasObject.transform
                }
            }.AddComponent<Text>();
            if (method.overlapText != null)
                text.text = method.overlapText;
            text.font = currentFont;
            text.text = method.buttonText;
            text.supportRichText = true;
            text.fontSize = 1;
            if (method.enabled)
            {
                text.color = textColors[1];
            }
            else
            {
                text.color = textColors[0];
            }
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Italic;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 0;
            RectTransform component = text.GetComponent<RectTransform>();
            component.localPosition = Vector3.zero;
            component.sizeDelta = new Vector2(.2f, .03f);
            component.localPosition = new Vector3(.064f, 0, 0.111f - offset / 2.7f);
            component.rotation = Quaternion.Euler(new Vector3(180f, 90f, 90f));
        }


        public static void RecreateMenu()
        {
            if (menu != null)
            {
                UnityEngine.Object.Destroy(menu);
                menu = null;

                CreateMenu();
                RecenterMenu(rightHanded, UnityInput.Current.GetKey(keyboardButton));
                //RecenterMenu(rightHanded);
            }
        }

        public static void RecenterMenu(bool isRightHanded, bool isKeyboardCondition)
        {
            if (!isKeyboardCondition)
            {
                if (!isRightHanded)
                {
                    menu.transform.position = GorillaTagger.Instance.leftHandTransform.position;
                    menu.transform.rotation = GorillaTagger.Instance.leftHandTransform.rotation;
                }
                else
                {
                    menu.transform.position = GorillaTagger.Instance.rightHandTransform.position;
                    Vector3 rotation = GorillaTagger.Instance.rightHandTransform.rotation.eulerAngles;
                    rotation += new Vector3(0f, 0f, 180f);
                    menu.transform.rotation = Quaternion.Euler(rotation);
                }
            }
            else
            {
                try
                {
                    TPC = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera").GetComponent<Camera>();
                }
                catch { }

                GameObject.Find("Shoulder Camera").transform.Find("CM vcam1").gameObject.SetActive(false);

                if (TPC != null)
                {
                    TPC.transform.position = new Vector3(-999f, -999f, -999f);
                    TPC.transform.rotation = Quaternion.identity;
                    GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bg.transform.localScale = new Vector3(10f, 10f, 0.01f);
                    bg.transform.transform.position = TPC.transform.position + TPC.transform.forward;
                    Color realcolor = backgroundColor.GetCurrentColor();
                    bg.GetComponent<Renderer>().material.color = new Color32((byte)(realcolor.r * 50), (byte)(realcolor.g * 50), (byte)(realcolor.b * 50), 255);
                    UnityEngine.Object.Destroy(bg, 0.05f);
                    menu.transform.parent = TPC.transform;
                    menu.transform.position = TPC.transform.position + (TPC.transform.forward * 0.52f) + (TPC.transform.up * -0.02f);
                    menu.transform.rotation = TPC.transform.rotation * Quaternion.Euler(-90f, 90f, 0f);

                    if (reference != null)
                    {
                        if (Mouse.current.leftButton.isPressed)
                        {
                            Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                            bool hitButton = Physics.Raycast(ray, out RaycastHit hit, 100);
                            if (hitButton)
                            {
                                liquidclient.Components.Button collide = hit.transform.gameObject.GetComponent<liquidclient.Components.Button>();
                                collide?.OnTriggerEnter(buttonCollider);
                            }
                        }
                        else
                            reference.transform.position = new Vector3(999f, -999f, -999f);
                    }
                }
            }
        }

        public static void CreateReference(bool isRightHanded)
        {
            reference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reference.transform.parent = isRightHanded ? GorillaTagger.Instance.leftHandTransform : GorillaTagger.Instance.rightHandTransform;
            reference.GetComponent<Renderer>().material.color = backgroundColor.colors[0].color;
            reference.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            reference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            buttonCollider = reference.GetComponent<SphereCollider>();

            ColorChanger colorChanger = reference.AddComponent<ColorChanger>();
            colorChanger.colors = backgroundColor;
        }

        public static void Toggle(string buttonText)
        {
            if (currentCategory < 0 || currentCategory >= buttons.Length || buttons[currentCategory] == null)
            {
                return;
            }

            int lastPage = ((buttons[currentCategory].Length + buttonsPerPage - 1) / buttonsPerPage) - 1;

            if (buttonText == "Disconnect")
            {
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Disconnect();
                }
                return;
            }

            if (buttonText == "Home")
            {
                currentCategory = 0;
                RecreateMenu();
                return;
            }

            if (buttonText == "Settings")
            {
                currentCategory = 1;
                RecreateMenu();
                return;
            }

            if (buttonText == "PreviousPage")
            {
                pageNumber--;
                if (pageNumber < 0)
                    pageNumber = lastPage;
            }
            else if (buttonText == "NextPage")
            {
                pageNumber++;
                if (pageNumber > lastPage)
                    pageNumber = 0;
            }
            else
            {
                ButtonInfo target = Main.GetIndex(buttonText);
                if (target != null)
                {
                    if (target.isTogglable)
                    {
                        target.enabled = !target.enabled;
                        if (target.enabled)
                        {
                            NotifiLib.Send($"<color=blue>{target.toolTip}</color>");
                            if (target.enableMethod != null)
                                try { target.enableMethod.Invoke(); } catch { }
                        }
                        else
                        {
                            NotifiLib.Send( $"<color=blue>{target.toolTip}</color>");
                            if (target.disableMethod != null)
                                try { target.disableMethod.Invoke(); } catch { }
                        }
                    }
                    else
                    {
                        NotifiLib.Send( $"<color=blue>{target.toolTip}</color>");
                        if (target.method != null)
                            try { target.method.Invoke(); } catch { }
                    }
                }
            }
            RecreateMenu();
        }

        private static readonly Dictionary<string, (int Category, int Index)> cacheGetIndex = new Dictionary<string, (int Category, int Index)>(); // Looping through 800 elements is not a light task :/
        public static ButtonInfo GetIndex(string buttonText)
        {
            if (buttonText == null)
                return null;

            if (cacheGetIndex.ContainsKey(buttonText))
            {
                var CacheData = cacheGetIndex[buttonText];
                try
                {
                    if (buttons[CacheData.Category][CacheData.Index].buttonText == buttonText)
                        return buttons[CacheData.Category][CacheData.Index];
                }
                catch { cacheGetIndex.Remove(buttonText); }
            }

            int categoryIndex = 0;
            foreach (ButtonInfo[] buttons in buttons)
            {
                int buttonIndex = 0;
                foreach (ButtonInfo button in buttons)
                {
                    if (button.buttonText == buttonText)
                    {
                        try
                        {
                            cacheGetIndex.Add(buttonText, (categoryIndex, buttonIndex));
                        }
                        catch
                        {
                            if (cacheGetIndex.ContainsKey(buttonText))
                                cacheGetIndex.Remove(buttonText);
                        }

                        return button;
                    }
                    buttonIndex++;
                }
                categoryIndex++;
            }

            return null;
        }

        public static Color RandomColor(byte range = 255, byte alpha = 255) =>
            new Color32((byte)UnityEngine.Random.Range(0, range),
                        (byte)UnityEngine.Random.Range(0, range),
                        (byte)UnityEngine.Random.Range(0, range),
                        alpha);

        public static void WorldScale(GameObject obj, Vector3 targetWorldScale)
        {
            Vector3 parentScale = obj.transform.parent.lossyScale;
            obj.transform.localScale = new Vector3(
                targetWorldScale.x / parentScale.x,
                targetWorldScale.y / parentScale.y,
                targetWorldScale.z / parentScale.z
            );
        }

        private static int? noInvisLayerMask;
        public static int NoInvisLayerMask()
        {
            noInvisLayerMask ??= ~(
                1 << LayerMask.NameToLayer("TransparentFX") |
                1 << LayerMask.NameToLayer("Ignore Raycast") |
                1 << LayerMask.NameToLayer("Zone") |
                1 << LayerMask.NameToLayer("Gorilla Trigger") |
                1 << LayerMask.NameToLayer("Gorilla Boundary") |
                1 << LayerMask.NameToLayer("GorillaCosmetics") |
                1 << LayerMask.NameToLayer("GorillaParticle"));

            return noInvisLayerMask ?? GTPlayer.Instance.locomotionEnabledLayers;
        }

        public static void RoundObj(GameObject toRound, float bevel = 0.025f, string sides = "both")
        {
            Renderer ToRoundRenderer = toRound.GetComponent<Renderer>();

            GameObject BaseA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(BaseA.GetComponent<Collider>());
            BaseA.transform.parent = menu.transform;
            BaseA.transform.rotation = Quaternion.identity;
            BaseA.transform.localPosition = toRound.transform.localPosition;
            BaseA.transform.localScale = toRound.transform.localScale + new Vector3(0f, bevel * -2.55f, 0f);

            GameObject BaseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(BaseB.GetComponent<Collider>());
            BaseB.transform.parent = menu.transform;
            BaseB.transform.rotation = Quaternion.identity;
            BaseB.transform.localPosition = toRound.transform.localPosition;
            BaseB.transform.localScale = toRound.transform.localScale + new Vector3(0f, 0f, -bevel * 2f);

            List<GameObject> ToChange = new List<GameObject> { BaseA, BaseB };

            if (sides == "both" || sides == "bottom" || sides == "left")
            {
                GameObject RoundCornerD = CreateRoundedCorner(toRound, ToRoundRenderer, bevel,
                    new Vector3(0f, -(toRound.transform.localScale.y / 2f) + (bevel * 1.275f), -(toRound.transform.localScale.z / 2f) + bevel));

                GameObject RoundCornerB = CreateRoundedCorner(toRound, ToRoundRenderer, bevel,
                    new Vector3(0f, -(toRound.transform.localScale.y / 2f) + (bevel * 1.275f), (toRound.transform.localScale.z / 2f) - bevel));

                ToChange.Add(RoundCornerD);
                ToChange.Add(RoundCornerB);
            }

            if (sides == "both" || sides == "top" || sides == "right")
            {
                GameObject RoundCornerC = CreateRoundedCorner(toRound, ToRoundRenderer, bevel,
                    new Vector3(0f, (toRound.transform.localScale.y / 2f) - (bevel * 1.275f), -(toRound.transform.localScale.z / 2f) + bevel));

                GameObject RoundCornerA = CreateRoundedCorner(toRound, ToRoundRenderer, bevel,
                    new Vector3(0f, (toRound.transform.localScale.y / 2f) - (bevel * 1.275f), (toRound.transform.localScale.z / 2f) - bevel));

                ToChange.Add(RoundCornerC);
                ToChange.Add(RoundCornerA);
            }

            foreach (GameObject Changed in ToChange)
            {
                ColorRoundObj TargetChanger = Changed.AddComponent<ColorRoundObj>();
                TargetChanger.targetRenderer = ToRoundRenderer;
                TargetChanger.Start();
            }

            ToRoundRenderer.enabled = false;
        }

        private static GameObject CreateRoundedCorner(GameObject parent, Renderer renderer, float bevel, Vector3 localPositionOffset)
        {

            GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            corner.GetComponent<Renderer>().enabled = renderer.enabled;
            UnityEngine.Object.Destroy(corner.GetComponent<Collider>());

            corner.transform.parent = menu.transform;
            corner.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);
            corner.transform.localPosition = parent.transform.localPosition + localPositionOffset;
            corner.transform.localScale = new Vector3(bevel * 2.55f, parent.transform.localScale.x / 2f, bevel * 2f);

            return corner;
        }

        public static void RoundLeftObj(GameObject toRound)
        {
            float Bevel = 0.1f;

            Renderer ToRoundRenderer = toRound.GetComponent<Renderer>();
            GameObject BaseA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseA.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(BaseA.GetComponent<Collider>());

            BaseA.transform.parent = menu.transform;
            BaseA.transform.rotation = Quaternion.identity;
            BaseA.transform.localPosition = toRound.transform.localPosition;
            BaseA.transform.localScale = toRound.transform.localScale;

            GameObject BaseB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            BaseB.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(BaseB.GetComponent<Collider>());

            BaseB.transform.parent = menu.transform;
            BaseB.transform.rotation = Quaternion.identity;
            BaseB.transform.localPosition = toRound.transform.localPosition;
            BaseB.transform.localScale = toRound.transform.localScale + new Vector3(0f, 0f, -Bevel * 2f);

            GameObject RoundCornerA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerA.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(RoundCornerA.GetComponent<Collider>());

            RoundCornerA.transform.parent = menu.transform;
            RoundCornerA.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerA.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, (toRound.transform.localScale.y / 2f) - (Bevel * 1.275f), -(toRound.transform.localScale.z / 2f) + Bevel);
            RoundCornerA.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject RoundCornerB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            RoundCornerB.GetComponent<Renderer>().enabled = ToRoundRenderer.enabled;
            UnityEngine.Object.Destroy(RoundCornerB.GetComponent<Collider>());

            RoundCornerB.transform.parent = menu.transform;
            RoundCornerB.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 0f, 90f);

            RoundCornerB.transform.localPosition = toRound.transform.localPosition + new Vector3(0f, -(toRound.transform.localScale.y / 2f) + (Bevel * 1.275f), -(toRound.transform.localScale.z / 2f) + Bevel);
            RoundCornerB.transform.localScale = new Vector3(Bevel * 2.55f, toRound.transform.localScale.x / 2f, Bevel * 2f);

            GameObject[] ToChange = new GameObject[]
            {
        BaseA,
        BaseB,
        RoundCornerA,
        RoundCornerB
            };

            foreach (GameObject Changed in ToChange)
            {
                ColorRoundObj TargetChanger = Changed.AddComponent<ColorRoundObj>();
                TargetChanger.targetRenderer = ToRoundRenderer;

                TargetChanger.Start();
            }

            ToRoundRenderer.enabled = false;
        }

        public static readonly Dictionary<Type, object[]> typePool = new Dictionary<Type, object[]>();
        private static readonly Dictionary<Type, float> receiveTypeDelay = new Dictionary<Type, float>();

        public static T[] GetAllType<T>(float decayTime = 5f) where T : Object
        {
            Type type = typeof(T);

            float lastReceivedTime = receiveTypeDelay.GetValueOrDefault(type, -1f);

            if (Time.time > lastReceivedTime)
            {
                typePool.Remove(type);
                receiveTypeDelay[type] = Time.time + decayTime;
            }

            if (!typePool.ContainsKey(type))
                typePool.Add(type, FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            return (T[])typePool[type];
        }

        public static void ToggleIncremental(string buttonText, bool increment, bool reload = true)
        {
            ButtonInfo target = GetIndex(buttonText);
            if (target != null)
            {
                string newIndicator = " <color=grey>[</color><color=green>New</color><color=grey>]</color>";
                if (target.overlapText != null && target.overlapText.Contains(newIndicator))
                {
                    target.overlapText = target.overlapText.Replace(newIndicator, "");
                    if (target.overlapText == target.buttonText)
                        target.overlapText = target.buttonText;
                }

                if (target.label)
                    return;
            }
        }

        private static float randomIndex;
        private static float randomDecayTime;


        // Variables
        // Important
        // Objects
        public static GameObject menu;
        public static GameObject menuBackground;
        public static GameObject reference;
        public static GameObject canvasObject;
        public static GameObject ColorRoundObj;

        public static SphereCollider buttonCollider;
        public static Camera TPC;
        public static Text fpsObject;

        private static GameObject GunPointer;
        private static LineRenderer GunLine;

        public static string CosmeticsOwned;

        // Data
        public static int pageNumber = 0;
        public static int _currentCategory;
        public static int currentCategory
        {
            get => _currentCategory;
            set
            {
                if (value >= 0 && value < buttons.Length)
                {
                    _currentCategory = value;
                    pageNumber = 0;
                }
                else
                {
                    _currentCategory = 0;
                }
            }
        }
    }
}