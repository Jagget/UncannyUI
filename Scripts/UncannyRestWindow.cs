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
using DaggerfallConnect.Utility;
using DaggerfallWorkshop.Utility;

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
        private Texture2D[] _modeButtonsImage;
        private readonly Rect[] _modeImageRect = { new Rect(5, 58, 48, 11), new Rect(54, 58, 48, 11), new Rect(103, 58, 48, 11) };
        private readonly Rect[] _modeButtonRect = { new Rect(4, 43, 48, 11), new Rect(53, 43, 48, 11), new Rect(102, 43, 48, 11) };
        private readonly Button[] _modeButton = new Button[3];
        private readonly TextLabel[] _modeLabel = new TextLabel[4];

        private Panel _counterTextPanel;
        private Rect _counterTextPanelRect = new Rect(4, 10, 16, 8);
        private Panel _currentTimePanel;
        private TextLabel _currentTimeLabel;

        private Panel _allowRestPanel;
        private TextLabel _allowRestLabel;

        private Texture2D _mainPanelTexture;
        private Vector2Int _mainPanelSize = new Vector2Int(154, 58);

        private HorizontalSlider _restSlider;

        private int _selectedTime;

        private Rect _currentTimeRect = new Rect(17, 32, 124, 10);
        private Vector2 _sliderPos = new Vector2(17, 23);
        private Rect _allowRestRect = new Rect(17, 4, 124, 16);

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
            mainPanel.BackgroundTexture = _mainPanelTexture;
            mainPanel.Position = new Vector2(0, 70);
            mainPanel.Size = new Vector2(_mainPanelSize.x, _mainPanelSize.y);

            NativePanel.Components.Add(mainPanel);

            var canRest = CheckRestStatus(out string allowRestMsg);

            _currentTimePanel = DaggerfallUI.AddPanel(_currentTimeRect, mainPanel);
            _currentTimeLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _currentTimePanel);
            _currentTimeLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            _currentTimeLabel.Text = DaggerfallUnity.Instance.WorldTime.Now.DateString() + " " + DaggerfallUnity.Instance.WorldTime.Now.MinTimeString();
            _currentTimeLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            _currentTimeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _currentTimeLabel.VerticalAlignment = VerticalAlignment.Middle;

            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") < 2)
            {
                if (canRest)
                {
                    _modeButton[0] = DaggerfallUI.AddButton(_modeButtonRect[1], mainPanel);
                    _modeButton[0].OnMouseClick += HealedButton_OnMouseClick;
                    _modeButton[0].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestUntilHealed);
                    _modeButton[0].BackgroundTexture = _modeButtonsImage[0];

                    _modeLabel[0] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _modeButton[0]);
                    _modeLabel[0].TextColor = UncannyUILoader.Instance.DefaultColor();
                    _modeLabel[0].Text = UntilHealedString;
                    _modeLabel[0].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                    _modeLabel[0].HorizontalAlignment = HorizontalAlignment.Center;
                    _modeLabel[0].VerticalAlignment = VerticalAlignment.Middle;
                }

                _modeButton[1] = DaggerfallUI.AddButton(_modeButtonRect[2], mainPanel);
                _modeButton[1].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestForAWhile);
                _modeButton[1].BackgroundTexture = _modeButtonsImage[1];

                _modeLabel[1] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _modeButton[1]);
                _modeLabel[1].TextColor = UncannyUILoader.Instance.DefaultColor();
                _modeLabel[1].Text = RestString;
                _modeLabel[1].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                _modeLabel[1].HorizontalAlignment = HorizontalAlignment.Center;
                _modeLabel[1].VerticalAlignment = VerticalAlignment.Middle;

                if (canRest)
                {
                    _modeButton[1].OnMouseClick += WhileButton_OnMouseClick;
                    _modeLabel[1].Text = RestString;
                }
                else
                {
                    _modeButton[1].OnMouseClick += LoiterButton_OnMouseClick;

                    if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 0)
                        _modeLabel[1].Text = WaitString;

                    else if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 1)
                        _modeLabel[1].Text = LoiterString;
                }
            }
            else if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 2)
            {
                _modeButton[0] = DaggerfallUI.AddButton(_modeButtonRect[0], mainPanel);
                _modeButton[0].OnMouseClick += WhileButton_OnMouseClick;
                _modeButton[0].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestForAWhile);
                _modeButton[0].BackgroundTexture = _modeButtonsImage[0];

                _modeLabel[0] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _modeButton[0]);
                _modeLabel[0].TextColor = UncannyUILoader.Instance.DefaultColor();
                _modeLabel[0].Text = RestString;
                _modeLabel[0].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                _modeLabel[0].HorizontalAlignment = HorizontalAlignment.Center;
                _modeLabel[0].VerticalAlignment = VerticalAlignment.Middle;

                _modeButton[1] = DaggerfallUI.AddButton(_modeButtonRect[1], mainPanel);
                _modeButton[1].OnMouseClick += HealedButton_OnMouseClick;
                _modeButton[1].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestUntilHealed);
                _modeButton[1].BackgroundTexture = _modeButtonsImage[1];

                _modeLabel[1] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _modeButton[1]);
                _modeLabel[1].TextColor = UncannyUILoader.Instance.DefaultColor();
                _modeLabel[1].Text = UntilHealedString;
                _modeLabel[1].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                _modeLabel[1].HorizontalAlignment = HorizontalAlignment.Center;
                _modeLabel[1].VerticalAlignment = VerticalAlignment.Middle;

                _modeButton[2] = DaggerfallUI.AddButton(_modeButtonRect[2], mainPanel);
                _modeButton[2].OnMouseClick += LoiterButton_OnMouseClick;
                _modeButton[2].Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestLoiter);
                _modeButton[2].BackgroundTexture = _modeButtonsImage[2];

                _modeLabel[2] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _modeButton[2]);
                _modeLabel[2].TextColor = UncannyUILoader.Instance.DefaultColor();
                _modeLabel[2].Text = LoiterString;
                _modeLabel[2].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
                _modeLabel[2].HorizontalAlignment = HorizontalAlignment.Center;
                _modeLabel[2].VerticalAlignment = VerticalAlignment.Middle;
            }

            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") == 1 && !canRest)
            {
                _restSlider = DaggerfallUI.AddSlider(_sliderPos, 124, (x) => x.SetIndicator(1, DaggerfallUnity.Settings.LoiterLimitInHours, 1), 1, mainPanel);
            }
            else
            {
                _restSlider = DaggerfallUI.AddSlider(_sliderPos, 124, (x) => x.SetIndicator(1, 24, 1), 1, mainPanel);
            }

            _restSlider.Indicator.Position += new Vector2(-13, -1);
            _restSlider.Value = Mathf.Min(8, _restSlider.TotalUnits);
            _restSlider.Indicator.TextColor = UncannyUILoader.Instance.DefaultColor();

            _allowRestPanel = DaggerfallUI.AddPanel(_allowRestRect, mainPanel);
            _allowRestLabel = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, _allowRestPanel);
            _allowRestLabel.TextColor = UncannyUILoader.Instance.DefaultColor();
            _allowRestLabel.Text = allowRestMsg;
            _allowRestLabel.Text = allowRestMsg;
            _allowRestLabel.WrapText = true;
            _allowRestLabel.WrapWords = true;
            _allowRestLabel.MaxWidth = 124;
            _allowRestLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            _allowRestLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _allowRestLabel.VerticalAlignment = VerticalAlignment.Middle;

            //Counter
            // Setup counter text
            _counterTextPanel = DaggerfallUI.AddPanel(_counterTextPanelRect, mainPanel);
            counterLabel.Position = new Vector2(0, 2);
            counterLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            counterLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _counterTextPanel.Components.Add(counterLabel);
            _counterTextPanel.Enabled = false;

            // Stop button
            stopButton = DaggerfallUI.AddButton(_modeButtonRect[2], mainPanel);
            stopButton.OnMouseClick += StopButton_OnMouseClick;
            stopButton.Hotkey = DaggerfallShortcut.GetBinding(DaggerfallShortcut.Buttons.RestStop);
            stopButton.OnKeyboardEvent += StopButton_OnKeyboardEvent;
            stopButton.BackgroundTexture = _modeButtonsImage[2];
            stopButton.Enabled = false;

            _modeLabel[3] = DaggerfallUI.AddDefaultShadowedTextLabel(Vector2.zero, stopButton);
            _modeLabel[3].TextColor = UncannyUILoader.Instance.DefaultColor();
            _modeLabel[3].Text = StopString;
            _modeLabel[3].HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            _modeLabel[3].HorizontalAlignment = HorizontalAlignment.Center;
            _modeLabel[3].VerticalAlignment = VerticalAlignment.Middle;

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

            if (_currentTimeLabel != null)
                _currentTimeLabel.Text = DaggerfallUnity.Instance.WorldTime.Now.DateString() + " " + DaggerfallUnity.Instance.WorldTime.Now.MinTimeString();

            if (currentRestMode != RestModes.Selection)
            {
                if (currentRestMode == RestModes.FullRest)
                {
                    _restSlider.Value = totalHours;
                }
                else
                {
                    _restSlider.Value = hoursRemaining;
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

            if (_allowRestLabel != null)
            {
                _allowRestLabel.Text = allowRestMsg;
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

            DFSize baseSize = new DFSize(154, 69);

            _mainPanelTexture = ImageReader.GetSubTexture(baseTexture, new Rect(0, 0, _mainPanelSize.x, _mainPanelSize.y), baseSize);
            _mainPanelTexture.filterMode = DaggerfallUI.Instance.GlobalFilterMode;

            _modeButtonsImage = new Texture2D[3];

            for (var i = 0; i < _modeButtonsImage.Length; i++)
            {
                _modeButtonsImage[i] = ImageReader.GetSubTexture(baseTexture, _modeImageRect[i], baseSize);
                _modeButtonsImage[i].filterMode = DaggerfallUI.Instance.GlobalFilterMode;
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
                var time = _selectedTime;

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
                for (var i = 0; i < _modeButton.Length; i++)
                {
                    if (_modeButton[i] != null)
                    {
                        _modeButton[i].Enabled = true;
                    }
                }

                stopButton.Enabled = false;
                _allowRestPanel.Enabled = true;
            }
            else if (currentRestMode == RestModes.FullRest)
            {
                for (var i = 0; i < _modeButton.Length; i++)
                {
                    if (_modeButton[i] != null)
                    {
                        _modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                _allowRestPanel.Enabled = false;
                counterLabel.Text = totalHours.ToString();
            }
            else if (currentRestMode == RestModes.TimedRest)
            {
                for (var i = 0; i < _modeButton.Length; i++)
                {
                    if (_modeButton[i] != null)
                    {
                        _modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                _allowRestPanel.Enabled = false;
                counterLabel.Text = hoursRemaining.ToString();
            }
            else if (currentRestMode == RestModes.Loiter)
            {
                for (var i = 0; i < _modeButton.Length; i++)
                {
                    if (_modeButton[i] != null)
                    {
                        _modeButton[i].Enabled = false;
                    }
                }

                stopButton.Enabled = true;
                _allowRestPanel.Enabled = false;
                counterLabel.Text = hoursRemaining.ToString();
            }
        }

        private void WhileButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            _selectedTime = _restSlider.Value;

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
            _selectedTime = _restSlider.Value;

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
            _selectedTime = _restSlider.Value;

            // Validate range
            if (UncannyUILoader.Instance.GetModSettings().GetInt("Rest", "LoiterMode") > 1 && _selectedTime > DaggerfallUnity.Settings.LoiterLimitInHours)
            {
                DaggerfallUI.MessageBox(new string[]
                {
                    TextManager.Instance.GetLocalizedText("cannotLoiterMoreThanXHours1"),
                    string.Format(TextManager.Instance.GetLocalizedText("cannotLoiterMoreThanXHours2"), DaggerfallUnity.Settings.LoiterLimitInHours)
                });
                return;
            }

            hoursRemaining = _selectedTime;
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