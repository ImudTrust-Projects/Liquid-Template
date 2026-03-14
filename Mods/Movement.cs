using BepInEx;
using GorillaLocomotion;
using liquidclient.Classes;
using liquidclient.Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using static liquidclient.Menu.Main;

namespace liquidclient.Mods
{
    public class Movement
    {
        public static GameObject RightHandBottom, RightHandTop, RightHandLeft, RightHandRight, RightHandFront, RightHandBack;

        public static GameObject LeftHandBottom, LeftHandTop, LeftHandLeft, LeftHandRight, LeftHandFront, LeftHandBack;
        public static Rigidbody Rig_Rigidbody = GorillaTagger.Instance.rigidbody;

        private static GameObject leftplat = null;
        private static GameObject rightplat = null;

        private static GameObject CreatePlatform(Transform hand)
        {
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);

            plat.transform.localScale = new Vector3(0.025f, 0.3f, 0.4f);
            plat.transform.position = hand.position;
            plat.transform.rotation = hand.rotation;

            ColorChanger c = plat.AddComponent<ColorChanger>();
            c.colors = Settings.backgroundColor;

            return plat;
        }

        public static float startX = -1f;
        public static float startY = -1f;

        public static float subThingy;
        public static float subThingyZ;

        public static void WASDFly()
        {
            GorillaLocomotion.GTPlayer.Instance.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, 0.067f, 0f);

            bool W = UnityInput.Current.GetKey(KeyCode.W);
            bool A = UnityInput.Current.GetKey(KeyCode.A);
            bool S = UnityInput.Current.GetKey(KeyCode.S);
            bool D = UnityInput.Current.GetKey(KeyCode.D);
            bool Space = UnityInput.Current.GetKey(KeyCode.Space);
            bool Ctrl = UnityInput.Current.GetKey(KeyCode.LeftControl);

            if (Mouse.current.rightButton.isPressed)
            {
                Transform parentTransform = GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.parent;
                Quaternion currentRotation = parentTransform.rotation;
                Vector3 euler = currentRotation.eulerAngles;

                if (startX < 0)
                {
                    startX = euler.y;
                    subThingy = Mouse.current.position.value.x / UnityEngine.Screen.width;
                }
                if (startY < 0)
                {
                    startY = euler.x;
                    subThingyZ = Mouse.current.position.value.y / UnityEngine.Screen.height;
                }

                float newX = startY - ((((Mouse.current.position.value.y / UnityEngine.Screen.height) - subThingyZ) * 360) * 1.33f);
                float newY = startX + ((((Mouse.current.position.value.x / UnityEngine.Screen.width) - subThingy) * 360) * 1.33f);

                newX = (newX > 180f) ? newX - 360f : newX;
                newX = Mathf.Clamp(newX, -90f, 90f);

                parentTransform.rotation = Quaternion.Euler(newX, newY, euler.z);
            }
            else
            {
                startX = -1;
                startY = -1;
            }

            float speed = 15f;
            if (UnityInput.Current.GetKey(KeyCode.LeftShift))
                speed *= 2f;
            if (W)
            {
                GorillaTagger.Instance.rigidbody.transform.position += GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.parent.forward * Time.deltaTime * speed;
            }

            if (S)
            {
                GorillaTagger.Instance.rigidbody.transform.position += GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.parent.forward * Time.deltaTime * -speed;
            }

            if (A)
            {
                GorillaTagger.Instance.rigidbody.transform.position += GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.parent.right * Time.deltaTime * -speed;
            }

            if (D)
            {
                GorillaTagger.Instance.rigidbody.transform.position += GorillaLocomotion.GTPlayer.Instance.RightHand.controllerTransform.parent.right * Time.deltaTime * speed;
            }

            if (Space)
            {
                GorillaTagger.Instance.rigidbody.transform.position += new Vector3(0f, Time.deltaTime * speed, 0f);
            }

            if (Ctrl)
            {
                GorillaTagger.Instance.rigidbody.transform.position += new Vector3(0f, Time.deltaTime * -speed, 0f);
            }
            VRRig.LocalRig.head.rigTarget.transform.rotation = GorillaTagger.Instance.headCollider.transform.rotation;
        }

        public static void ZeroGravity() =>
             Rig_Rigidbody.AddForce(-Physics.gravity, ForceMode.Acceleration);


        public static void Platforms()
        {
            bool sticky = Main.GetIndex("Sticky Platforms").enabled;

            if (sticky)
            {
                if (ControllerInputPoller.instance.rightGrab && RightHandBottom == null)
                {
                    Transform hand = GorillaTagger.Instance.rightHandTransform;

                    RightHandBottom = CreatePlatform(hand);
                    RightHandBottom.transform.position = hand.position - new Vector3(0, 0.05f, 0);

                    RightHandTop = CreatePlatform(hand);
                    RightHandTop.transform.position = hand.position + new Vector3(0, 0.05f, 0);
                    RightHandTop.GetComponent<Renderer>().enabled = false;

                    RightHandLeft = CreatePlatform(hand);
                    RightHandLeft.transform.position = hand.position - new Vector3(0.05f, 0, 0);
                    RightHandLeft.GetComponent<Renderer>().enabled = false;

                    RightHandRight = CreatePlatform(hand);
                    RightHandRight.transform.position = hand.position + new Vector3(0.05f, 0, 0);
                    RightHandRight.GetComponent<Renderer>().enabled = false;

                    RightHandFront = CreatePlatform(hand);
                    RightHandFront.transform.position = hand.position + new Vector3(0, 0, 0.05f);
                    RightHandFront.GetComponent<Renderer>().enabled = false;

                    RightHandBack = CreatePlatform(hand);
                    RightHandBack.transform.position = hand.position - new Vector3(0, 0, 0.05f);
                    RightHandBack.GetComponent<Renderer>().enabled = false;
                }

                if (!ControllerInputPoller.instance.rightGrab)
                {
                    GameObject.Destroy(RightHandBottom);
                    GameObject.Destroy(RightHandTop);
                    GameObject.Destroy(RightHandLeft);
                    GameObject.Destroy(RightHandRight);
                    GameObject.Destroy(RightHandFront);
                    GameObject.Destroy(RightHandBack);

                    RightHandBottom = RightHandTop = RightHandLeft = RightHandRight = RightHandFront = RightHandBack = null;
                }

                if (ControllerInputPoller.instance.leftGrab && LeftHandBottom == null)
                {
                    Transform hand = GorillaTagger.Instance.leftHandTransform;

                    LeftHandBottom = CreatePlatform(hand);
                    LeftHandBottom.transform.position = hand.position - new Vector3(0, 0.05f, 0);

                    LeftHandTop = CreatePlatform(hand);
                    LeftHandTop.transform.position = hand.position + new Vector3(0, 0.05f, 0);
                    LeftHandTop.GetComponent<Renderer>().enabled = false;

                    LeftHandLeft = CreatePlatform(hand);
                    LeftHandLeft.transform.position = hand.position - new Vector3(0.05f, 0, 0);
                    LeftHandLeft.GetComponent<Renderer>().enabled = false;

                    LeftHandRight = CreatePlatform(hand);
                    LeftHandRight.transform.position = hand.position + new Vector3(0.05f, 0, 0);
                    LeftHandRight.GetComponent<Renderer>().enabled = false;

                    LeftHandFront = CreatePlatform(hand);
                    LeftHandFront.transform.position = hand.position + new Vector3(0, 0, 0.05f);
                    LeftHandFront.GetComponent<Renderer>().enabled = false;

                    LeftHandBack = CreatePlatform(hand);
                    LeftHandBack.transform.position = hand.position - new Vector3(0, 0, 0.05f);
                    LeftHandBack.GetComponent<Renderer>().enabled = false;
                }

                if (!ControllerInputPoller.instance.leftGrab)
                {
                    GameObject.Destroy(LeftHandBottom);
                    GameObject.Destroy(LeftHandTop);
                    GameObject.Destroy(LeftHandLeft);
                    GameObject.Destroy(LeftHandRight);
                    GameObject.Destroy(LeftHandFront);
                    GameObject.Destroy(LeftHandBack);

                    LeftHandBottom = LeftHandTop = LeftHandLeft = LeftHandRight = LeftHandFront = LeftHandBack = null;
                }
            }

            else
            {
                if (ControllerInputPoller.instance.leftGrab && leftplat == null)
                    leftplat = CreatePlatform(GorillaTagger.Instance.leftHandTransform);

                if (ControllerInputPoller.instance.rightGrab && rightplat == null)
                    rightplat = CreatePlatform(GorillaTagger.Instance.rightHandTransform);

                if (ControllerInputPoller.instance.leftGrabRelease && leftplat != null)
                {
                    GameObject.Destroy(leftplat);
                    leftplat = null;
                }

                if (ControllerInputPoller.instance.rightGrabRelease && rightplat != null)
                {
                    GameObject.Destroy(rightplat);
                    rightplat = null;
                }
            }
        }
    }
}
