using System.Text;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace Game.Mods.UncannyUI.Scripts
{
    public class UncannyRestWindow : DaggerfallRestWindow
    {
        // Labels
        protected readonly string IllegalToRestString = UncannyUILoader.Instance.GetMod().Localize("IllegalToRest");
        protected readonly string LoiterString = UncannyUILoader.Instance.GetMod().Localize("Loiter");
        protected readonly string RestString = UncannyUILoader.Instance.GetMod().Localize("Rest");
        protected readonly string StopString = UncannyUILoader.Instance.GetMod().Localize("Stop");
        protected readonly string UntilHealedString = UncannyUILoader.Instance.GetMod().Localize("UntilHealed");
        protected readonly string WaitString = UncannyUILoader.Instance.GetMod().Localize("Wait");
        protected readonly string MustRentARoomString = UncannyUILoader.Instance.GetMod().Localize("MustRentARoom");
        protected readonly string MayNotRestHereString = UncannyUILoader.Instance.GetMod().Localize("MayNotRestHere");
        protected readonly string YouHaveARoomString = UncannyUILoader.Instance.GetMod().Localize("YouHaveARoom");
        protected readonly string MayRestHereString = UncannyUILoader.Instance.GetMod().Localize("MayRestHere");
        protected readonly string MembershipAllowsToRestString = UncannyUILoader.Instance.GetMod().Localize("MembershipAllowsToRest");
        protected readonly string DayString = UncannyUILoader.Instance.GetMod().Localize("Day");
        protected readonly string DaysString = UncannyUILoader.Instance.GetMod().Localize("Days");
        protected readonly string AndString = UncannyUILoader.Instance.GetMod().Localize("And");
        protected readonly string HourString = UncannyUILoader.Instance.GetMod().Localize("Hour");
        protected readonly string HoursString = UncannyUILoader.Instance.GetMod().Localize("Hours");

        //ModeButtons
        Texture2D[] modeButtonsImage;
        Rect[] modeImageRect = { new Rect(5, 0, 48, 11), new Rect(54, 0, 48, 11), new Rect(103, 0, 48, 11) };
        Button[] modeButton = new Button[3];
        Rect[] modeButtonRect = { new Rect(6, 44, 48, 11), new Rect(54, 44, 48, 11), new Rect(102, 44, 48, 11) };
        TextLabel[] modeLabel = new TextLabel[4];
        Panel counterTextPanel;
        Rect counterTextPanelRect = new Rect(4, 10, 16, 8);

        Panel currentTimePanel;
        TextLabel currentTimeLabel;

        Panel allowRestPanel;
        TextLabel allowRestLabel;

        Texture2D mainPanelTexture;
        Vector2Int mainPanelSize = new Vector2Int(154, 58);

        HorizontalSlider restSlider;

        int selectedTime;

        Rect currentTimeRect = new Rect(17, 32, 124, 10);
        Vector2 sliderPos = new Vector2(17, 23);
        Rect allowRestRect = new Rect(17, 4, 124, 16);

        public UncannyRestWindow(IUserInterfaceManager uiManager, bool ignoreAllocatedBed = false)
            : base(uiManager)
        {
            this.ignoreAllocatedBed = ignoreAllocatedBed;
        }

        #region Setup Methods

        protected override void Setup()
        {
            // Disable default canceling behavior so exiting can be handled by the Update function instead
            AllowCancel = false;

            // Load all the textures used by rest interface
            LoadTextures();

            var hideWorld = UncannyUILoader.Instance.GetModSettings().GetBool("Common", "HideWorld");

            // Hide world while resting
            if (hideWorld)
            {
                ParentPanel.BackgroundColor = Color.black;
            }
            else
            {
                ParentPanel.BackgroundColor = Color.clear;
            }

            // Create interface panel
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.BackgroundTexture = mainPanelTexture;
            mainPanel.Position = new Vector2(0, 70);
            mainPanel.Size = new Vector2(mainPanelSize.x, mainPanelSize.y);

            NativePanel.Components.Add(mainPanel);

            string allowRestMsg;
            var canRest = CheckRestStatus(out allowRestMsg);

            currentTimePanel = DaggerfallUI.AddPanel(currentTimeRect, mainPanel);
            currentTimeLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, currentTimePanel);
            currentTimeLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            currentTimeLabel.Text = DaggerfallUnity.Instance.WorldTime.Now.DateString() + " " + DaggerfallUnity.Instance.WorldTime.Now.MinTimeString();
            currentTimeLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            currentTimeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            currentTimeLabel.VerticalAlignment = VerticalAlignment.Middle;

            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") < 2)
            {
                if (canRest)
                {
                    modeButton[0] = DaggerfallUI.AddButton(modeButtonRect[1], mainPanel);
                    modeButton[0].OnMouseClick += HealedButton_OnMouseClick;
                    modeButton[0].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestUntilHealed);
                    modeButton[0].BackgroundTexture = modeButtonsImage[0];

                    modeLabel[0] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, modeButton[0]);
                    modeLabel[0].TextColor = UncannyUILoader.Instance.DefaultColor();
                    modeLabel[0].Text = UntilHealedString;
                    modeLabel[0].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                    modeLabel[0].HorizontalAlignment = HorizontalAlignment.Center;
                    modeLabel[0].VerticalAlignment = VerticalAlignment.Middle;
                }

                modeButton[1] = DaggerfallUI.AddButton(modeButtonRect[2], mainPanel);
                modeButton[1].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestForAWhile);
                modeButton[1].BackgroundTexture = modeButtonsImage[1];

                modeLabel[1] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, modeButton[1]);
                modeLabel[1].TextColor = UncannyUILoader.Instance.DefaultColor();
                modeLabel[1].Text = RestString;
                modeLabel[1].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                modeLabel[1].HorizontalAlignment = HorizontalAlignment.Center;
                modeLabel[1].VerticalAlignment = VerticalAlignment.Middle;

                if (canRest)
                {
                    modeButton[1].OnMouseClick += WhileButton_OnMouseClick;
                    modeLabel[1].Text = RestString;
                }
                else
                {
                    modeButton[1].OnMouseClick += LoiterButton_OnMouseClick;

                    if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 0)
                        modeLabel[1].Text = WaitString;

                    else if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 1)
                        modeLabel[1].Text = LoiterString;
                }
            }
            else if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 2)
            {
                modeButton[0] = DaggerfallUI.AddButton(modeButtonRect[0], mainPanel);
                modeButton[0].OnMouseClick += WhileButton_OnMouseClick;
                modeButton[0].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestForAWhile);
                modeButton[0].BackgroundTexture = modeButtonsImage[0];

                modeLabel[0] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, modeButton[0]);
                modeLabel[0].TextColor = UncannyUILoader.Instance.DefaultColor();
                modeLabel[0].Text = RestString;
                modeLabel[0].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                modeLabel[0].HorizontalAlignment = HorizontalAlignment.Center;
                modeLabel[0].VerticalAlignment = VerticalAlignment.Middle;

                modeButton[1] = DaggerfallUI.AddButton(modeButtonRect[1], mainPanel);
                modeButton[1].OnMouseClick += HealedButton_OnMouseClick;
                modeButton[1].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestUntilHealed);
                modeButton[1].BackgroundTexture = modeButtonsImage[1];

                modeLabel[1] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, modeButton[1]);
                modeLabel[1].TextColor = UncannyUILoader.Instance.DefaultColor();
                modeLabel[1].Text = UntilHealedString;
                modeLabel[1].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                modeLabel[1].HorizontalAlignment = HorizontalAlignment.Center;
                modeLabel[1].VerticalAlignment = VerticalAlignment.Middle;

                modeButton[2] = DaggerfallUI.AddButton(modeButtonRect[2], mainPanel);
                modeButton[2].OnMouseClick += LoiterButton_OnMouseClick;
                modeButton[2].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestLoiter);
                modeButton[2].BackgroundTexture = modeButtonsImage[2];

                modeLabel[2] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, modeButton[2]);
                modeLabel[2].TextColor = UncannyUILoader.Instance.DefaultColor();
                modeLabel[2].Text = LoiterString;
                modeLabel[2].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                modeLabel[2].HorizontalAlignment = HorizontalAlignment.Center;
                modeLabel[2].VerticalAlignment = VerticalAlignment.Middle;
            }

            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 1 && !canRest)
            {
                restSlider = DaggerfallUI.AddSlider(sliderPos, 124, (x) => x.SetIndicator(1, DaggerfallUnity.Settings.LoiterLimitInHours, 1), 1, mainPanel);
            }
            else
            {
                restSlider = DaggerfallUI.AddSlider(sliderPos, 124, (x) => x.SetIndicator(1, 24, 1), 1, mainPanel);
            }

            restSlider.Indicator.Position += new Vector2(-13, -1);
            restSlider.Value = Mathf.Min(8, restSlider.TotalUnits);
            restSlider.Indicator.TextColor = UncannyUILoader.Instance.DefaultColor();

            allowRestPanel = DaggerfallUI.AddPanel(allowRestRect, mainPanel);
            allowRestLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, allowRestPanel);
            allowRestLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            allowRestLabel.Text = allowRestMsg;
            allowRestLabel.Text = allowRestMsg;
            allowRestLabel.WrapText = true;
            allowRestLabel.WrapWords = true;
            allowRestLabel.MaxWidth = 124;
            allowRestLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            allowRestLabel.HorizontalAlignment = HorizontalAlignment.Center;
            allowRestLabel.VerticalAlignment = VerticalAlignment.Middle;

            //Counter
            // Setup counter text
            counterTextPanel = DaggerfallUI.AddPanel(counterTextPanelRect, mainPanel);
            counterLabel.Position = new Vector2(0, 2);
            counterLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            counterLabel.HorizontalAlignment = HorizontalAlignment.Center;
            counterTextPanel.Components.Add(counterLabel);
            counterTextPanel.Enabled = false;

            // Stop button
            stopButton = DaggerfallUI.AddButton(modeButtonRect[2], mainPanel);
            stopButton.OnMouseClick += StopButton_OnMouseClick;
            stopButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestStop);
            stopButton.OnKeyboardEvent += StopButton_OnKeyboardEvent;
            stopButton.BackgroundTexture = modeButtonsImage[2];
            stopButton.Enabled = false;

            modeLabel[3] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, stopButton);
            modeLabel[3].TextColor = UncannyUILoader.Instance.DefaultColor();
            modeLabel[3].Text = StopString;
            modeLabel[3].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            modeLabel[3].HorizontalAlignment = HorizontalAlignment.Center;
            modeLabel[3].VerticalAlignment = VerticalAlignment.Middle;

            // Store toggle closed binding for this window
            toggleClosedBinding = InputManager.Instance.GetBinding(InputManager.Actions.Rest);
        }

        #endregion

        #region Overrides

        public override void Update()
        {
            base.Update();

            if (!endedRest)
                ShowStatus();

            if (currentTimeLabel != null)
                currentTimeLabel.Text = DaggerfallUnity.Instance.WorldTime.Now.DateString() + " " + DaggerfallUnity.Instance.WorldTime.Now.MinTimeString();

            if (currentRestMode != RestModes.Selection)
            {
                if (currentRestMode == RestModes.FullRest)
                {
                    restSlider.Value = totalHours;
                }
                else
                {
                    restSlider.Value = hoursRemaining;
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void OnPush()
        {
            base.OnPush();

            string allowRestMsg;
            var canRest = CheckRestStatus(out allowRestMsg);

            if (allowRestLabel != null)
            {
                allowRestLabel.Text = allowRestMsg;
            }
        }

        public override void OnPop()
        {
            base.OnPop();
        }

        #endregion

        #region Private Methods

        void LoadTextures()
        {
            baseTexture = UncannyUILoader.Instance.LoadRestUI();

            mainPanelTexture = new Texture2D(mainPanelSize.x, mainPanelSize.y);
            mainPanelTexture.filterMode = FilterMode.Point;
            mainPanelTexture.SetPixels(baseTexture.GetPixels(0, 11, mainPanelSize.x, mainPanelSize.y)); //pixels are read bottom up
            mainPanelTexture.Apply();

            modeButtonsImage = new Texture2D[3];

            for (var i = 0; i < modeButtonsImage.Length; i++)
            {
                modeButtonsImage[i] = new Texture2D((int)modeImageRect[i].width, (int)modeImageRect[i].height);
                modeButtonsImage[i].filterMode = FilterMode.Point;
                modeButtonsImage[i].SetPixels(baseTexture.GetPixels((int)modeImageRect[i].x, (int)modeImageRect[i].y, (int)modeImageRect[i].width, (int)modeImageRect[i].height));
                modeButtonsImage[i].Apply();
            }
        }

        private bool CheckRestStatus(out string out_msg)
        {
            remainingHoursRented = -1;
            allocatedBed = Vector3.zero;
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            if (playerGPS.IsPlayerInTown(true, true))
            {
                out_msg = IllegalToRestString;
                return false;
            }

            else if (playerGPS.IsPlayerInTown() && playerEnterExit.IsPlayerInsideBuilding)
            {
                var sceneName = DaggerfallInterior.GetSceneName(playerGPS.CurrentLocation.MapTableData.MapId, playerEnterExit.BuildingDiscoveryData.buildingKey);
                if (SaveLoadManager.StateManager.ContainsPermanentScene(sceneName))
                {
                    // Can rest if it's an player owned ship/house.
                    var buildingKey = playerEnterExit.BuildingDiscoveryData.buildingKey;
                    if (playerEnterExit.BuildingType == DFLocation.BuildingTypes.Ship || DaggerfallBankManager.IsHouseOwned(buildingKey))
                    {
                        out_msg = MayRestHereString;
                        return true;
                    }
                    else
                    {
                        // Find room rental record and get remaining time..
                        var mapId = playerGPS.CurrentLocation.MapTableData.MapId;
                        RoomRental_v1 room = GameManager.Instance.PlayerEntity.GetRentedRoom(mapId, buildingKey);
                        remainingHoursRented = PlayerEntity.GetRemainingHours(room);

                        // Get allocated bed marker - default to 0 if out of range
                        // We relink marker position by index as building positions are not stable, they can move from terrain mods or floating Y
                        Vector3[] restMarkers = playerEnterExit.Interior.FindMarkers(DaggerfallInterior.InteriorMarkerTypes.Rest);
                        var bedIndex = (room.allocatedBedIndex >= 0 && room.allocatedBedIndex < restMarkers.Length) ? room.allocatedBedIndex : 0;
                        allocatedBed = restMarkers[bedIndex];
                        if (remainingHoursRented > 0)
                        {
                            var remainingMsg = new StringBuilder();
                            var days = remainingHoursRented / 24;
                            var hours = remainingHoursRented % 24;

                            remainingMsg.Append(YouHaveARoomString);

                            if (days == 1)
                            {
                                remainingMsg.Append(" " + days + " " + DayString);
                            }
                            else if (days > 1)
                            {
                                remainingMsg.Append(" " + days + " " + DaysString);
                            }

                            if (days > 0 && hours > 0)
                            {
                                remainingMsg.Append(" " + AndString);
                            }

                            if (hours == 1)
                            {
                                remainingMsg.Append(" " + hours + " " + HourString);
                            }
                            else if (hours > 1)
                            {
                                remainingMsg.Append(" " + hours + " " + HoursString);
                            }

                            out_msg = remainingMsg.ToString();

                            return true;
                        }
                    }
                }

                // Check for guild hall rest privileges (exclude taverns since they are all marked as fighters guilds in data)
                if (playerEnterExit.BuildingDiscoveryData.buildingType != DFLocation.BuildingTypes.Tavern &&
                    GameManager.Instance.GuildManager.GetGuild(playerEnterExit.BuildingDiscoveryData.factionID).CanRest())
                {
                    playerEnterExit.Interior.FindMarker(out allocatedBed, DaggerfallInterior.InteriorMarkerTypes.Rest);
                    out_msg = MembershipAllowsToRestString;
                    return true;
                }

                if (playerEnterExit.BuildingDiscoveryData.buildingType == DFLocation.BuildingTypes.Tavern)
                {
                    //We are in a tavern but not rented a room
                    out_msg = MustRentARoomString;
                    return false;
                }

                out_msg = MayNotRestHereString;
                return false;
            }

            out_msg = MayRestHereString;
            return true;
        }

        void EndRest()
        {
            const int youWakeUpTextId = 353;
            const int enemiesNearby = 354;
            const int youAreHealedTextId = 350;
            const int finishedLoiteringTextId = 349;

            endedRest = true;

            if (enemyBrokeRest)
            {
                DaggerfallMessageBox mb = DaggerfallUI.MessageBox(enemiesNearby);
                mb.OnClose += RestFinishedPopup_OnClose;
            }
            else if (preventedRestMessage != null)
            {
                if (preventedRestMessage != "")
                {
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(preventedRestMessage);
                    mb.OnClose += RestFinishedPopup_OnClose;
                }
                else
                {
                    const int cannotRestNow = 355;
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(cannotRestNow);
                    mb.OnClose += RestFinishedPopup_OnClose;
                }
            }
            else
            {
                if (remainingHoursRented == 0)
                {
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("expiredRentedRoom"));
                    mb.OnClose += RestFinishedPopup_OnClose;
                    currentRestMode = RestModes.Selection;
                    playerEntity.RemoveExpiredRentedRooms();
                }
                else if (currentRestMode == RestModes.TimedRest)
                {
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(youWakeUpTextId);
                    mb.OnClose += RestFinishedPopup_OnClose;
                    currentRestMode = RestModes.Selection;
                }
                else if (currentRestMode == RestModes.FullRest)
                {
                    var message = IsPlayerFullyHealed() ? youAreHealedTextId : youWakeUpTextId;
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(message);
                    mb.OnClose += RestFinishedPopup_OnClose;
                    currentRestMode = RestModes.Selection;
                }
                else if (currentRestMode == RestModes.Loiter)
                {
                    DaggerfallMessageBox mb = DaggerfallUI.MessageBox(finishedLoiteringTextId);
                    mb.OnClose += RestFinishedPopup_OnClose;
                    currentRestMode = RestModes.Selection;
                }
            }
        }

        bool IsPlayerFullyHealed()
        {
            // Check if player fully healed
            // Will eventually need to tailor check for character
            // For example, sorcerers cannot recover magicka from resting
            if (playerEntity.CurrentHealth == playerEntity.MaxHealth &&
                playerEntity.CurrentFatigue == playerEntity.MaxFatigue &&
                (playerEntity.CurrentMagicka == playerEntity.MaxMagicka || playerEntity.Career.NoRegenSpellPoints))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if player is allowed to rest at this location.
        /// </summary>
        bool CanRest(bool alreadyWarned = false)
        {
            remainingHoursRented = -1;
            allocatedBed = Vector3.zero;
            PlayerEnterExit playerEnterExit = GameManager.Instance.PlayerEnterExit;
            PlayerGPS playerGPS = GameManager.Instance.PlayerGPS;

            if (playerGPS.IsPlayerInTown(true, true))
            {
                if (!alreadyWarned)
                {
                    CloseWindow();
                    DaggerfallUI.MessageBox(cityCampingIllegal);
                }

                // Register crime and start spawning guards
                playerEntity.CrimeCommitted = PlayerEntity.Crimes.Vagrancy;
                playerEntity.SpawnCityGuards(true);

                return alreadyWarned;
            }
            else if (playerGPS.IsPlayerInTown() && playerEnterExit.IsPlayerInsideBuilding)
            {
                // Check owned locations
                var sceneName = DaggerfallInterior.GetSceneName(playerGPS.CurrentLocation.MapTableData.MapId, playerEnterExit.BuildingDiscoveryData.buildingKey);
                if (SaveLoadManager.StateManager.ContainsPermanentScene(sceneName))
                {
                    // Can rest if it's an player owned ship/house.
                    var buildingKey = playerEnterExit.BuildingDiscoveryData.buildingKey;
                    if (playerEnterExit.BuildingType == DFLocation.BuildingTypes.Ship || DaggerfallBankManager.IsHouseOwned(buildingKey))
                        return true;

                    // Find room rental record and get remaining time..
                    var mapId = playerGPS.CurrentLocation.MapTableData.MapId;
                    RoomRental_v1 room = GameManager.Instance.PlayerEntity.GetRentedRoom(mapId, buildingKey);
                    remainingHoursRented = PlayerEntity.GetRemainingHours(room);

                    // Get allocated bed marker - default to 0 if out of range
                    // We relink marker position by index as building positions are not stable, they can move from terrain mods or floating Y
                    Vector3[] restMarkers = playerEnterExit.Interior.FindMarkers(DaggerfallInterior.InteriorMarkerTypes.Rest);
                    var bedIndex = (room.allocatedBedIndex >= 0 && room.allocatedBedIndex < restMarkers.Length) ? room.allocatedBedIndex : 0;
                    allocatedBed = restMarkers[bedIndex];
                    if (remainingHoursRented > 0)
                        return true;
                }

                // Check for guild hall rest privileges (exclude taverns since they are all marked as fighters guilds in data)
                if (playerEnterExit.BuildingDiscoveryData.buildingType != DFLocation.BuildingTypes.Tavern &&
                    GameManager.Instance.GuildManager.GetGuild(playerEnterExit.BuildingDiscoveryData.factionID).CanRest())
                {
                    playerEnterExit.Interior.FindMarker(out allocatedBed, DaggerfallInterior.InteriorMarkerTypes.Rest);
                    return true;
                }

                CloseWindow();
                DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("haveNotRentedRoom"));
                return false;
            }

            return true;
        }

        void MoveToBed()
        {
            if (allocatedBed != Vector3.zero && !ignoreAllocatedBed)
            {
                PlayerMotor playerMotor = GameManager.Instance.PlayerMotor;
                playerMotor.transform.position = allocatedBed;
                playerMotor.FixStanding(0.4f, 0.4f);
            }
        }

        void DoRestForAWhile(bool alreadyWarned)
        {
            if (CanRest(alreadyWarned))
            {
                const int cannotRestMoreThan99HoursTextId = 26;

                // Validate input
                var time = selectedTime;

                // Validate range
                if (time < 0)
                {
                    time = 0;
                }
                else if (time > 99)
                {
                    DaggerfallUI.MessageBox(cannotRestMoreThan99HoursTextId);
                    return;
                }

                hoursRemaining = time;
                waitTimer = Time.realtimeSinceStartup;
                currentRestMode = RestModes.TimedRest;
                MoveToBed();
            }
        }

        void DoRestUntilHealed(bool alreadyWarned)
        {
            if (CanRest(alreadyWarned))
            {
                waitTimer = Time.realtimeSinceStartup;
                currentRestMode = RestModes.FullRest;
                MoveToBed();
            }
        }

        #endregion

        #region Event Handlers

        void ShowStatus()
        {
            //Always override this
            mainPanel.Enabled = true;
            counterPanel.Enabled = false;

            // Display status based on current rest state
            if (currentRestMode == RestModes.Selection)
            {
                for (var i = 0; i < modeButton.Length; i++)
                {
                    if (modeButton[i] != null)
                    {
                        modeButton[i].Enabled = true;
                    }
                }

                stopButton.Enabled = false;
                allowRestPanel.Enabled = true;
            }
            else if (currentRestMode == RestModes.FullRest)
            {
                for (var i = 0; i < modeButton.Length; i++)
                {
                    if (modeButton[i] != null)
                    {
                        modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                allowRestPanel.Enabled = false;
                counterLabel.Text = totalHours.ToString();
            }
            else if (currentRestMode == RestModes.TimedRest)
            {
                for (var i = 0; i < modeButton.Length; i++)
                {
                    if (modeButton[i] != null)
                    {
                        modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                allowRestPanel.Enabled = false;
                counterLabel.Text = hoursRemaining.ToString();
            }
            else if (currentRestMode == RestModes.Loiter)
            {
                for (var i = 0; i < modeButton.Length; i++)
                {
                    if (modeButton[i] != null)
                    {
                        modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                allowRestPanel.Enabled = false;
                counterLabel.Text = hoursRemaining.ToString();
            }
        }

        private void WhileButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTime = restSlider.Value;

            if (DaggerfallUnity.Settings.IllegalRestWarning && GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallMessageBox mb = DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("illegalRestWarning"));
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                mb.OnButtonClick += ConfirmIllegalRestForAWhile_OnButtonClick;
            }
            else
            {
                DoRestForAWhile(false);
            }
        }

        private void ConfirmIllegalRestForAWhile_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                DoRestForAWhile(true);
            }
        }

        private void HealedButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTime = restSlider.Value;

            if (DaggerfallUnity.Settings.IllegalRestWarning && GameManager.Instance.PlayerGPS.IsPlayerInTown(true, true))
            {
                DaggerfallMessageBox mb = DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("illegalRestWarning"));
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                mb.AddButton(DaggerfallMessageBox.MessageBoxButtons.No);
                mb.OnButtonClick += ConfirmIllegalRestUntilHealed_OnButtonClick;
            }
            else
            {
                DoRestUntilHealed(false);
            }
        }

        private void ConfirmIllegalRestUntilHealed_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                DoRestUntilHealed(true);
            }
        }

        private void LoiterButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // Validate input
            selectedTime = restSlider.Value;

            // Validate range
            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") > 1 && selectedTime > DaggerfallUnity.Settings.LoiterLimitInHours)
            {
                DaggerfallUI.MessageBox(new string[]
                {
                    TextManager.Instance.GetLocalizedText("cannotLoiterMoreThanXHours1"),
                    string.Format(TextManager.Instance.GetLocalizedText("cannotLoiterMoreThanXHours2"), DaggerfallUnity.Settings.LoiterLimitInHours)
                });
                return;
            }

            hoursRemaining = selectedTime;
            waitTimer = Time.realtimeSinceStartup;
            currentRestMode = RestModes.Loiter;
            playerEntity.IsLoitering = true;
        }

        private void StopButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            EndRest();
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
        }

        private void RestFinishedPopup_OnClose()
        {
            DaggerfallUI.Instance.PopToHUD();
            playerEntity.RaiseSkills();
        }

        #endregion
    }
}