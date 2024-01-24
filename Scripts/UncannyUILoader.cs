using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility.AssetInjection;
using System.IO;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

public class UncannyUILoader : MonoBehaviour
{
    internal const string TexturesFolder = "UI";
    public static Mod mod { get; private set; }

    static UncannyUILoader instance;

    protected readonly string DialougeString = "uncanny_dialouge";
    protected readonly string InventoryString = "uncanny_inventory";
    protected readonly string InventoryHighlightString = "uncanny_inventoryh";
    protected readonly string BuyOverlayString = "uncanny_buy_overlay";
    protected readonly string BuyOverlayHighlightString = "uncanny_buy_overlayh";
    protected readonly string SellOverlayString = "uncanny_sell_overlay";
    protected readonly string SellOverlayHighlightString = "uncanny_sell_overlayh";
    protected readonly string RestString = "uncanny_rest";

    private Color defaultColor = new Color32(243, 239, 44, 255);


    public static UncannyUILoader Instance
    {
        get { return instance ?? (instance = FindObjectOfType<UncannyUILoader>()); }
    }

    public Color DefaultColor()
    {
        return defaultColor;
    }

    public Mod GetMod()
    {
        return mod;
    }

    // Use this for initialization
    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;  // Get mod

        var settings = mod.GetSettings();

        bool dialogueWindowEnabled = settings.GetBool("Dialogue", "Enabled");
        bool inventoryWindowEnabled = settings.GetBool("Inventory", "Enabled");
        bool restWindowEnabled = settings.GetBool("Rest", "Enabled");

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
           

        instance = new GameObject("UncannyUI").AddComponent<UncannyUILoader>(); // Add script to the scene.
        instance.defaultColor = settings.GetColor("Common", "DefaultColor");
    }


    public DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings.ModSettings GetModSettings()
    {
        return mod.GetSettings();
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

        TextureReplacement.TryImportTextureFromLooseFiles(Path.Combine(TexturesFolder, name), false, false, false, out tex);

        if(tex == null)
            tex = mod.GetAsset<Texture2D>(name);

        tex.filterMode = FilterMode.Point;

        return tex;
    }
}