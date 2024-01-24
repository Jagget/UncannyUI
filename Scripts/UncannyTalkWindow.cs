using UnityEngine;
using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop.Game.UserInterface
{
    public class UncannyTalkWindow : DaggerfallTalkWindow
    {
        enum PortraitPosition
        {
            Off = 0,
            Left = 1,
            Center = 2,
            Right = 3,
        }

        const int MainPanelWidth = 310;

        List<TalkManager.ListItem> listTopicMain;
        List<TalkManager.ListItem> listTopicCurrentEvents;

        TextLabel labelTonePolite;
        TextLabel labelToneNormal;
        TextLabel labelToneBlunt;
        TextLabel labelCopyMSG;

        float drawCopyMSGTime;
        Panel panelCopyMSG;

        int lastHover = -1;

        int portraitPosition = 0;
        int portraitSize = 32;

        protected readonly string GoodByeSring = UncannyUILoader.Instance.GetMod().Localize("Goodbye");
        protected readonly string LabelToneStringPolite = UncannyUILoader.Instance.GetMod().Localize("Polite");
        protected readonly string LabelToneStringNormal = UncannyUILoader.Instance.GetMod().Localize("Normal");
        protected readonly string LabelToneStringBlunt = UncannyUILoader.Instance.GetMod().Localize("Blunt");
        protected readonly string CopyToLogString = UncannyUILoader.Instance.GetMod().Localize("CopiedtoLog");

        protected readonly string TopicWhereString = UncannyUILoader.Instance.GetMod().Localize("WhereamI");
        protected readonly string TopicWorkString = UncannyUILoader.Instance.GetMod().Localize("Anywork");
        protected readonly string TopicNewsString = UncannyUILoader.Instance.GetMod().Localize("Anynews");

        protected readonly string TopicEventsString = UncannyUILoader.Instance.GetMod().Localize("CurrentEvents");
        protected readonly string TopicFactionString = UncannyUILoader.Instance.GetMod().Localize("Factions");
        protected readonly string TopicLocationString = UncannyUILoader.Instance.GetMod().Localize("Locations");
        protected readonly string TopicPeopleString = UncannyUILoader.Instance.GetMod().Localize("People");
        protected readonly string TopicThingsString = UncannyUILoader.Instance.GetMod().Localize("Things");
        protected readonly string TopicPreviousString = UncannyUILoader.Instance.GetMod().Localize("PreviousList");

        public UncannyTalkWindow(IUserInterfaceManager uiManager, DaggerfallBaseWindow previous = null)
            : base(uiManager, previous)
        {
            ParentPanel.BackgroundColor = Color.clear;
        }

        public override void OnPush()
        {
            // Racial override can suppress talk
            // We still setup and push window normally, actual suppression is done in Update()
            MagicAndEffects.MagicEffects.RacialOverrideEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
            if (racialOverride != null)
                suppressTalk = racialOverride.GetSuppressTalk(out suppressTalkMessage);

            copyIndexes = new List<int>();
            if (listboxTopic != null)
                listboxTopic.ClearItems();

            SetStartConversation();

            // Reset scrollbars
            if (verticalScrollBarTopic != null)
                verticalScrollBarTopic.ScrollIndex = 0;
            if (horizontalSliderTopic != null)
                horizontalSliderTopic.ScrollIndex = 0;
            if (verticalScrollBarTopic != null && horizontalSliderTopic != null)
                UpdateScrollBarsTopic();
            if (verticalScrollBarConversation != null)
            {
                verticalScrollBarConversation.ScrollIndex = 0;
                UpdateScrollBarConversation();
            }

            if (textlabelPlayerSays != null)
                textlabelPlayerSays.Text = "";

            if (isSetup)
            {
                SetTalkCategoryMain();
            }

            toneLastUsed = -1;
            currentQuestion = "";
        }


        //Even if we dont use Portratits, we still need to keep this for the TalkManager to work
        public override void SetNPCPortrait(FacePortraitArchive facePortraitArchive, int recordId)
        {
            base.SetNPCPortrait(facePortraitArchive, recordId);
            return;
        }

        protected override void Setup()
        {
            textureBackground = UncannyUILoader.Instance.LoadDialougeUI();

            if (!textureBackground)
            {
                Debug.LogError(string.Format("Failed to load background image for talk window"));
                CloseWindow();
                return;
            }

            textureBackground.filterMode = DaggerfallUI.Instance.GlobalFilterMode;
            mainPanel = DaggerfallUI.AddPanel(NativePanel, AutoSizeModes.None);
            mainPanel.BackgroundTexture = textureBackground;
            mainPanel.Size = new Vector2(MainPanelWidth, 102); // reference size is always vanilla df resolution
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Bottom;

            portraitPosition = UncannyUILoader.Instance.GetModSettings().GetInt("Dialogue", "PortraitPosition");
            portraitSize = UncannyUILoader.Instance.GetModSettings().GetInt("Dialogue", "PortraitSize");

            if (portraitPosition > 0)
            {
                int portraitPos = 5;

                switch (portraitPosition)
                {
                    case (int)PortraitPosition.Right:
                        portraitPos = MainPanelWidth - portraitSize;
                        break;
                    case (int)PortraitPosition.Left:
                        portraitPos = 0;
                        break;
                    case (int)PortraitPosition.Center:
                        portraitPos = 155 - (portraitSize / 2);
                        break;
                    default:
                        break;
                }

                panelPortraitPos = new Vector2(portraitPos, -portraitSize + 6);
                panelPortraitSize = new Vector2(portraitSize, portraitSize);
                panelPortrait = DaggerfallUI.AddPanel(new Rect(panelPortraitPos, panelPortraitSize), mainPanel);
                panelPortrait.BackgroundTexture = texturePortrait;
            }

            panelNameNPC = DaggerfallUI.AddPanel(mainPanel, AutoSizeModes.None);

            int NPCNamePanelWidth = 128;

            panelNameNPC.Position = portraitPosition == (int)PortraitPosition.Right
                ? new Vector2(MainPanelWidth - NPCNamePanelWidth - portraitSize - 5, -1)
                : new Vector2(MainPanelWidth - NPCNamePanelWidth - 1, -1);

            panelNameNPC.Size = new Vector2(NPCNamePanelWidth, 10);

            labelNameNPC = new TextLabel();
            labelNameNPC.Position = new Vector2(0, 0);
            labelNameNPC.Size = new Vector2(NPCNamePanelWidth, 10);
            labelNameNPC.Name = "label_npcName";
            labelNameNPC.MaxCharacters = -1;
            labelNameNPC.HorizontalAlignment = HorizontalAlignment.Right;
            labelNameNPC.VerticalAlignment = VerticalAlignment.Top;
            labelNameNPC.TextColor = UncannyUILoader.Instance.DefaultColor();
            panelNameNPC.Components.Add(labelNameNPC);

            UpdateNameNPC();

            textlabelPlayerSays = new TextLabel();
            textlabelPlayerSays.Position = new Vector2(116, 75);
            textlabelPlayerSays.Size = new Vector2(124, 38);
            textlabelPlayerSays.Name = "label_player_says";
            textlabelPlayerSays.MaxWidth = (int)textlabelPlayerSays.Size.x;
            textlabelPlayerSays.MaxCharacters = -1;
            textlabelPlayerSays.WrapText = true;
            textlabelPlayerSays.WrapWords = true;
            textlabelPlayerSays.TextColor = textcolorPlayerSays;
            mainPanel.Components.Add(textlabelPlayerSays);

            listboxTopic = new ListBox();
            listboxTopic.OnScroll += ListBoxTopic_OnScroll;
            listboxTopic.Position = new Vector2(4, 9);
            listboxTopic.Size = new Vector2(97, 90);

            listboxTopic.MaxCharacters = -1;
            listboxTopic.RowSpacing = 0;
            listboxTopic.Name = "list_topic";
            listboxTopic.EnabledHorizontalScroll = true;
            listboxTopic.VerticalScrollMode = ListBox.VerticalScrollModes.PixelWise;
            listboxTopic.HorizontalScrollMode = ListBox.HorizontalScrollModes.PixelWise;
            listboxTopic.RectRestrictedRenderArea = new Rect(listboxTopic.Position, listboxTopic.Size);
            listboxTopic.RestrictedRenderAreaCoordinateType = BaseScreenComponent.RestrictedRenderArea_CoordinateType.ParentCoordinates;

            //listboxTopic.OnSelectItem += ListboxTopic_OnSelectItem;
            listboxTopic.OnMouseMove += ListboxTopic_OnMouseMove;
            listboxTopic.OnMouseLeave += ListboxTopic_OnMouseLeave;
            listboxTopic.OnMouseDown += ListboxTopic_OnMouseClick;

            mainPanel.Components.Add(listboxTopic);

            listboxConversation = new ListBox();
            listboxConversation.OnScroll += ListBoxConversation_OnScroll;
            listboxConversation.Position = new Vector2(111, 9);
            listboxConversation.Size = new Vector2(191, 62);
            listboxConversation.RowSpacing = 4;
            listboxConversation.MaxCharacters = -1; // text is wrapped, so no max characters defined
            listboxConversation.Name = "list_answers";
            listboxConversation.WrapTextItems = true;
            listboxConversation.WrapWords = true;
            listboxConversation.RectRestrictedRenderArea = new Rect(listboxConversation.Position, listboxConversation.Size);
            listboxConversation.RestrictedRenderAreaCoordinateType = BaseScreenComponent.RestrictedRenderArea_CoordinateType.ParentCoordinates;
            listboxConversation.VerticalScrollMode = ListBox.VerticalScrollModes.PixelWise;
            listboxConversation.SelectedShadowPosition = DaggerfallUI.DaggerfallDefaultShadowPos;
            listboxConversation.OnMouseDoubleClick += ListboxConversation_OnMouseDoubleClick;

            mainPanel.Components.Add(listboxConversation);

            labelTonePolite = new TextLabel();
            labelTonePolite.Position = new Vector2(260, 76);
            labelTonePolite.Size = new Vector2(197, 8);
            labelTonePolite.Text = LabelToneStringPolite;
            labelTonePolite.MaxCharacters = -1;
            labelTonePolite.TextColor = UncannyUILoader.Instance.DefaultColor();

            mainPanel.Components.Add(labelTonePolite);

            labelToneNormal = new TextLabel();
            labelToneNormal.Position = new Vector2(260, 84);
            labelToneNormal.Size = new Vector2(197, 8);
            labelToneNormal.Text = LabelToneStringNormal;
            labelToneNormal.MaxCharacters = -1;
            labelToneNormal.TextColor = UncannyUILoader.Instance.DefaultColor();

            mainPanel.Components.Add(labelToneNormal);

            labelToneBlunt = new TextLabel();
            labelToneBlunt.Position = new Vector2(260, 92);
            labelToneBlunt.Size = new Vector2(197, 8);
            labelToneBlunt.Text = LabelToneStringBlunt;
            labelToneBlunt.MaxCharacters = -1;
            labelToneBlunt.TextColor = UncannyUILoader.Instance.DefaultColor();

            mainPanel.Components.Add(labelToneBlunt);

            rectButtonTonePolite = new Rect(257, 174, 4, 4);
            rectButtonToneNormal = new Rect(257, 182, 4, 4);
            rectButtonToneBlunt = new Rect(257, 190, 4, 4);

            panelTonePolitePos = new Vector2(257, 174);
            panelToneNormalPos = new Vector2(257, 182);
            panelToneBluntPos = new Vector2(257, 190);
            panelToneSize = new Vector2(4f, 4f);

            panelCopyMSG = DaggerfallUI.AddPanel(mainPanel, AutoSizeModes.None);
            panelCopyMSG.Position = new Vector2(0, 0);
            panelCopyMSG.HorizontalAlignment = HorizontalAlignment.Center;
            panelCopyMSG.VerticalAlignment = VerticalAlignment.Middle;
            panelCopyMSG.Size = new Vector2(64, 10);
            panelCopyMSG.BackgroundColor = new Color(0, 0, 0, 0.75f);
            panelCopyMSG.SetFocus();

            labelCopyMSG = new TextLabel();
            labelCopyMSG.Position = new Vector2(0, 0);
            labelCopyMSG.Size = new Vector2(64, 10);
            labelCopyMSG.Text = CopyToLogString;
            labelCopyMSG.MaxCharacters = -1;
            labelCopyMSG.HorizontalAlignment = HorizontalAlignment.Center;
            labelCopyMSG.VerticalAlignment = VerticalAlignment.Middle;
            panelCopyMSG.Components.Add(labelCopyMSG);
            panelCopyMSG.Enabled = false;

            SetStartConversation();
            SetupCheckboxes();
            SetupScrollBars();
            UpdateCheckboxes();
            UpdateScrollBarsTopic();
            UpdateScrollBarConversation();
            SetTalkCategoryMain();
            isSetup = true;
        }

        private void SetupTopics()
        {
            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////

            listTopicCurrentEvents = new List<TalkManager.ListItem>();
            listTopicCurrentEvents.Clear();
            listTopicCurrentEvents.Add(new TalkManager.ListItem());
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].caption = TopicWhereString;
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].questionType = TalkManager.QuestionType.WhereAmI;

            listTopicCurrentEvents.Add(new TalkManager.ListItem());
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].caption = TopicWorkString;
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].questionType = TalkManager.QuestionType.Work;

            listTopicCurrentEvents.Add(new TalkManager.ListItem());
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].caption = TopicNewsString;
            listTopicCurrentEvents[listTopicCurrentEvents.Count - 1].questionType = TalkManager.QuestionType.News;

            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////

            listTopicMain = new List<TalkManager.ListItem>();
            listTopicMain.Clear();

            listTopicMain.Add(new TalkManager.ListItem());
            listTopicMain[listTopicMain.Count - 1].caption = TopicEventsString;
            listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
            listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.ItemGroup;
            listTopicMain[listTopicMain.Count - 1].listChildItems = listTopicCurrentEvents;

            listTopicMain.Add(new TalkManager.ListItem());
            listTopicMain[listTopicMain.Count - 1].caption = TopicFactionString;
            listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
            listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.ItemGroup;
            listTopicMain[listTopicMain.Count - 1].listChildItems = TalkManager.Instance.ListTopicTellMeAbout;

            listTopicMain.Add(new TalkManager.ListItem());
            listTopicMain[listTopicMain.Count - 1].caption = TopicLocationString;
            listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
            listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.ItemGroup;
            listTopicMain[listTopicMain.Count - 1].listChildItems = TalkManager.Instance.ListTopicLocation;

            if (TalkManager.Instance.ListTopicPerson.Count > 0)
            {
                if (!(TalkManager.Instance.ListTopicPerson.Count == 1 && TalkManager.Instance.ListTopicPerson[0].type
                     == TalkManager.ListItemType.NavigationBack))
                {
                    listTopicMain.Add(new TalkManager.ListItem());
                    listTopicMain[listTopicMain.Count - 1].caption = TopicPeopleString;
                    listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
                    listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.ItemGroup;
                    listTopicMain[listTopicMain.Count - 1].listChildItems = TalkManager.Instance.ListTopicPerson;
                }
            }

            if (TalkManager.Instance.ListTopicThings != null && TalkManager.Instance.ListTopicThings.Count > 0)
            {
                if (!(TalkManager.Instance.ListTopicThings.Count == 1 && TalkManager.Instance.ListTopicThings[0].type
                    == TalkManager.ListItemType.NavigationBack))
                {
                    listTopicMain.Add(new TalkManager.ListItem());
                    listTopicMain[listTopicMain.Count - 1].caption = TopicThingsString;
                    listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
                    listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.ItemGroup;
                    listTopicMain[listTopicMain.Count - 1].listChildItems = TalkManager.Instance.ListTopicThings;
                }
            }

            listTopicMain.Add(new TalkManager.ListItem());
            listTopicMain[listTopicMain.Count - 1].caption = GoodByeSring;
            listTopicMain[listTopicMain.Count - 1].questionType = TalkManager.QuestionType.NoQuestion;
            listTopicMain[listTopicMain.Count - 1].type = TalkManager.ListItemType.Item;

            /////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            if (TalkManager.Instance.ListTopicPerson.Count == 0)
            {
                TalkManager.Instance.ListTopicPerson.Insert(0, new TalkManager.ListItem());
                TalkManager.Instance.ListTopicPerson[0].type = TalkManager.ListItemType.NavigationBack;
                TalkManager.Instance.ListTopicPerson[0].caption = TopicPreviousString;
                TalkManager.Instance.ListTopicPerson[0].listParentItems = listTopicMain;
            }
            else if (TalkManager.Instance.ListTopicPerson[0].type != TalkManager.ListItemType.NavigationBack)
            {
                TalkManager.Instance.ListTopicPerson.Insert(0, new TalkManager.ListItem());
                TalkManager.Instance.ListTopicPerson[0].type = TalkManager.ListItemType.NavigationBack;
                TalkManager.Instance.ListTopicPerson[0].caption = TopicPreviousString;
                TalkManager.Instance.ListTopicPerson[0].listParentItems = listTopicMain;
            }
            if (TalkManager.Instance.ListTopicThings != null)
            {
                if (TalkManager.Instance.ListTopicThings.Count == 0)
                {
                    TalkManager.Instance.ListTopicThings.Insert(0, new TalkManager.ListItem());
                    TalkManager.Instance.ListTopicThings[0].type = TalkManager.ListItemType.NavigationBack;
                    TalkManager.Instance.ListTopicThings[0].caption = TopicPreviousString;
                    TalkManager.Instance.ListTopicThings[0].listParentItems = listTopicMain;
                }

                else if (TalkManager.Instance.ListTopicThings[0].type != TalkManager.ListItemType.NavigationBack)
                {
                    TalkManager.Instance.ListTopicThings.Insert(0, new TalkManager.ListItem());
                    TalkManager.Instance.ListTopicThings[0].type = TalkManager.ListItemType.NavigationBack;
                    TalkManager.Instance.ListTopicThings[0].caption = TopicPreviousString;
                    TalkManager.Instance.ListTopicThings[0].listParentItems = listTopicMain;
                }
            }

            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////

            if (listTopicCurrentEvents[0].type != TalkManager.ListItemType.NavigationBack)
            {
                listTopicCurrentEvents.Insert(0, new TalkManager.ListItem());
                listTopicCurrentEvents[0].type = TalkManager.ListItemType.NavigationBack;
                listTopicCurrentEvents[0].caption = TopicPreviousString;
                listTopicCurrentEvents[0].listParentItems = listTopicMain;
            }

            if (TalkManager.Instance.ListTopicTellMeAbout[0].type != TalkManager.ListItemType.NavigationBack)
            {
                TalkManager.Instance.ListTopicTellMeAbout.Insert(0, new TalkManager.ListItem());
                TalkManager.Instance.ListTopicTellMeAbout[0].type = TalkManager.ListItemType.NavigationBack;
                TalkManager.Instance.ListTopicTellMeAbout[0].caption = TopicPreviousString;
                TalkManager.Instance.ListTopicTellMeAbout[0].listParentItems = listTopicMain;
            }

            for (int i = 0; i < TalkManager.Instance.ListTopicTellMeAbout.Count; i++)
            {
                if (TalkManager.Instance.ListTopicTellMeAbout[i].questionType == TalkManager.QuestionType.News)
                {
                    TalkManager.Instance.ListTopicTellMeAbout.RemoveAt(i);
                    i--;
                }
                else if (TalkManager.Instance.ListTopicTellMeAbout[i].questionType == TalkManager.QuestionType.Work)
                {
                    TalkManager.Instance.ListTopicTellMeAbout.RemoveAt(i);
                    i--;
                }
                else if (TalkManager.Instance.ListTopicTellMeAbout[i].questionType == TalkManager.QuestionType.WhereAmI)
                {
                    TalkManager.Instance.ListTopicTellMeAbout.RemoveAt(i);
                    i--;
                }
            }

            if (TalkManager.Instance.ListTopicLocation[0].type != TalkManager.ListItemType.NavigationBack)
            {
                TalkManager.Instance.ListTopicLocation.Insert(0, new TalkManager.ListItem());
                TalkManager.Instance.ListTopicLocation[0].type = TalkManager.ListItemType.NavigationBack;
                TalkManager.Instance.ListTopicLocation[0].caption = TopicPreviousString;
                TalkManager.Instance.ListTopicLocation[0].listParentItems = listTopicMain;
            }
        }

        public override void UpdateListboxTopic()
        {

        }

        private void ListboxConversation_OnMouseDoubleClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            if (listboxConversation.SelectedIndex < 0)
                return;

            if (!copyIndexes.Contains(listboxConversation.SelectedIndex))
            {
                copyIndexes.Add(listboxConversation.SelectedIndex);
                drawCopyMSGTime = 1;
            }
            else
            {
                copyIndexes.Remove(listboxConversation.SelectedIndex);
            }
        }

        void SetStartConversation()
        {
            if (listboxConversation != null)
            {
                listboxConversation.ClearItems();
                ListBox.ListItem textLabelNPCGreeting;
                listboxConversation.AddItem(TalkManager.Instance.NPCGreetingText, out textLabelNPCGreeting);
                textLabelNPCGreeting.selectedTextColor = textcolorHighlighted;
                textLabelNPCGreeting.textLabel.HorizontalAlignment = HorizontalAlignment.Right;
                textLabelNPCGreeting.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;
                if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
                {
                    textLabelNPCGreeting.textLabel.TextScale = textScaleModernConversationStyle;
                    textLabelNPCGreeting.textLabel.MaxWidth = (int)(textLabelNPCGreeting.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                    textLabelNPCGreeting.textLabel.BackgroundColor = textcolorAnswerBackgroundModernConversationStyle;
                }
            }
            TalkManager.Instance.StartNewConversation();
        }

        public override void Update()
        {
            if (panelCopyMSG != null)
            {
                if (drawCopyMSGTime > 0)
                {
                    drawCopyMSGTime -= Time.unscaledDeltaTime;
                    panelCopyMSG.Enabled = true;
                }
                else if (panelCopyMSG.Enabled)
                {
                    panelCopyMSG.Enabled = false;
                }
            }
            base.Update();
        }

        protected override void SetupScrollBars()
        {
            // topic list scroll bar (e.g. items in character inventory)
            verticalScrollBarTopic = new VerticalScrollBar();
            verticalScrollBarTopic.Position = new Vector2(107, 108);
            verticalScrollBarTopic.Size = new Vector2(5, 88);
            verticalScrollBarTopic.OnScroll += VerticalScrollBarTopic_OnScroll;
            NativePanel.Components.Add(verticalScrollBarTopic);

            horizontalSliderTopic = new HorizontalSlider();
            horizontalSliderTopic.Position = new Vector2(2, 203);
            horizontalSliderTopic.Size = new Vector2(72, 5);
            horizontalSliderTopic.OnScroll += HorizontalSliderTopic_OnScroll;
            NativePanel.Components.Add(horizontalSliderTopic);

            // conversion list scroll bar
            verticalScrollBarConversation = new VerticalScrollBar();
            verticalScrollBarConversation.Position = new Vector2(308, 108);
            verticalScrollBarConversation.Size = new Vector2(5, 61);
            verticalScrollBarConversation.OnScroll += VerticalScrollBarConversation_OnScroll;
            NativePanel.Components.Add(verticalScrollBarConversation);
        }

        protected override void UpdateScrollBarsTopic()
        {
            verticalScrollBarTopic.DisplayUnits = (int)listboxTopic.Size.y; //Math.Min(maxNumTopicsShown, listboxTopic.Count);
            verticalScrollBarTopic.TotalUnits = listboxTopic.HeightContent();  //listboxTopic.Count;
            verticalScrollBarTopic.ScrollIndex = 0;
            verticalScrollBarTopic.Update();

            horizontalSliderTopic.DisplayUnits = (int)listboxTopic.Size.x; //maxNumCharactersOfTopicShown;
            horizontalSliderTopic.TotalUnits = listboxTopic.WidthContent();  //lengthOfLongestItemInListBox;
            horizontalSliderTopic.ScrollIndex = 0;
            horizontalSliderTopic.Update();
        }

        protected override void UpdateScrollBarConversation()
        {
            verticalScrollBarConversation.DisplayUnits = (int)listboxConversation.Size.y; //Math.Max(5, listboxConversation.HeightContent() / 10);
            verticalScrollBarConversation.TotalUnits = listboxConversation.HeightContent();
            verticalScrollBarConversation.ScrollIndex = 0;
            if (listboxConversation.Count > 0)
                verticalScrollBarConversation.ScrollIndex = listboxConversation.HeightContent() - (int)listboxConversation.Size.y; //listboxConversation.GetItem(listboxConversation.Count - 1).textLabel.TextHeight;
            verticalScrollBarConversation.Update();
        }

        protected override void SetListboxTopics(ref ListBox listboxTopic, List<TalkManager.ListItem> listTopic)
        {
            listCurrentTopics = listTopic;
            listboxTopic.ClearItems();

            for (int i = 0; i < listTopic.Count; i++)
            {
                TalkManager.ListItem item = listTopic[i];
                ListBox.ListItem listboxItem;
                if (item.caption == null) // this is a check to detect problems arising from old save data - where caption end up as null
                {
                    item.caption = item.key; //  just try to take key as caption then (answers might still be broken)
                    if (item.caption == String.Empty)
                        item.caption = TextManager.Instance.GetLocalizedText("resolvingError");
                }
                else if (item.caption == String.Empty)
                {
                    item.caption = TextManager.Instance.GetLocalizedText("resolvingError");
                }
                listboxTopic.AddItem(item.caption, out listboxItem);

                if (item.type == TalkManager.ListItemType.NavigationBack)
                {
                    listboxItem.textColor = textcolorCaptionGotoParentList;
                }
            }

            // compute length of longest item in listbox from current list items...
            //lengthOfLongestItemInListBox = listboxTopic.LengthOfLongestItem();
            widthOfLongestItemInListBox = listboxTopic.WidthContent();

            // update listboxTopic.MaxHorizontalScrollIndex
            //listboxTopic.MaxHorizontalScrollIndex = Math.Max(0, lengthOfLongestItemInListBox - maxNumCharactersOfTopicShown);
            listboxTopic.MaxHorizontalScrollIndex = Math.Max(0, widthOfLongestItemInListBox - (int)listboxTopic.Size.x);

            listboxTopic.Update();
            UpdateScrollBarsTopic();
        }

        void SetTalkCategoryMain()
        {
            SetupTopics();
            SetListboxTopics(ref listboxTopic, listTopicMain);
            listboxTopic.Update();
            UpdateScrollBarsTopic();
        }

        /// <summary>
        /// Gets safe scroll index.
        /// Scroller will be adjust to always be inside display range where possible.
        /// </summary>
        protected override int GetSafeScrollIndex(VerticalScrollBar scroller)
        {
            // Get current scroller index
            int scrollIndex = scroller.ScrollIndex;
            if (scrollIndex < 0)
                scrollIndex = 0;

            // Ensure scroll index within current range
            if (scrollIndex + scroller.DisplayUnits > scroller.TotalUnits)
            {
                scrollIndex = scroller.TotalUnits - scroller.DisplayUnits;
                if (scrollIndex < 0) scrollIndex = 0;
                scroller.Reset(scroller.DisplayUnits, scroller.TotalUnits, scrollIndex);
            }

            return scrollIndex;
        }

        /// <summary>
        /// Gets safe scroll index.
        /// Scroller will be adjust to always be inside display range where possible.
        /// </summary>
        protected override int GetSafeScrollIndex(HorizontalSlider slider)
        {
            // Get current scroller index
            int sliderIndex = slider.ScrollIndex;
            if (sliderIndex < 0)
                sliderIndex = 0;

            // Ensure scroll index within current range
            if (sliderIndex + slider.DisplayUnits > slider.TotalUnits)
            {
                sliderIndex = slider.TotalUnits - slider.DisplayUnits;
                if (sliderIndex < 0) sliderIndex = 0;
                slider.Reset(slider.DisplayUnits, slider.TotalUnits, sliderIndex);
            }

            return sliderIndex;
        }

        protected override void UpdateQuestion(int index)
        {
            if (index < 0 || index >= listboxTopic.Count)
            {
                textlabelPlayerSays.Text = "";
                return;
            }

            TalkManager.ListItem listItem = listCurrentTopics[index];

            if (listItem.type == TalkManager.ListItemType.Item)
                currentQuestion = TalkManager.Instance.GetQuestionText(listItem, selectedTalkTone);
            else
                currentQuestion = "";

            textlabelPlayerSays.Text = currentQuestion;
        }

        protected override void SetQuestionAnswerPairInConversationListbox(string question, string answer)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            ListBox.ListItem textLabelQuestion;
            ListBox.ListItem textLabelAnswer;
            listboxConversation.AddItem(question, out textLabelQuestion);
            textLabelQuestion.textColor = DaggerfallUI.DaggerfallQuestionTextColor;
            textLabelQuestion.selectedTextColor = textcolorHighlighted; // textcolorQuestionHighlighted
            textLabelQuestion.textLabel.HorizontalAlignment = HorizontalAlignment.Left;
            textLabelQuestion.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;

            if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
            {
                textLabelQuestion.textLabel.TextScale = textScaleModernConversationStyle;
                textLabelQuestion.textLabel.MaxWidth = (int)(textLabelQuestion.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                textLabelQuestion.textLabel.BackgroundColor = textcolorQuestionBackgroundModernConversationStyle;
            }
            listboxConversation.AddItem(answer, out textLabelAnswer);
            textLabelAnswer.selectedTextColor = textcolorHighlighted;
            textLabelAnswer.textLabel.HorizontalAlignment = HorizontalAlignment.Right;
            textLabelAnswer.textLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Left;
            //textLabelAnswer.textLabel.BackgroundColor = new Color(0.4f, 0.3f, 0.9f);
            if (DaggerfallUnity.Settings.EnableModernConversationStyleInTalkWindow)
            {
                textLabelAnswer.textLabel.TextScale = textScaleModernConversationStyle;
                textLabelAnswer.textLabel.MaxWidth = (int)(textLabelAnswer.textLabel.MaxWidth * textBlockSizeModernConversationStyle);
                textLabelAnswer.textLabel.BackgroundColor = textcolorAnswerBackgroundModernConversationStyle;
            }

            listboxConversation.SelectedIndex = listboxConversation.Count - 1; // always highlight the new answer

            UpdateScrollBarConversation();
        }

        private void ListboxTopic_OnMouseMove(int x, int y)
        {
            int mouseHoverElement = (y + verticalScrollBarTopic.ScrollIndex) / 7;

            if (mouseHoverElement < 0 || mouseHoverElement >= listboxTopic.Count)
            {
                mouseHoverElement = -1;
            }

            if (mouseHoverElement != lastHover)
            {
                UpdateQuestion(mouseHoverElement);
                lastHover = mouseHoverElement;
            }
        }

        private void ListboxTopic_OnMouseClick(BaseScreenComponent sender, Vector2 pos)
        {
            //If the last hover was outside, make a attempt to grab what the player is pressing on
            if (lastHover < 0 || lastHover >= listboxTopic.Count)
            {
                int mouseHoverElement = (int)((pos.y + verticalScrollBarTopic.ScrollIndex) / 7);

                if (mouseHoverElement >= 0 && mouseHoverElement < listboxTopic.Count)
                {
                    lastHover = mouseHoverElement;
                    UpdateQuestion(lastHover);  //Remove question
                }
            }

            if (inListboxTopicContentUpdate == true || lastHover < 0 || lastHover >= listboxTopic.Count)
            {
                inListboxTopicContentUpdate = false;
                return;
            }

            inListboxTopicContentUpdate = true;

            for (int i = 0; i < listCurrentTopics.Count; i++)
            {
                Debug.Log(i + " " + listCurrentTopics[i].caption);
            }

            //Handle Goodbye
            if (listCurrentTopics[lastHover].caption == GoodByeSring)
            {
                CloseWindow();
            }

            TalkManager.ListItem listItem = listCurrentTopics[lastHover];

            if (listItem.type == TalkManager.ListItemType.NavigationBack)
            {
                if (listItem.listParentItems != null)
                {
                    SetListboxTopics(ref listboxTopic, listItem.listParentItems);
                    DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                    lastHover = -1;
                    UpdateQuestion(lastHover); //Remove question
                }
            }
            else if (listItem.type == TalkManager.ListItemType.ItemGroup)
            {
                if (listItem.listChildItems != null)
                {
                    SetListboxTopics(ref listboxTopic, listItem.listChildItems);
                    DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                    lastHover = -1;
                    UpdateQuestion(lastHover);  //Remove question
                }
            }
            else if (listItem.type == TalkManager.ListItemType.Item)
            {
                string answer = TalkManager.Instance.GetAnswerText(listItem);
                SetQuestionAnswerPairInConversationListbox(currentQuestion, answer);
            }

            inListboxTopicContentUpdate = false;
        }

        private void ListboxTopic_OnMouseLeave(BaseScreenComponent sender)
        {
            UpdateQuestion(-1);
        }

        protected override void VerticalScrollBarTopic_OnScroll()
        {
            // Update scroller
            verticalScrollBarTopic.TotalUnits = listboxTopic.HeightContent(); //listboxTopic.Count;
            int scrollIndex = GetSafeScrollIndex(verticalScrollBarTopic);

            listboxTopic.ScrollIndex = scrollIndex;
            listboxTopic.Update();
        }

        protected override void HorizontalSliderTopic_OnScroll()
        {
            // Update scroller
            horizontalSliderTopic.TotalUnits = widthOfLongestItemInListBox; //lengthOfLongestItemInListBox;
            int horizontalScrollIndex = GetSafeScrollIndex(horizontalSliderTopic); // horizontalSliderTopicWindow.ScrollIndex;

            listboxTopic.HorizontalScrollIndex = horizontalScrollIndex;
            listboxTopic.Update();
        }

        protected override void VerticalScrollBarConversation_OnScroll()
        {
            // Update scroller
            verticalScrollBarConversation.TotalUnits = listboxConversation.HeightContent();
            int scrollIndex = GetSafeScrollIndex(verticalScrollBarConversation);

            listboxConversation.ScrollIndex = scrollIndex;
            listboxConversation.Update();
        }

        protected override void ListBoxTopic_OnScroll()
        {
            int scrollIndex = listboxTopic.ScrollIndex;

            // Update scroller
            verticalScrollBarTopic.SetScrollIndexWithoutRaisingScrollEvent(scrollIndex); // important to use this function here to prevent creating infinite callback loop
            verticalScrollBarTopic.Update();
        }

        protected override void ListBoxConversation_OnScroll()
        {
            int scrollIndex = listboxConversation.ScrollIndex;

            // Update scroller
            verticalScrollBarConversation.SetScrollIndexWithoutRaisingScrollEvent(scrollIndex); // important to use this function here to prevent creating infinite callback loop
            verticalScrollBarConversation.Update();
        }

        protected override void ButtonTonePolite_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Polite;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(lastHover);
        }

        protected override void ButtonToneNormal_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Normal;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(lastHover);
        }

        protected override void ButtonToneBlunt_OnClickHandler(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            selectedTalkTone = TalkTone.Blunt;
            if (TalkToneToIndex(selectedTalkTone) == toneLastUsed)
                return;
            toneLastUsed = TalkToneToIndex(selectedTalkTone);
            UpdateCheckboxes();
            UpdateQuestion(lastHover);
        }
    }
}