using System.IO;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using UnityEngine;

namespace Game.Mods.UncannyUI.Scripts
{
    public class UncannyUILoader : MonoBehaviour
    {
        internal const string _texturesFolder = "UI";
        public static Mod Mod { get; private set; }

        static UncannyUILoader _instance;

        protected readonly string DialougeString = "uncanny_dialouge";
        protected readonly string InventoryString = "uncanny_inventory";
        protected readonly string InventoryHighlightString = "uncanny_inventoryh";
        protected readonly string BuyOverlayString = "uncanny_buy_overlay";
        protected readonly string BuyOverlayHighlightString = "uncanny_buy_overlayh";
        protected readonly string SellOverlayString = "uncanny_sell_overlay";
        protected readonly string SellOverlayHighlightString = "uncanny_sell_overlayh";
        protected readonly string RestString = "uncanny_rest";

        private Color _defaultColor = new Color32(243, 239, 44, 255);


        public static UncannyUILoader Instance
        {
            get { return _instance != null ? _instance : (_instance = FindObjectOfType<UncannyUILoader>()); }
        }

        public Color DefaultColor()
        {
            return _defaultColor;
        }

        public Mod GetMod()
        {
            return Mod;
        }

        // Use this for initialization
        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            Mod = initParams.Mod;  // Get mod

            var settings = Mod.GetSettings();

            var dialogueWindowEnabled = settings.GetBool("Dialogue", "Enabled");
            var inventoryWindowEnabled = settings.GetBool("Inventory", "Enabled");
            var restWindowEnabled = settings.GetBool("Rest", "Enabled");

            if (dialogueWindowEnabled)
            {
                UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Talk, typeof(UncannyTalkWindow));
            }

            if(inventoryWindowEnabled)
            {
                UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Inventory, typeof(UncannyInventoryWindow));
            }

            if(restWindowEnabled)
            {
                UIWindowFactory.RegisterCustomUIWindow(UIWindowType.Rest, typeof(UncannyRestWindow));
            }
           

            _instance = new GameObject("UncannyUI").AddComponent<UncannyUILoader>(); // Add script to the scene.
            _instance._defaultColor = settings.GetColor("Common", "DefaultColor");
        }


        public DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings.ModSettings GetModSettings()
        {
            return Mod.GetSettings();
        }

        public Texture2D LoadDialougeUI()
        {
            return LoadTexture(DialougeString);
        }

        public Texture2D LoadInventoryUI()
        {
            return LoadTexture(InventoryString);
        }

        public Texture2D LoadInventoryHighlightUI()
        {
            return LoadTexture(InventoryHighlightString);
        }

        public Texture2D LoadBuyUI()
        {
            return LoadTexture(BuyOverlayString);
        }

        public Texture2D LoadBuyHighlightUI()
        {
            return LoadTexture(BuyOverlayHighlightString);
        }

        public Texture2D LoadSellUI()
        {
            return LoadTexture(SellOverlayString);
        }

        public Texture2D LoadSellHighlightUI()
        {
            return LoadTexture(SellOverlayHighlightString);
        }

        public Texture2D LoadRestUI()
        {
            return LoadTexture(RestString);
        }

        private Texture2D LoadTexture(string name)
        {
            Texture2D tex;

            TextureReplacement.TryImportTextureFromLooseFiles(Path.Combine(_texturesFolder, name), false, false, false, out tex);

            if(tex == null)
                tex = Mod.GetAsset<Texture2D>(name);

            tex.filterMode = FilterMode.Point;

            return tex;
        }
    }
}