using UnityEngine;
using System.Collections.Generic;
using liquidclient.Classes;

namespace liquidclient
{
    public class Settings
    {
        public static ExtGradient backgroundColor = new ExtGradient { rainbow = true};
        public static ExtGradient[] buttonColors = new ExtGradient[]
        {
            new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) }, // Disabled
            new ExtGradient { rainbow = true } // Enabled
        };
        public static Color[] textColors = new Color[]
        {
            Color.white, // Disabled
            Color.white // Enabled
        };

        public static Font currentFont = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

        public static bool fpsCounter = true;
        public static bool disconnectButton = true;
        public static bool rightHanded;
        public static bool disableNotifications;

        public static KeyCode keyboardButton = KeyCode.Q;
        public static int buttonsPerPage = 6;

        public static float gradientSpeed = 0.5f; // Speed of colors
        
        public static Vector3 menuSize = new Vector3(0.1f, 0.3f, 0.3825f); // if u change this also change menubackground size or else the menu will become big boi
        public static Vector3 menubackgroundSize = new Vector3(0.05f, 1.1f, 1.1f);
        
        public static Vector3 disconnectButtonSize = new Vector3(0.09f, 0.9f, 0.08f);
        public static Vector3 homeButtonSize = new Vector3(0.09f, 0.250f, 0.08f);
        public static Vector3 settingsButtonSize = new Vector3(0.1f, 0.1f, 0.08f);

        public static bool settingsButton = true;
        public static bool homeButton = true;
    }
}