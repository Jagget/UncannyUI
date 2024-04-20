using UnityEngine;
using System;
using System.Collections.Generic;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Banking;
using System.Linq;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace Game.Mods.UncannyUI.Scripts
{
    /// <summary>
    /// Implements inventory window.
    /// </summary>
    public class UncannyInventoryWindow : DaggerfallInventoryWindow
    {
        public static string LabelAmulets = UncannyUILoader.Instance.GetMod().Localize("Amulets");
        public static string LabelBracelets = UncannyUILoader.Instance.GetMod().Localize("Bracelets");
        public static string LabelRings = UncannyUILoader.Instance.GetMod().Localize("Rings");
        public static string LabelBracers = UncannyUILoader.Instance.GetMod().Localize("Bracers");
        public static string LabelMarks = UncannyUILoader.Instance.GetMod().Localize("Marks");
        public static string LabelCrystals = UncannyUILoader.Instance.GetMod().Localize("Crystals");
        public static string LabelEquip = UncannyUILoader.Instance.GetMod().Localize("Equip");
        public static string LabelUse = UncannyUILoader.Instance.GetMod().Localize("Use");
        public static string LabelInfo = UncannyUILoader.Instance.GetMod().Localize("Info");
        public static string LabelExit = UncannyUILoader.Instance.GetMod().Localize("Exit");
        public static string LabelWeaponsAndArmorButtonTooltip = UncannyUILoader.Instance.GetMod().Localize("weaponsAndArmorButtonTooltip");
        public static string LabelMagicItemsButtonTooltip = UncannyUILoader.Instance.GetMod().Localize("magicItemsButtonTooltip");
        public static string LabelClothingAndMiscButtonTooltip = UncannyUILoader.Instance.GetMod().Localize("clothingAndMiscButtonTooltip");
        public static string LabelIngredientsButtonTooltip = UncannyUILoader.Instance.GetMod().Localize("ingredientsButtonTooltip");

        private DaggerfallUnityItem _stackItem;
        private ItemCollection _stackFrom;
        private ItemCollection _stackTo;
        private bool _stackEquip;

        int _dropIconArchive;
        int _dropIconTexture;

        ItemCollection.AddPosition _preferredOrder = ItemCollection.AddPosition.DontCare;

        private int _maxAmount;

        Button _remoteButton;
        Rect _remoteButtonRect = new Rect(274, 10, 23, 18);

        readonly Rect[] _itemLocalButtonRects = new[]
        {
            new Rect(0, 0, 23, 22),
            new Rect(23, 0, 23, 22),
            new Rect(46, 0, 23, 22),
            new Rect(69, 0, 23, 22),
            new Rect(0, 22, 23, 22),
            new Rect(23, 22, 23, 22),
            new Rect(46, 22, 23, 22),
            new Rect(69, 22, 23, 22),
            new Rect(0, 44, 23, 22),
            new Rect(23, 44, 23, 22),
            new Rect(46, 44, 23, 22),
            new Rect(69, 44, 23, 22),
            new Rect(0, 66, 23, 22),
            new Rect(23, 66, 23, 22),
            new Rect(46, 66, 23, 22),
            new Rect(69, 66, 23, 22),
            new Rect(0, 88, 23, 22),
            new Rect(23, 88, 23, 22),
            new Rect(46, 88, 23, 22),
            new Rect(69, 88, 23, 22),
            new Rect(0, 110, 23, 22),
            new Rect(23, 110, 23, 22),
            new Rect(46, 110, 23, 22),
            new Rect(69, 110, 23, 22),
        };

        readonly Rect[] _itemRemoteButtonRects = new[]
        {
            new Rect(0, 0, 23, 22),
            new Rect(23, 0, 23, 22),
            new Rect(0, 22, 23, 22),
            new Rect(23, 22, 23, 22),
            new Rect(0, 44, 23, 22),
            new Rect(23, 44, 23, 22),
            new Rect(0, 66, 23, 22),
            new Rect(23, 66, 23, 22),
            new Rect(0, 88, 23, 22),
            new Rect(23, 88, 23, 22),
            new Rect(0, 110, 23, 22),
            new Rect(23, 110, 23, 22),
        };

        TextLabel equipButtonLabel, useButtonLabel, infoButtonLabel, exitButtonLabel;

        TextLabel characterNameLabel;
        Rect characterNameRect = new Rect(48, 1, 116, 9);

        TextLabel carryGoldLabel;
        Vector2 carryGoldPos = new Vector2(174, 2);
        TextLabel carryWeightLabel;
        Vector2 carryWeightPos = new Vector2(232, 2);
        TextLabel wagonCarryWeightLabel;
        Vector2 wagonCarryWeightPos = new Vector2(288, 2);


        string[] accessoryLabels = { LabelAmulets, LabelBracelets, LabelRings, LabelBracers, LabelMarks, LabelCrystals };
        Rect firstAccessoryRect = new Rect(0, 32, 46, 9);
        int accessoryYAdd = 31;

        Rect localItemListRect, remoteItemListRect;

        protected Texture2D remoteSelected, remoteNotSelected;

        private KeyCode _toggleClosedBinding;
        private bool _suppressInventory;
        private string _suppressInventoryMessage = string.Empty;

        private Panel _wagonIconPanel;
        private bool _hideTheWagon;
        private bool _isHmlModAdded;

        public UncannyInventoryWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
            StartGameBehaviour.OnNewGame += StartGameBehaviour_OnNewGame;
        }

        protected override void Setup()
        {
            var hiddenMapLocationsMod = ModManager.Instance.GetModFromGUID("7e487a8a-71e9-4187-990f-22012c11a04e");
            _isHmlModAdded = hiddenMapLocationsMod != null && hiddenMapLocationsMod.Enabled;

            localItemListScrollerRect = new Rect(163, 29, 101, 132);
            localItemListRect = new Rect(9, 0, 92, 132);

            remoteItemListScrollerRect = new Rect(265, 29, 55, 132);
            remoteItemListRect = new Rect(9, 0, 46, 132);

            wagonButtonRect = new Rect(297, 10, 23, 18);

            itemInfoPanelRect = new Rect(166, 165, 75, 32);

            weaponsAndArmorRect = new Rect(172, 10, 23, 18);
            magicItemsRect = new Rect(195, 10, 23, 18);
            clothingAndMiscRect = new Rect(218, 10, 23, 18);
            ingredientsRect = new Rect(241, 10, 23, 18);

            equipButtonRect = new Rect(243, 163, 31, 10);
            useButtonRect = new Rect(243, 175, 31, 10);
            infoButtonRect = new Rect(243, 187, 31, 10);
            exitButtonRect = new Rect(284, 186, 33, 12); //make the exit button larger to center the text better

            Panel characterNamePanel = DaggerfallUI.AddPanel(characterNameRect, NativePanel);
            characterNameLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, characterNamePanel);
            characterNameLabel.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;
            characterNameLabel.Text = GameManager.Instance.PlayerEntity.Name;
            characterNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            characterNameLabel.VerticalAlignment = VerticalAlignment.Middle;

            carryGoldLabel = DaggerfallUI.AddDefaultShadowedTextLabel(carryGoldPos, NativePanel);
            carryGoldLabel.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;

            carryWeightLabel = DaggerfallUI.AddDefaultShadowedTextLabel(carryWeightPos, NativePanel);
            carryWeightLabel.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;

            wagonCarryWeightLabel = DaggerfallUI.AddDefaultShadowedTextLabel(wagonCarryWeightPos, NativePanel);
            wagonCarryWeightLabel.TextColor = DaggerfallUI.DaggerfallUnityDefaultToolTipTextColor;

            _hideTheWagon = UncannyUILoader.Instance.GetModSettings().GetBool("Inventory", "HideTheWagon");

            goldButtonRect = new Rect(163, 0, 55, 10);

            // Load all the textures used by inventory system
            LoadTextures();

            // Always dim background
            ParentPanel.BackgroundColor = ScreenDimColor;

            // Setup native panel background
            NativePanel.BackgroundTexture = baseTexture;

            // Character portrait
            SetupPaperdoll();

            itemInfoPanel = DaggerfallUI.AddPanel(itemInfoPanelRect, NativePanel);
            OverideSetupItemInfoPanel();

            // Setup UI
            SetupTabPageButtons();
            SetupTabPageButtonTooltips();
            SetupActionButtons();
            OverrideSetupItemListScrollers();
            SetupAccessoryElements();

            // Exit buttons
            Button exitButton = DaggerfallUI.AddButton(exitButtonRect, NativePanel);
            exitButton.OnMouseClick += ExitButton_OnMouseClick;
            exitButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryExit);
            exitButton.OnKeyboardEvent += ExitButton_OnKeyboardEvent;

            //Buttons
            equipButtonLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, equipButton);
            equipButtonLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            equipButtonLabel.Text = UncannyInventoryWindow.LabelEquip;
            equipButtonLabel.HorizontalAlignment = HorizontalAlignment.Center;
            equipButtonLabel.VerticalAlignment = VerticalAlignment.Middle;

            useButtonLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, useButton);
            useButtonLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            useButtonLabel.Text = UncannyInventoryWindow.LabelUse;
            useButtonLabel.HorizontalAlignment = HorizontalAlignment.Center;
            useButtonLabel.VerticalAlignment = VerticalAlignment.Middle;

            infoButtonLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, infoButton);
            infoButtonLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            infoButtonLabel.Text = UncannyInventoryWindow.LabelInfo;
            infoButtonLabel.HorizontalAlignment = HorizontalAlignment.Center;
            infoButtonLabel.VerticalAlignment = VerticalAlignment.Middle;

            exitButtonLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, exitButton);
            exitButtonLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            exitButtonLabel.Text = UncannyInventoryWindow.LabelExit;
            exitButtonLabel.HorizontalAlignment = HorizontalAlignment.Center;
            exitButtonLabel.VerticalAlignment = VerticalAlignment.Middle;

            SetupAccessoryLabels();

            //Wagon icon & Text
            _wagonIconPanel = DaggerfallUI.AddPanel(new Rect(2, 2, wagonButton.InteriorWidth - 4, wagonButton.InteriorHeight - 4), wagonButton);
            _wagonIconPanel.BackgroundTexture = DaggerfallUnity.ItemHelper.GetContainerImage(InventoryContainerImages.Wagon).texture;
            _wagonIconPanel.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;

            wagonButton.BackgroundTexture = wagonNotSelected;
            _remoteButton.BackgroundTexture = remoteSelected;

            //Remote icons
            remoteTargetIconPanel = DaggerfallUI.AddPanel(new Rect(2, 2, _remoteButton.InteriorWidth - 4, _remoteButton.InteriorHeight - 4), _remoteButton);
            remoteTargetIconPanel.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;
            UpdateRemoteTargetIcon();

            // Setup initial state
            SelectTabPage(TabPages.WeaponsAndArmor);
            SelectActionMode(ActionModes.Equip);
            OverrideCheckWagonAccess();

            // Setup initial display
            FilterLocalItems();
            localItemListScroller.Items = localItemsFiltered;
            FilterRemoteItems();
            remoteItemListScroller.Items = remoteItemsFiltered;
            UpdateAccessoryItemsDisplay();

            // Store toggle closed binding for this window
            OverrideSetupClosedBinding();

            UpdateLocalTargetIcon();
        }

        void SetupAccessoryLabels()
        {
            for (var i = 0; i < accessoryLabels.Length; i++)
            {
                Panel newPanel = DaggerfallUI.AddPanel(
                    new Rect(firstAccessoryRect.x, firstAccessoryRect.y + (accessoryYAdd * i), firstAccessoryRect.width, firstAccessoryRect.height),
                    NativePanel);

                TextLabel newLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, newPanel);
                newLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
                newLabel.Text = accessoryLabels[i];
                newLabel.HorizontalAlignment = HorizontalAlignment.Center;
                newLabel.VerticalAlignment = VerticalAlignment.Middle;
            }
        }

        Texture2D[] MagicItemForegroundAnimationHander(DaggerfallUnityItem item)
        {
            return (item.IsEnchanted) ? magicAnimation.animatedTextures : null;
        }

        protected override void SetupActionButtons()
        {
            wagonButton = DaggerfallUI.AddButton(wagonButtonRect, NativePanel);
            wagonButton.OnMouseClick += WagonButton_OnMouseClick;
            wagonButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryWagon);

            infoButton = DaggerfallUI.AddButton(infoButtonRect, NativePanel);
            infoButton.OnMouseClick += InfoButton_OnMouseClick;
            infoButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryInfo);

            equipButton = DaggerfallUI.AddButton(equipButtonRect, NativePanel);
            equipButton.OnMouseClick += EquipButton_OnMouseClick;
            equipButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryEquip);

            useButton = DaggerfallUI.AddButton(useButtonRect, NativePanel);
            useButton.OnMouseClick += UseButton_OnMouseClick;
            useButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryUse);

            goldButton = DaggerfallUI.AddButton(goldButtonRect, NativePanel);
            goldButton.OnMouseClick += GoldButton_OnMouseClick;
            goldButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.InventoryGold);

            _remoteButton = DaggerfallUI.AddButton(_remoteButtonRect, NativePanel);
            _remoteButton.OnMouseClick += RemoteButton_OnMouseClick;
            _remoteButton.OnMiddleMouseClick += RemoteButton_OnMiddleMouseClick;
            _remoteButton.OnRightMouseClick += RemoteButton_OnRightMouseClick;

            if (itemInfoPanel != null)
                goldButton.OnMouseEnter += GoldButton_OnMouseEnter;
        }


        protected void OverideSetupItemInfoPanel()
        {
            itemInfoPanelLabel = new MultiFormatTextLabel
            {
                Position = new Vector2(2, 6),
                VerticalAlignment = VerticalAlignment.Top,
                MinTextureDimTextLabel = 16, // important to prevent scaling issues for single text lines
                TextScale = 0.8f,
                MaxTextWidth = (int)itemInfoPanelRect.width - 4,
                WrapText = true,
                WrapWords = true,
                ExtraLeading = 2, // spacing between info panel elements
                TextColor = DaggerfallUI.DaggerfallInfoPanelTextColor,
                ShadowPosition = new Vector2(0.5f, 0.5f),
                ShadowColor = DaggerfallUI.DaggerfallAlternateShadowColor1
            };

            itemInfoPanel.Components.Add(itemInfoPanelLabel);
        }

        protected void OverrideSetupItemListScrollers()
        {
            TextLabel miscLabelTemplate = new TextLabel()
            {
                Position = Vector2.zero,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                TextScale = 0.75f
            };

            localItemListScroller = new ItemListScroller(6, 4, localItemListRect, _itemLocalButtonRects, miscLabelTemplate, defaultToolTip, 2, 1, true, 0, 0)
            {
                Position = new Vector2(localItemListScrollerRect.x, localItemListScrollerRect.y),
                Size = new Vector2(localItemListScrollerRect.width, localItemListScrollerRect.height),
                BackgroundColourHandler = ItemBackgroundColourHandler,
                ForegroundAnimationHandler = MagicItemForegroundAnimationHander,
                ForegroundAnimationDelay = magicAnimationDelay,
            };

            NativePanel.Components.Add(localItemListScroller);
            localItemListScroller.OnItemClick += LocalItemListScroller_OnItemLeftClick;
            localItemListScroller.OnItemRightClick += LocalItemListScroller_OnItemRightClick;
            localItemListScroller.OnItemMiddleClick += LocalItemListScroller_OnItemMiddleClick;
            localItemListScroller.OnItemHover += LocalItemListScroller_OnHover;

            remoteItemListScroller = new ItemListScroller(6, 2, remoteItemListRect, _itemRemoteButtonRects, miscLabelTemplate, defaultToolTip, 2, 1, true, 0, 0)
            {
                Position = new Vector2(remoteItemListScrollerRect.x, remoteItemListScrollerRect.y),
                Size = new Vector2(remoteItemListScrollerRect.width, remoteItemListScrollerRect.height),
                BackgroundColourHandler = ItemBackgroundColourHandler,
                ForegroundAnimationHandler = MagicItemForegroundAnimationHander,
                ForegroundAnimationDelay = magicAnimationDelay
            };


            NativePanel.Components.Add(remoteItemListScroller);
            remoteItemListScroller.OnItemClick += RemoteItemListScroller_OnItemLeftClick;
            remoteItemListScroller.OnItemRightClick += RemoteItemListScroller_OnItemRightClick;
            remoteItemListScroller.OnItemMiddleClick += RemoteItemListScroller_OnItemMiddleClick;
            SetRemoteItemsAnimation();
            remoteItemListScroller.OnItemHover += RemoteItemListScroller_OnHover;
        }

        public override void OnPush()
        {
            _toggleClosedBinding = InputManager.Instance.GetBinding(InputManager.Actions.Inventory);

            // Racial override can suppress inventory
            // We still set up and push window normally, actual suppression is done in Update()
            var racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
            if (racialOverride != null)
                _suppressInventory = racialOverride.GetSuppressInventory(out _suppressInventoryMessage);

            // Local items always points to player inventory
            localItems = PlayerEntity.Items;

            // Start a new dropped items target
            droppedItems.Clear();
            if (chooseOne)
            {
                remoteTargetType = RemoteTargetTypes.Merchant;
                _dropIconArchive = DaggerfallLootDataTables.combatArchive;
                _dropIconTexture = 11;
            }
            else
            {
                // Set dropped items as default target
                remoteItems = droppedItems;
                remoteTargetType = RemoteTargetTypes.Dropped;
                _dropIconArchive = DaggerfallLootDataTables.randomTreasureArchive;
                _dropIconTexture = -1;
            }

            // Use custom loot target if specified
            if (lootTarget != null)
            {
                remoteItems = lootTarget.Items;
                isPrivateProperty = lootTarget.houseOwned;
                theftBasket = isPrivateProperty ? new ItemCollection() : null;
                remoteTargetType = RemoteTargetTypes.Loot;
                lootTargetStartCount = remoteItems.Count;
                lootTarget.OnInventoryOpen();
                if (lootTarget.playerOwned && lootTarget.TextureArchive > 0)
                {
                    _dropIconArchive = lootTarget.TextureArchive;
                    int[] iconIdxs;
                    DaggerfallLootDataTables.dropIconIdxs.TryGetValue(_dropIconArchive, out iconIdxs);
                    if (iconIdxs != null)
                    {
                        for (var i = 0; i < iconIdxs.Length; i++)
                        {
                            if (iconIdxs[i] == lootTarget.TextureRecord)
                            {
                                _dropIconTexture = i;
                                break;
                            }
                        }
                    }
                }

                OnClose += OnCloseWindow;
            }

            // Clear wagon button state on open
            if (wagonButton != null)
            {
                usingWagon = false;
                wagonButton.BackgroundTexture = wagonNotSelected;
                _remoteButton.BackgroundTexture = remoteSelected;
            }

            if (equipButton != null)
            {
                // When managing inventory only, make "equip" default action so player can manage gear
                SelectActionMode(ActionModes.Equip);
            }

            if (IsSetup)
            {
                OverrideCheckWagonAccess();
                // Reset item list scroll
                localItemListScroller.ResetScroll();
                remoteItemListScroller.ResetScroll();
                SetRemoteItemsAnimation();
            }

            // Clear info panel
            if (itemInfoPanelLabel != null)
                itemInfoPanelLabel.SetText(new TextFile.Token[0]);

            // Update tracked weapons for setting equip delay
            SetEquipDelayTime(false);

            // Refresh window
            Refresh();
        }

        protected void OverrideCheckWagonAccess()
        {
            if (allowDungeonWagonAccess)
            {
                ShowWagon(true);
                SelectActionMode(ActionModes.Remove);
            }
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon &&
                     PlayerEntity.Items.Contains(ItemGroups.Transportation, (int)Transportation.Small_cart) &&
                     DungeonWagonAccessProximityCheck())
            {
                allowDungeonWagonAccess = true;
                if (lootTarget == null)
                    ShowWagon(true);
            }
        }

        bool DungeonWagonAccessProximityCheck()
        {
            const float proximityWagonAccessDistance = 5f;

            // Get all static doors
            DaggerfallStaticDoors[] allDoors = GameObject.FindObjectsOfType<DaggerfallStaticDoors>();
            if (allDoors != null && allDoors.Length > 0)
            {
                Vector3 playerPos = GameManager.Instance.PlayerObject.transform.position;
                // Find closest door to player
                var closestDoorDistance = float.MaxValue;
                foreach (DaggerfallStaticDoors doors in allDoors)
                {
                    int doorIndex;
                    Vector3 doorPos;
                    if (doors.FindClosestDoorToPlayer(playerPos, -1, out doorPos, out doorIndex, DoorTypes.DungeonExit))
                    {
                        var distance = Vector3.Distance(playerPos, doorPos);
                        if (distance < closestDoorDistance)
                            closestDoorDistance = distance;
                    }
                }

                // Allow wagon access if close enough to any exit door
                if (closestDoorDistance < proximityWagonAccessDistance)
                    return true;
            }

            return false;
        }

        public override void OnPop()
        {
            // Reset dungeon wagon access permission
            allowDungeonWagonAccess = false;

            // Reset choose one mode
            chooseOne = false;

            // Handle stealing and reset shop shelf stealing mode
            if (shopShelfStealing && remoteItems.Count < lootTargetStartCount)
            {
                playerEntity.TallyCrimeGuildRequirements(true, 1);
                Debug.Log("Player crime detected: stealing from a shop!!");
            }

            shopShelfStealing = false;

            // If icon has changed move items to dropped list so this loot is removed and a new one created
            if (lootTarget != null && lootTarget.playerOwned && lootTarget.TextureArchive > 0 &&
                (lootTarget.TextureArchive != _dropIconArchive || lootTarget.TextureRecord != DaggerfallLootDataTables.dropIconIdxs[_dropIconArchive][_dropIconTexture]))
            {
                droppedItems.TransferAll(lootTarget.Items);
            }

            // Generate serializable loot pile in world for dropped items
            if (droppedItems.Count > 0)
            {
                DaggerfallLoot droppedLootContainer;
                if (_dropIconTexture > -1)
                    droppedLootContainer = GameObjectHelper.CreateDroppedLootContainer(
                        GameManager.Instance.PlayerObject,
                        DaggerfallUnity.NextUID,
                        _dropIconArchive,
                        DaggerfallLootDataTables.dropIconIdxs[_dropIconArchive][_dropIconTexture]);
                else
                    droppedLootContainer = GameObjectHelper.CreateDroppedLootContainer(GameManager.Instance.PlayerObject, DaggerfallUnity.NextUID);

                droppedLootContainer.Items.TransferAll(droppedItems);
                if (lootTarget != null)
                {
                    // Move newly created loot container to original position in x & z coords.
                    Vector3 pos = new Vector3(lootTarget.transform.position.x, droppedLootContainer.transform.position.y, lootTarget.transform.position.z);
                    droppedLootContainer.transform.position = pos;
                }
            }

            // Clear any loot target on exit
            if (lootTarget != null)
            {
                // Remove loot container if empty
                if (lootTarget.Items.Count == 0)
                    GameObjectHelper.RemoveLootContainer(lootTarget);

                lootTarget.OnInventoryClose();
                lootTarget = null;
            }

            // Add equip delay if weapon was changed
            SetEquipDelayTime(true);

            // Show "equipping" message if a delay was added
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            if (GameManager.Instance.WeaponManager.EquipCountdownRightHand > 0)
            {
                DaggerfallUnityItem currentRightHandWeapon = player.ItemEquipTable.GetItem(EquipSlots.RightHand);
                if (currentRightHandWeapon != null)
                {
                    var message = TextManager.Instance.GetLocalizedText("equippingWeapon");
                    var templateName = TextManager.Instance.GetLocalizedItemName(currentRightHandWeapon.ItemTemplate.index, currentRightHandWeapon.ItemTemplate.name);
                    message = message.Replace("%s", templateName);
                    DaggerfallUI.Instance.PopupMessage(message);
                }
            }

            if (GameManager.Instance.WeaponManager.EquipCountdownLeftHand > 0)
            {
                DaggerfallUnityItem currentLeftHandWeapon = player.ItemEquipTable.GetItem(EquipSlots.LeftHand);
                if (currentLeftHandWeapon != null)
                {
                    var message = TextManager.Instance.GetLocalizedText("equippingWeapon");
                    var templateName = TextManager.Instance.GetLocalizedItemName(currentLeftHandWeapon.ItemTemplate.index, currentLeftHandWeapon.ItemTemplate.name);
                    message = message.Replace("%s", templateName);
                    DaggerfallUI.Instance.PopupMessage(message);
                }
            }
        }

        void OverrideSetupClosedBinding()
        {
            _toggleClosedBinding = InputManager.Instance.GetBinding(InputManager.Actions.Inventory);
        }

        public override void Update()
        {
            base.Update();

            if (DaggerfallUI.Instance.HotkeySequenceProcessed == HotkeySequence.HotkeySequenceProcessStatus.NotFound)
            {
                // Toggle window closed with same hotkey used to open it
                if (InputManager.Instance.GetKeyUp(_toggleClosedBinding))
                    CloseWindow();
            }

            // Close window immediately if inventory suppressed
            if (_suppressInventory)
            {
                CloseWindow();
                if (!string.IsNullOrEmpty(_suppressInventoryMessage))
                    DaggerfallUI.MessageBox(_suppressInventoryMessage);
                return;
            }
        }

        protected override void SelectTabPage(TabPages tabPage)
        {
            // Select new tab page
            selectedTabPage = tabPage;

            // Set all buttons to appropriate state
            weaponsAndArmorButton.BackgroundTexture = (tabPage == TabPages.WeaponsAndArmor) ? weaponsAndArmorSelected : weaponsAndArmorNotSelected;
            magicItemsButton.BackgroundTexture = (tabPage == TabPages.MagicItems) ? magicItemsSelected : magicItemsNotSelected;
            clothingAndMiscButton.BackgroundTexture = (tabPage == TabPages.ClothingAndMisc) ? clothingAndMiscSelected : clothingAndMiscNotSelected;
            ingredientsButton.BackgroundTexture = (tabPage == TabPages.Ingredients) ? ingredientsSelected : ingredientsNotSelected;

            // Clear info panel
            if (itemInfoPanelLabel != null)
                itemInfoPanelLabel.SetText(new TextFile.Token[0]);

            // Update filtered list
            localItemListScroller.ResetScroll();
            FilterLocalItems();
            localItemListScroller.Items = localItemsFiltered;
        }

        void SetupTabPageButtonTooltips()
        {
            weaponsAndArmorButton.ToolTip = defaultToolTip;
            weaponsAndArmorButton.ToolTipText = UncannyInventoryWindow.LabelWeaponsAndArmorButtonTooltip;

            magicItemsButton.ToolTip = defaultToolTip;
            magicItemsButton.ToolTipText = UncannyInventoryWindow.LabelMagicItemsButtonTooltip;

            clothingAndMiscButton.ToolTip = defaultToolTip;
            clothingAndMiscButton.ToolTipText = UncannyInventoryWindow.LabelClothingAndMiscButtonTooltip;

            ingredientsButton.ToolTip = defaultToolTip;
            ingredientsButton.ToolTipText = UncannyInventoryWindow.LabelIngredientsButtonTooltip;
        }

        protected override void SelectActionMode(ActionModes mode)
        {
            selectedActionMode = mode;

            // Clear all button selections
            equipButton.BackgroundTexture = equipNotSelected;
            useButton.BackgroundTexture = useNotSelected;
            infoButton.BackgroundTexture = infoNotSelected;

            // Set button selected texture
            switch (mode)
            {
                case ActionModes.Equip:
                    equipButton.BackgroundTexture = equipSelected;
                    break;
                case ActionModes.Use:
                    useButton.BackgroundTexture = useSelected;
                    break;
                case ActionModes.Info:
                    infoButton.BackgroundTexture = infoSelected;
                    break;
            }
        }

        protected override float GetCarriedWeight()
        {
            return playerEntity.CarriedWeight - DaggerfallBankManager.goldUnitWeightInKg; //Since we added gold to the inventory, we ignore it here.
        }

        protected override void UpdateLocalTargetIcon()
        {
            carryGoldLabel.Text = PlayerEntity.GoldPieces.ToString();
            carryWeightLabel.Text = String.Format(GetCarriedWeight() % 1 == 0 ? "{0:F0} / {1}" : "{0:F2} / {1}", GetCarriedWeight(), PlayerEntity.MaxEncumbrance);
            if (PlayerEntity.Items.Contains(ItemGroups.Transportation, (int)Transportation.Small_cart))
            {
                wagonCarryWeightLabel.Text = String.Format(PlayerEntity.WagonWeight % 1 == 0 ? "{0:F0} / {1}" : "{0:F2} / {1}", PlayerEntity.WagonWeight, ItemHelper.WagonKgLimit);
            }
            else
            {
                wagonCarryWeightLabel.Text = "";
            }
        }

        protected override void UpdateRemoteTargetIcon()
        {
            ImageData containerImage;
            if (_dropIconTexture > -1)
            {
                var filename = TextureFile.IndexToFileName(_dropIconArchive);
                containerImage = ImageReader.GetImageData(filename, DaggerfallLootDataTables.dropIconIdxs[_dropIconArchive][_dropIconTexture], 0, true);
            }
            else if (lootTarget != null && lootTarget.TextureArchive > 0)
            {
                var filename = TextureFile.IndexToFileName(lootTarget.TextureArchive);
                containerImage = ImageReader.GetImageData(filename, lootTarget.TextureRecord, 0, true);
            }
            else
            {
                containerImage = DaggerfallUnity.ItemHelper.GetContainerImage(
                    (remoteTargetType == RemoteTargetTypes.Loot) ? lootTarget.ContainerImage : InventoryContainerImages.Ground);
            }

            remoteTargetIconPanel.BackgroundTexture = containerImage.texture;
        }

        protected override void FilterLocalItems()
        {
            // Clear current references
            localItemsFiltered.Clear();

            if (localItems != null)
            {
                // Add items to list
                for (var i = 0; i < localItems.Count; i++)
                {
                    DaggerfallUnityItem item = localItems.GetItem(i);
                    // Add if not equipped
                    if (!item.IsEquipped)
                        OverrideAddLocalItem(item);
                }
            }
        }

        protected void OverrideAddLocalItem(DaggerfallUnityItem item)
        {
            var isWeaponOrArmor = (item.ItemGroup == ItemGroups.Weapons || item.ItemGroup == ItemGroups.Armor);

            //Do not show cart in inventory
            if (_hideTheWagon && item == localItems.GetItem(ItemGroups.Transportation, (int)Transportation.Small_cart))
                return;

            // Add based on view
            if (selectedTabPage == TabPages.WeaponsAndArmor)
            {
                // Weapons and armor
                if (isWeaponOrArmor && !item.IsEnchanted)
                    localItemsFiltered.Add(item);
            }
            else if (selectedTabPage == TabPages.MagicItems)
            {
                // Enchanted items
                if (item.IsEnchanted || item.IsOfTemplate((int)MiscItems.Spellbook))
                    localItemsFiltered.Add(item);
            }
            else if (selectedTabPage == TabPages.Ingredients)
            {
                // Ingredients
                if (item.IsIngredient && !item.IsEnchanted)
                    localItemsFiltered.Add(item);
            }
            else if (selectedTabPage == TabPages.ClothingAndMisc)
            {
                // Everything else
                if ((!isWeaponOrArmor && !item.IsEnchanted && !item.IsIngredient && !item.IsOfTemplate((int)MiscItems.Spellbook) || item.IsOfTemplate((int)Currency.Gold_pieces)))
                    localItemsFiltered.Add(item);
            }
        }

        protected override void LoadTextures()
        {
            // Load source textures
            baseTexture = UncannyUILoader.Instance.LoadInventoryUI();
            goldTexture = UncannyUILoader.Instance.LoadInventoryHighlightUI();

            if (!baseTexture || !goldTexture)
            {
                Debug.LogError(string.Format("Failed to load image(s) for inventory window"));
                CloseWindow();
                return;
            }

            DFSize baseSize = new DFSize(320, 200);

            // Cut out tab page not selected button textures
            weaponsAndArmorNotSelected = ImageReader.GetSubTexture(baseTexture, weaponsAndArmorRect, baseSize);
            magicItemsNotSelected = ImageReader.GetSubTexture(baseTexture, magicItemsRect, baseSize);
            clothingAndMiscNotSelected = ImageReader.GetSubTexture(baseTexture, clothingAndMiscRect, baseSize);
            ingredientsNotSelected = ImageReader.GetSubTexture(baseTexture, ingredientsRect, baseSize);

            // Cut out tab page selected button textures
            weaponsAndArmorSelected = ImageReader.GetSubTexture(goldTexture, weaponsAndArmorRect, baseSize);
            magicItemsSelected = ImageReader.GetSubTexture(goldTexture, magicItemsRect, baseSize);
            clothingAndMiscSelected = ImageReader.GetSubTexture(goldTexture, clothingAndMiscRect, baseSize);
            ingredientsSelected = ImageReader.GetSubTexture(goldTexture, ingredientsRect, baseSize);

            // Cut out action mode selected buttons
            equipSelected = ImageReader.GetSubTexture(goldTexture, equipButtonRect, baseSize);
            equipNotSelected = ImageReader.GetSubTexture(baseTexture, equipButtonRect, baseSize);

            useSelected = ImageReader.GetSubTexture(goldTexture, useButtonRect, baseSize);
            useNotSelected = ImageReader.GetSubTexture(baseTexture, useButtonRect, baseSize);

            infoSelected = ImageReader.GetSubTexture(goldTexture, infoButtonRect, baseSize);
            infoNotSelected = ImageReader.GetSubTexture(baseTexture, infoButtonRect, baseSize);

            // WagonButton
            wagonSelected = ImageReader.GetSubTexture(goldTexture, wagonButtonRect, baseSize);
            wagonNotSelected = ImageReader.GetSubTexture(baseTexture, wagonButtonRect, baseSize);

            remoteSelected = ImageReader.GetSubTexture(goldTexture, _remoteButtonRect, baseSize);
            remoteNotSelected = ImageReader.GetSubTexture(baseTexture, _remoteButtonRect, baseSize);

            // Load coins animation textures
            coinsAnimation = ImageReader.GetImageData(coinsAnimTextureName, 6, 0, true, false, true);

            // Load magic item animation textures
            magicAnimation = ImageReader.GetImageData(magicAnimTextureName, 5, 0, true, false, true);
        }

        void ShowWagon(bool show)
        {
            if (show)
            {
                // Save current target and switch to wagon
                wagonButton.BackgroundTexture = wagonSelected;
                _remoteButton.BackgroundTexture = remoteNotSelected;
                lastRemoteItems = remoteItems;
                lastRemoteTargetType = remoteTargetType;
                remoteItems = PlayerEntity.WagonItems;
            }
            else
            {
                // Restore previous target or default to dropped items
                wagonButton.BackgroundTexture = wagonNotSelected;
                _remoteButton.BackgroundTexture = remoteSelected;
                if (lastRemoteItems != null)
                {
                    remoteItems = lastRemoteItems;
                    remoteTargetType = lastRemoteTargetType;
                    lastRemoteItems = null;
                }
                else
                {
                    remoteItems = droppedItems;
                    lastRemoteItems = null;
                }
            }

            usingWagon = show;
            remoteItemListScroller.ResetScroll();
            Refresh(false);
        }

        void SetEquipDelayTime(bool setTime)
        {
            var delayTimeRight = 0;
            var delayTimeLeft = 0;
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            DaggerfallUnityItem currentRightHandItem = player.ItemEquipTable.GetItem(EquipSlots.RightHand);
            DaggerfallUnityItem currentLeftHandItem = player.ItemEquipTable.GetItem(EquipSlots.LeftHand);

            if (setTime)
            {
                if (lastRightHandItem != currentRightHandItem)
                {
                    // Add delay for unequipping old item
                    if (lastRightHandItem != null)
                        delayTimeRight = WeaponManager.EquipDelayTimes[lastRightHandItem.GroupIndex];

                    // Add delay for equipping new item
                    if (currentRightHandItem != null)
                        delayTimeRight += WeaponManager.EquipDelayTimes[currentRightHandItem.GroupIndex];
                }

                if (lastLeftHandItem != currentLeftHandItem)
                {
                    // Add delay for unequipping old item
                    if (lastLeftHandItem != null)
                        delayTimeLeft = WeaponManager.EquipDelayTimes[lastLeftHandItem.GroupIndex];

                    // Add delay for equipping new item
                    if (currentLeftHandItem != null)
                        delayTimeLeft += WeaponManager.EquipDelayTimes[currentLeftHandItem.GroupIndex];
                }
            }
            else
            {
                lastRightHandItem = null;
                lastLeftHandItem = null;
                if (currentRightHandItem != null)
                {
                    lastRightHandItem = currentRightHandItem;
                }

                if (currentLeftHandItem != null)
                {
                    lastLeftHandItem = currentLeftHandItem;
                }
            }

            GameManager.Instance.WeaponManager.EquipCountdownRightHand += delayTimeRight;
            GameManager.Instance.WeaponManager.EquipCountdownLeftHand += delayTimeLeft;
        }

        private void WagonButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!PlayerEntity.Items.Contains(ItemGroups.Transportation, (int)Transportation.Small_cart))
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("noWagon"));
            else if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon && !allowDungeonWagonAccess)
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("exitTooFar"));
            else
                ShowWagon(true);
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void RemoteButton_OnMiddleMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // If items are being dropped by player, iterate up through drop archives
            if (!usingWagon)
            {
                if (CanChangeDropIcon())
                {
                    _dropIconArchive = GetNextArchive();
                    _dropIconTexture = 0;
                    UpdateRemoteTargetIcon();
                }
            }
        }

        private void RemoteButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // If items are being dropped by player, iterate up through drop textures

            if (!usingWagon)
            {
                if (CanChangeDropIcon())
                {
                    _dropIconTexture++;
                    if (_dropIconTexture >= DaggerfallLootDataTables.dropIconIdxs[_dropIconArchive].Length)
                        _dropIconTexture = 0;
                    UpdateRemoteTargetIcon();
                }
            }

            ShowWagon(false);
        }

        private void RemoteButton_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // If items are being dropped by player, iterate down through drop textures
            if (!usingWagon)
            {
                if (CanChangeDropIcon())
                {
                    _dropIconTexture--;
                    if (_dropIconTexture < 0)
                        _dropIconTexture = DaggerfallLootDataTables.dropIconIdxs[_dropIconArchive].Length - 1;
                    UpdateRemoteTargetIcon();
                }
            }
        }

        private void EquipButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            SelectActionMode(ActionModes.Equip);
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void UseButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            SelectActionMode(ActionModes.Use);
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void InfoButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            SelectActionMode(ActionModes.Info);
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void GoldButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // Show message box
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            const int goldToDropTextId = 25;
            DaggerfallInputMessageBox mb = new DaggerfallInputMessageBox(uiManager, this);
            mb.SetTextTokens(goldToDropTextId);
            mb.TextPanelDistanceY = 0;
            mb.InputDistanceX = 15;
            mb.InputDistanceY = -6;
            mb.TextBox.Numeric = true;
            mb.TextBox.MaxCharacters = 8;
            mb.TextBox.Text = "0";
            mb.OnGotUserInput += DropGoldPopup_OnGotUserInput;
            mb.Show();
        }

        private void DropGoldPopup_OnGotUserInput(DaggerfallInputMessageBox sender, string input)
        {
            // Get player gold count
            var playerGold = GameManager.Instance.PlayerEntity.GoldPieces;

            // Determine how many gold pieces to drop
            var goldToDrop = 0;
            var result = int.TryParse(input, out goldToDrop);
            if (!result || goldToDrop < 1 || goldToDrop > playerGold)
                return;
            if (usingWagon)
            {
                // Check wagon weight limit
                var wagonCanHold = ComputeCanHoldAmount(playerGold, DaggerfallBankManager.goldUnitWeightInKg, ItemHelper.WagonKgLimit, remoteItems.GetWeight());
                if (goldToDrop > wagonCanHold)
                {
                    goldToDrop = wagonCanHold;
                    DaggerfallUI.MessageBox(String.Format(wagonFullGold, wagonCanHold));
                }
            }

            // Create new item for gold pieces and add to other container
            DaggerfallUnityItem goldPieces = ItemBuilder.CreateGoldPieces(goldToDrop);
            remoteItems.AddItem(goldPieces, _preferredOrder);

            // Remove gold count from player
            GameManager.Instance.PlayerEntity.GoldPieces -= goldToDrop;

            Refresh(false);
            UpdateItemInfoPanelGold();
        }

        private void OverrideTransferItem(DaggerfallUnityItem item, ItemCollection from, ItemCollection to, int maxAmount = -1, bool blockTransport = false, bool equip = false, bool allowSplitting = true)
        {
            // Block transfer of horse or cart (don't allow putting either in wagon)
            if (blockTransport && item.ItemGroup == ItemGroups.Transportation)
                return;

            // Block transfer of summoned items
            if (item.IsSummoned)
            {
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("cannotRemoveItem"));
                return;
            }

            // Handle map items
            if (item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Map))
            {
                if (_isHmlModAdded)
                {
                    remoteItems.RemoveItem(item);
                    localItems.AddItem(item);
                    Refresh(false);
                    return;
                }

                RecordLocationFromMap(item);
                from.RemoveItem(item);
                Refresh(false);
                return;
            }

            // Handle quest item transfer
            if (item.IsQuestItem)
            {
                // Get quest item
                Item questItem = GetQuestItem(item);

                // Player cannot drop most quest items unless enabled
                if (!DaggerfallUnity.Settings.CanDropQuestItems)
                {
                    if (questItem == null || (!questItem.AllowDrop && from == localItems))
                    {
                        DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("cannotRemoveItem"));
                        return;
                    }
                }

                // Dropping or picking up quest item
                if (questItem.AllowDrop && from == localItems && remoteTargetType != RemoteTargetTypes.Wagon)
                    questItem.PlayerDropped = true;
                else if (from == remoteItems)
                    questItem.PlayerDropped = false;
            }

            // Extinguish light sources when transferring out of player inventory
            if (item.IsLightSource && playerEntity.LightSource == item && from == localItems)
                playerEntity.LightSource = null;

            // Handle stacks & splitting if needed
            if (maxAmount != -1)
                this._maxAmount = maxAmount;
            else
                this._maxAmount = item.stackCount;

            if (this._maxAmount <= 0)
                return;

            var splitRequired = maxAmount < item.stackCount && item.stackCount > 1;
            var controlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (splitRequired || controlPressed)
            {
                if (allowSplitting && item.IsAStack())
                {
                    _stackItem = item;
                    _stackFrom = from;
                    _stackTo = to;
                    _stackEquip = equip;
                    var defaultValue = controlPressed ? "0" : this._maxAmount.ToString();

                    // Show message box
                    DaggerfallInputMessageBox mb = new DaggerfallInputMessageBox(uiManager, this);
                    mb.SetTextBoxLabel(String.Format(TextManager.Instance.GetLocalizedText("howManyItems"), this._maxAmount));
                    mb.TextPanelDistanceY = 0;
                    mb.InputDistanceX = 15;
                    mb.TextBox.Numeric = true;
                    mb.TextBox.MaxCharacters = 8;
                    mb.TextBox.Text = defaultValue;
                    mb.OnGotUserInput += SplitStackPopup_OnGotUserInput;
                    mb.Show();
                    return;
                }

                if (splitRequired)
                    return;
            }

            DoTransferItem(item, from, to, equip);
        }


        void RecordLocationFromMap(DaggerfallUnityItem item)
        {
            const int mapTextId = 499;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            try
            {
                DFLocation revealedLocation = playerGPS.DiscoverRandomLocation();

                if (string.IsNullOrEmpty(revealedLocation.Name))
                    throw new Exception();

                playerGPS.LocationRevealedByMapItem = revealedLocation.Name;
                GameManager.Instance.PlayerEntity.Notebook.AddNote(
                    TextManager.Instance.GetLocalizedText("readMap").Replace("%map", TextManager.Instance.GetLocalizedLocationName(revealedLocation.MapTableData.MapId, revealedLocation.Name)));

                DaggerfallMessageBox mapText = new DaggerfallMessageBox(uiManager, this);
                mapText.SetTextTokens(DaggerfallUnity.Instance.TextProvider.GetRandomTokens(mapTextId));
                mapText.ClickAnywhereToClose = true;
                mapText.Show();
            }
            catch (Exception)
            {
                // Player has already descovered all valid locations in this region!
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("readMapFail"));
            }
        }

        private int WeightInGPUnits(float weight)
        {
            return Mathf.RoundToInt(weight * 400f);
        }

        private int ComputeCanHoldAmount(int unitsAvailable, float unitWeightInKg, float capacityInKg, float loadInKg)
        {
            var canHold = unitsAvailable;
            var roundUnitWeight = WeightInGPUnits(unitWeightInKg);
            if (roundUnitWeight > 0)
            {
                var roundCapacity = WeightInGPUnits(capacityInKg);
                var roundLoad = WeightInGPUnits(loadInKg);
                canHold = Math.Min(canHold, (roundCapacity - roundLoad) / roundUnitWeight);
            }

            return canHold;
        }

        private void SplitStackPopup_OnGotUserInput(DaggerfallInputMessageBox sender, string input)
        {
            // Determine how many items to split
            var count = 0;
            var result = int.TryParse(input, out count);
            if (!result || count > _maxAmount)
                return;

            DaggerfallUnityItem item = _stackFrom.SplitStack(_stackItem, count);
            if (item != null)
                DoTransferItem(item, _stackFrom, _stackTo, _stackEquip);
            else
                Refresh(false);
        }

        /// <summary>
        /// Gets virtual quest item from DaggerfallUnityItem.
        /// </summary>
        /// <param name="item">Source DaggerfallUnityItem.</param>
        /// <returns>Quest Item if found.</returns>
        Item GetQuestItem(DaggerfallUnityItem item)
        {
            if (item == null)
                return null;

            Quest quest = QuestMachine.Instance.GetQuest(item.QuestUID);
            if (quest == null)
                throw new Exception("DaggerfallUnityItem references a QuestUID that could not be found.");

            Item questItem = quest.GetItem(item.QuestItemSymbol);
            if (questItem == null)
                throw new Exception("DaggerfallUnityItem references a QuestItemSymbol that could not be found.");

            return questItem;
        }

        private void AttemptPrivatePropertyTheft()
        {
            GameManager.Instance.PlayerEntity.TallyCrimeGuildRequirements(true, 1);
            PlayerGPS.DiscoveredBuilding buildingDiscoveryData = GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData;
            var weightAndNumItems = (int)theftBasket.GetWeight() + theftBasket.Count;
            var chanceBeingDetected = FormulaHelper.CalculateShopliftingChance(playerEntity, buildingDiscoveryData.quality, weightAndNumItems);
            // Send the guards if detected
            if (!Dice100.FailedRoll(chanceBeingDetected))
            {
                playerEntity.CrimeCommitted = PlayerEntity.Crimes.Theft;
                playerEntity.SpawnCityGuards(true);
            }
            else
            {
                PlayerEntity.TallySkill(DFCareer.Skills.Pickpocket, 1);
            }
        }

        // Moving local and remote Use item clicks to new method
        // This ensures the items are handled the same except when needed
        // This will need more work as more usable items are available
        protected void UseItemOverride(DaggerfallUnityItem item, ItemCollection collection = null)
        {
            if (item.IsPotionRecipe)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallMessageBox messageBoxRecipe = new DaggerfallMessageBox(uiManager, this);

                List<TextFile.Token> messages = new List<TextFile.Token>();
                TextFile.Token[] tokens = ItemHelper.GetItemInfo(item, DaggerfallUnity.TextProvider);
                MacroHelper.ExpandMacros(ref tokens, item);
                messages.AddRange(tokens);
                messages.AddRange(item.GetMacroDataSource().PotionRecipeIngredients(TextFile.Formatting.JustifyCenter));

                messageBoxRecipe.SetTextTokens(messages.ToArray());
                messageBoxRecipe.ClickAnywhereToClose = true;
                messageBoxRecipe.Show();
            }
            else
            {
                UseItem(item, collection);
            }
        }

        // Get action mode, swapping equip and remove - for right clicks
        protected override ActionModes GetActionModeRightClick()
        {
            return ActionModes.Remove;
        }

        protected override void AccessoryItemsButton_OnMouseClick(BaseScreenComponent sender, Vector2 position, ActionModes actionMode)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // Get item
            EquipSlots slot = (EquipSlots)sender.Tag;
            DaggerfallUnityItem item = playerEntity.ItemEquipTable.GetItem(slot);
            if (item == null)
                return;

            // Handle click based on action
            if (actionMode == ActionModes.Equip ||
                actionMode == ActionModes.Select ||
                actionMode == ActionModes.Remove)
            {
                UnequipItem(item);
            }
            else if (actionMode == ActionModes.Info)
            {
                ShowInfoPopup(item);
            }
            else if (actionMode == ActionModes.Use)
            {
                UseItemOverride(item);
            }
        }

        private DaggerfallUnityItem PaperDoll_GetItem(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            // Get equip value
            var value = paperDoll.GetEquipIndex((int)position.x, (int)position.y);
            if (value == 0xff)
                return null;

            // Get item
            EquipSlots slot = (EquipSlots)value;
            DaggerfallUnityItem item = playerEntity.ItemEquipTable.GetItem(slot);
            return item;
        }

        protected override void PaperDoll_OnMouseClick(BaseScreenComponent sender, Vector2 position, ActionModes actionMode)
        {
            DaggerfallUnityItem item = PaperDoll_GetItem(sender, position);
            if (item == null)
                return;

            // Handle click based on action
            if (actionMode == ActionModes.Equip ||
                actionMode == ActionModes.Select ||
                actionMode == ActionModes.Remove)
            {
                UnequipItem(item);
            }

            else if (actionMode == ActionModes.Use)
            {
                UseItemOverride(item);
            }
            else if (actionMode == ActionModes.Info)
            {
                ShowInfoPopup(item);
            }
        }

        protected override void LocalItemListScroller_OnItemClick(DaggerfallUnityItem item, ActionModes actionMode)
        {
            if (actionMode == ActionModes.Equip)
            {
                if (PlayerEntity.ItemEquipTable.GetEquipSlot(item) != EquipSlots.None)
                {
                    EquipItem(item);
                }
                else
                {
                    if (!item.UseItem(localItems))
                        UseItemOverride(item, localItems);
                    ;
                }
            }
            else if (actionMode == ActionModes.Use)
            {
                // Allow item to handle its own use, fall through to general use function if unhandled
                if (!item.UseItem(localItems))
                    UseItemOverride(item, localItems);
                else if (PlayerEntity.ItemEquipTable.GetEquipSlot(item) != EquipSlots.None)
                {
                    EquipItem(item);
                }
            }
            else if (actionMode == ActionModes.Remove)
            {
                // Transfer to remote items
                if (remoteItems != null && !chooseOne)
                {
                    var canHold = -1;
                    if (usingWagon)
                        canHold = WagonCanHoldAmount(item);
                    OverrideTransferItem(item, localItems, remoteItems, canHold, true);
                    if (theftBasket != null && lootTarget != null && lootTarget.houseOwned)
                        theftBasket.RemoveItem(item);
                }
            }
            else if (actionMode == ActionModes.Info)
            {
                ShowInfoPopup(item);
            }

            Refresh(false);
        }

        protected override void RemoteItemListScroller_OnItemClick(DaggerfallUnityItem item, ActionModes actionMode)
        {
            // Send click to quest system
            if (item.IsQuestItem)
            {
                Quest quest = QuestMachine.Instance.GetQuest(item.QuestUID);
                if (quest != null)
                {
                    Item questItem = quest.GetItem(item.QuestItemSymbol);
                    if (questItem != null)
                    {
                        questItem.SetPlayerClicked();
                    }
                }
            }

            var onlyRemove = UncannyUILoader.Instance.GetModSettings().GetBool("Inventory", "OnlyRemoveFromRemote");

            actionMode = onlyRemove ? ActionModes.Remove : actionMode;

            // Handle click based on action
            if (actionMode == ActionModes.Equip)
            {
                // Transfer to local items
                if (localItems != null)
                    OverrideTransferItem(item, remoteItems, localItems, CanCarryAmount(item), equip: true);
                if (theftBasket != null && lootTarget != null && lootTarget.houseOwned)
                    theftBasket.AddItem(item);
            }
            else if (actionMode == ActionModes.Use)
            {
                // Allow item to handle its own use, fall through to general use function if unhandled
                if (!item.UseItem(remoteItems))
                    UseItem(item, remoteItems);
                Refresh(false);
            }
            else if (actionMode == ActionModes.Remove)
            {
                OverrideTransferItem(item, remoteItems, localItems, CanCarryAmount(item));
                if (theftBasket != null && lootTarget != null && lootTarget.houseOwned)
                    theftBasket.AddItem(item);
            }
            else if (actionMode == ActionModes.Info)
            {
                ShowInfoPopup(item);
            }
        }

        private bool CanChangeDropIcon()
        {
            return (remoteTargetType == RemoteTargetTypes.Dropped ||
                    (remoteTargetType == RemoteTargetTypes.Loot && lootTarget.playerOwned));
        }

        private int GetNextArchive()
        {
            var next = false;
            foreach (var ai in DaggerfallLootDataTables.dropIconIdxs.Keys)
            {
                if (next)
                    return ai;
                next = (ai == _dropIconArchive);
            }

            return DaggerfallLootDataTables.dropIconIdxs.Keys.First();
        }

        private void UpdateItemInfoPanelGold()
        {
            var gold = GameManager.Instance.PlayerEntity.GoldPieces;
            var weight = gold * DaggerfallBankManager.goldUnitWeightInKg;
            TextFile.Token[] tokens =
            {
                TextFile.CreateTextToken(string.Format(goldAmount, gold)),
                TextFile.NewLineToken,
                TextFile.CreateTextToken(string.Format(goldWeight, weight.ToString(weight % 1 == 0 ? "F0" : "F2")))
            };
            UpdateItemInfoPanel(tokens);
        }

        private void OnCloseWindow()
        {
            if (isPrivateProperty && theftBasket != null && theftBasket.Count != 0)
            {
                AttemptPrivatePropertyTheft();
            }

            OnClose -= OnCloseWindow;
        }
    }
}