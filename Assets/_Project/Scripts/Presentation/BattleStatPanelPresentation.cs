using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticaUniversalis.Presentation
{
    public enum BattleArmorHudState
    {
        None = 0,
        Half = 1,
        Full = 2
    }

    public sealed class BattlePlayerCardData
    {
        public string PlayerId;
        public string DisplayName;
        public int JerseyNumber;
        public int Health = 100;
        public int Kills;
        public int Deaths;
        public int RoundKills;
        public int Money;
        public string PrimaryWeaponName;
        public string SecondaryWeaponName;
        public string PrimaryWeaponId;
        public string SecondaryWeaponId;
        public bool HasFlash;
        public bool HasSmoke;
        public bool HasHe;
        public bool HasFire;
        /// <summary>True = CT incendiary silhouette; false = T molotov. Tint still follows holder side.</summary>
        public bool FireIsIncendiary;
        public bool HasDefuseKit;
        /// <summary>T-side bomb carrier flag for utility slot 4.</summary>
        public bool HasBomb;
        public BattleArmorHudState Armor;
        public float Mentality = 50f;
        public float Form = 50f;
        public float Stamina = 100f;
        public bool Alive = true;
    }

    public sealed class BattlePlayerCardView
    {
        public Image Root;
        public Image FrostOverlay;
        /// <summary>True = left column (number bar outer-left, HP on the right).</summary>
        public bool LeftSide;
        public Text HealthText;
        public Text[] RoundKillStars;
        public Image KillIcon;
        public Text KillText;
        public Image DeathIcon;
        public Text DeathText;
        public Text PrimaryWeapon;
        public Text SecondaryWeapon;
        public Image PrimaryWeaponIcon;
        public Image SecondaryWeaponIcon;
        public Text MoneyText;
        public Image[] UtilitySlots;
        public Image MentalityIcon;
        public Text MentalityText;
        public Image FormIcon;
        public Text FormText;
        public Image StaminaIcon;
        public Text StaminaText;
        public Text PlayerName;
        public Image NumberBar;
        public Text NumberText;
        public string PlayerId = string.Empty;
    }

    /// <summary>
    /// Scoreboard cards matching mock layouts:
    /// left column = T-style mock (number bar outer-left);
    /// right column = CT-style mock (number bar outer-right).
    /// Colors: CT #19305e/#729cca · T #776536/#e0a526.
    /// </summary>
    public static class BattleStatPanelPresentation
    {
        public static readonly Color CtLight = Hex("729cca");
        public static readonly Color CtDark = Hex("19305e");
        public static readonly Color TLight = Hex("e0a526");
        public static readonly Color TDark = Hex("776536");

        /// <summary>Normalized gap between player cards (and between team header and first card).</summary>
        public const float PlayerStackGap = 0.030f;

        private static readonly Color TextPrimary = Color.white;
        private static readonly Color TextMuted = new Color(0.92f, 0.93f, 0.95f, 1f);
        private static readonly Color MoneyGreen = new Color(0.30f, 0.95f, 0.40f, 1f);
        private static readonly Color DeadRowBg = new Color(0.22f, 0.22f, 0.24f, 1f);
        private static readonly Color DeadText = new Color(0.62f, 0.63f, 0.66f, 1f);
        private static readonly Color DeadAccent = new Color(0.50f, 0.51f, 0.53f, 1f);
        // Matches control-bar frost: translucent light veil so the map reads through.
        private static readonly Color FrostGlass = new Color(0.78f, 0.84f, 0.92f, 0.42f);

        private static Sprite[] _utilityIcons;
        private static Sprite _utilityFlashIcon;
        private static Sprite _utilitySmokeIcon;
        private static Sprite _utilityHeIcon;
        private static Sprite _utilityMolotovIcon;
        private static Sprite _utilityIncendiaryIcon;
        private static Sprite _utilityKitIcon;
        private static Sprite _utilityBombIcon;
        private static Sprite _solidSprite;

        public static void SetUtilityIcons(Sprite[] icons) => _utilityIcons = icons;

        /// <summary>
        /// Nade silhouettes (white). Holder-side accent tints them; fire shape is molotov vs incendiary.
        /// </summary>
        public static void SetNadeIcons(
            Sprite flash, Sprite smoke, Sprite he, Sprite molotov, Sprite incendiary)
        {
            _utilityFlashIcon = flash;
            _utilitySmokeIcon = smoke;
            _utilityHeIcon = he;
            _utilityMolotovIcon = molotov;
            _utilityIncendiaryIcon = incendiary;
            _utilityIcons = new[] { flash, smoke, he, molotov };
        }

        public static void SetKitAndBombIcons(Sprite defuseKit, Sprite bomb)
        {
            _utilityKitIcon = defuseKit;
            _utilityBombIcon = bomb;
        }

        /// <summary>Scoreboard / kill-feed weapon silhouette scale vs layout rect.</summary>
        public const float WeaponIconScale = 0.80f;
        public static Color PanelColor(bool isTerrorist) => isTerrorist ? TDark : CtDark;
        public static Color AccentColor(bool isTerrorist) => isTerrorist ? TLight : CtLight;
        public static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 1f);

        /// <summary>Paints an Image as a fully opaque flat fill (needed when no sprite was assigned).</summary>
        public static void ApplyOpaqueFill(Image image, Color color)
        {
            if (image == null)
                return;
            image.sprite = SolidSprite();
            image.type = Image.Type.Simple;
            image.color = Opaque(color);
        }

        /// <summary>1x1 white sprite so Image.color actually paints opaque panels.</summary>
        private static Sprite SolidSprite()
        {
            if (_solidSprite != null)
                return _solidSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);
            _solidSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            _solidSprite.name = "BattleStatSolid";
            return _solidSprite;
        }

        public static BattlePlayerCardView CreateCard(
            Transform parent,
            bool leftSide,
            int slotIndex,
            Font font,
            Sprite killIcon,
            Sprite deathIcon,
            Sprite assistIcon,
            Sprite mentalityIcon,
            Sprite formIcon,
            Sprite staminaIcon,
            Action<bool, int> onEnter,
            Action<bool, int> onExit)
        {
            _ = assistIcon;

            Sprite solid = SolidSprite();
            Image root = CreateUiObject<Image>("StatCard" + slotIndex, parent);
            root.sprite = solid;
            // Remaining HP paints as an opaque horizontal fill from the number-bar side;
            // frost / lost strip grows from the HP (map-facing) side.
            root.type = Image.Type.Filled;
            root.fillMethod = Image.FillMethod.Horizontal;
            root.fillOrigin = leftSide
                ? (int)Image.OriginHorizontal.Left
                : (int)Image.OriginHorizontal.Right;
            root.fillAmount = 1f;
            root.color = leftSide ? TDark : CtDark;
            root.raycastTarget = true;

            var trigger = root.gameObject.AddComponent<EventTrigger>();
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ => onEnter?.Invoke(leftSide, slotIndex));
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => onExit?.Invoke(leftSide, slotIndex));

            // Lost-HP strip: frosted veil growing from the HP side (left card → from right; right card → from left).
            Image frost = CreateUiObject<Image>("Frost", root.transform);
            Stretch(frost.rectTransform);
            frost.sprite = solid;
            frost.type = Image.Type.Filled;
            frost.fillMethod = Image.FillMethod.Horizontal;
            frost.fillOrigin = leftSide
                ? (int)Image.OriginHorizontal.Right
                : (int)Image.OriginHorizontal.Left;
            frost.fillAmount = 0f;
            frost.color = FrostGlass;
            frost.raycastTarget = false;
            frost.transform.SetAsFirstSibling();

            var view = new BattlePlayerCardView
            {
                Root = root,
                FrostOverlay = frost,
                LeftSide = leftSide,
                RoundKillStars = new Text[5],
                UtilitySlots = new Image[6]
            };

            // Number bar on outer edge (light), numeral in dark.
            float bar0 = leftSide ? 0f : 0.90f;
            float bar1 = leftSide ? 0.10f : 1f;
            view.NumberBar = CreateUiObject<Image>("NumberBar", root.transform);
            SetRect(view.NumberBar.rectTransform, new Vector2(bar0, 0f), new Vector2(bar1, 1f));
            view.NumberBar.sprite = solid;
            view.NumberBar.type = Image.Type.Simple;
            view.NumberBar.color = leftSide ? TLight : CtLight;
            view.NumberBar.raycastTarget = false;
            view.NumberText = CreateText("Num", view.NumberBar.transform, 28, TextAnchor.MiddleCenter,
                leftSide ? TDark : CtDark, font);
            Stretch(view.NumberText.rectTransform);
            view.NumberText.fontStyle = FontStyle.Bold;
            view.NumberText.raycastTarget = false;

            if (leftSide)
                BuildLeftLayout(view, root.transform, font, killIcon, deathIcon, mentalityIcon, formIcon, staminaIcon);
            else
                BuildRightLayout(view, root.transform, font, killIcon, deathIcon, mentalityIcon, formIcon, staminaIcon);

            return view;
        }

        /// <summary>
        /// Left column = mirror of right mock:
        /// number bar outer-left · name/conds · money · guns/stars/KD · HP outer-right.
        /// </summary>
        private static void BuildLeftLayout(
            BattlePlayerCardView view, Transform root, Font font,
            Sprite killIcon, Sprite deathIcon,
            Sprite mentalityIcon, Sprite formIcon, Sprite staminaIcon)
        {
            // HP on the map-facing (right) edge.
            view.HealthText = CreateText("Health", root, 40, TextAnchor.MiddleCenter, TextPrimary, font);
            SetRect(view.HealthText.rectTransform, new Vector2(0.80f, 0.08f), new Vector2(0.99f, 0.96f));
            view.HealthText.fontStyle = FontStyle.Bold;

            // Name top, tight against number bar.
            view.PlayerName = CreateText("Name", root, 20, TextAnchor.MiddleLeft, TextPrimary, font);
            SetRect(view.PlayerName.rectTransform, new Vector2(0.115f, 0.70f), new Vector2(0.42f, 0.96f));
            view.PlayerName.fontStyle = FontStyle.Bold;

            // Weapons point toward HP (right): icon only; upper = primary, lower = secondary.
            BuildWeaponSlot(view, root, font, leftSide: true,
                textMin: new Vector2(0.40f, 0.70f), textMax: new Vector2(0.78f, 0.94f),
                iconMin: new Vector2(0.40f, 0.72f), iconMax: new Vector2(0.78f, 0.94f),
                primary: true);
            BuildWeaponSlot(view, root, font, leftSide: true,
                textMin: new Vector2(0.40f, 0.48f), textMax: new Vector2(0.78f, 0.72f),
                iconMin: new Vector2(0.40f, 0.50f), iconMax: new Vector2(0.78f, 0.72f),
                primary: false);

            // Stars under weapons, grow toward HP.
            BuildStars(view, root, font, 0.48f, 0.78f, 0.28f, 0.48f, growFromRight: true, TLight);

            // Money left of HP / right of conditions cluster.
            view.MoneyText = CreateText("Money", root, 18, TextAnchor.MiddleRight, MoneyGreen, font);
            SetRect(view.MoneyText.rectTransform, new Vector2(0.30f, 0.28f), new Vector2(0.50f, 0.50f));
            view.MoneyText.fontStyle = FontStyle.Bold;

            // 心態／手感／體力：靠編號條，圖示貼外側、數值緊鄰。
            BuildConditionColumn(view, root, font, mentalityIcon, formIcon, staminaIcon,
                0.115f, 0.32f, 0.06f, 0.66f, iconOnRight: false);

            // K/D near HP bottom.
            BuildKdRow(view, root, font, killIcon, deathIcon, 0.52f, 0.78f, 0.02f, 0.22f);

            // Utility placeholders (icons pending) between K/D and money.
            BuildUtilityRow(view, root, 0.32f, 0.52f, 0.02f, 0.18f, false);
        }

        /// <summary>
        /// Right column = reference mock:
        /// HP left · primary/secondary icons · stars · K/D · money · conds · name · number bar right.
        /// Utility / armor / kit slots reserved but empty for now.
        /// </summary>
        private static void BuildRightLayout(
            BattlePlayerCardView view, Transform root, Font font,
            Sprite killIcon, Sprite deathIcon,
            Sprite mentalityIcon, Sprite formIcon, Sprite staminaIcon)
        {
            // HP on the map-facing (left) edge — large.
            view.HealthText = CreateText("Health", root, 40, TextAnchor.MiddleCenter, TextPrimary, font);
            SetRect(view.HealthText.rectTransform, new Vector2(0.01f, 0.08f), new Vector2(0.20f, 0.96f));
            view.HealthText.fontStyle = FontStyle.Bold;

            // Primary / secondary icons: barrel toward HP (left) → flip icon.
            BuildWeaponSlot(view, root, font, leftSide: false,
                textMin: new Vector2(0.20f, 0.70f), textMax: new Vector2(0.58f, 0.94f),
                iconMin: new Vector2(0.20f, 0.72f), iconMax: new Vector2(0.58f, 0.94f),
                primary: true);
            BuildWeaponSlot(view, root, font, leftSide: false,
                textMin: new Vector2(0.20f, 0.48f), textMax: new Vector2(0.58f, 0.72f),
                iconMin: new Vector2(0.20f, 0.50f), iconMax: new Vector2(0.58f, 0.72f),
                primary: false);

            // Name top-right, tight against number bar.
            view.PlayerName = CreateText("Name", root, 20, TextAnchor.MiddleRight, TextPrimary, font);
            SetRect(view.PlayerName.rectTransform, new Vector2(0.58f, 0.70f), new Vector2(0.885f, 0.96f));
            view.PlayerName.fontStyle = FontStyle.Bold;

            // Round-kill stars under weapons.
            BuildStars(view, root, font, 0.20f, 0.50f, 0.28f, 0.48f, growFromRight: false, CtLight);

            // Green money right of stars, left of condition stack.
            view.MoneyText = CreateText("Money", root, 18, TextAnchor.MiddleLeft, MoneyGreen, font);
            SetRect(view.MoneyText.rectTransform, new Vector2(0.50f, 0.28f), new Vector2(0.70f, 0.50f));
            view.MoneyText.fontStyle = FontStyle.Bold;

            // 心態／手感／體力：靠右側編號條，[icon][value] 緊排。
            BuildConditionColumn(view, root, font, mentalityIcon, formIcon, staminaIcon,
                0.68f, 0.885f, 0.06f, 0.66f, iconOnRight: false);

            // K/D bottom-left beside HP.
            BuildKdRow(view, root, font, killIcon, deathIcon, 0.20f, 0.46f, 0.02f, 0.22f);

            // Utility placeholders (雷煙火閃鉗甲) — slots only; icons pending.
            BuildUtilityRow(view, root, 0.46f, 0.68f, 0.02f, 0.18f, false);
        }

        public static void LayoutCardInColumn(BattlePlayerCardView view, int index, int count = 5)
        {
            if (view?.Root == null)
                return;
            // Full column width so cards match the team header bar.
            const float topStart = 1f;
            const float bottomPad = 0f;
            float gap = PlayerStackGap;
            float rowHeight = (topStart - bottomPad - (count - 1) * gap) / count;
            float top = topStart - index * (rowHeight + gap);
            float bottom = top - rowHeight;
            SetRect(view.Root.rectTransform, new Vector2(0f, bottom), new Vector2(1f, top));
        }

        /// <summary>
        /// General-mode side column on screen:
        /// header and player stack share the same width;
        /// gap between header and first card equals <see cref="PlayerStackGap"/> between cards.
        /// </summary>
        public static void GetGeneralColumnScreenRects(
            bool leftSide,
            out Vector2 headerMin,
            out Vector2 headerMax,
            out Vector2 panelMin,
            out Vector2 panelMax)
        {
            float x0 = leftSide ? 0.006f : 0.795f;
            float x1 = leftSide ? 0.205f : 0.994f;
            const float colTop = 0.758f;
            const float colBottom = 0.018f;
            const float headerHeight = 0.048f;
            float gap = PlayerStackGap;

            float headerTop = colTop;
            float headerBottom = headerTop - headerHeight;
            float panelH = (headerBottom - colBottom) / (1f + gap);
            float panelBottom = colBottom;
            float panelTop = panelBottom + panelH;

            headerMin = new Vector2(x0, headerBottom);
            headerMax = new Vector2(x1, headerTop);
            panelMin = new Vector2(x0, panelBottom);
            panelMax = new Vector2(x1, panelTop);
        }

        /// <summary>
        /// Three header fields with equal width, equal gaps, and Best Fit at 90% of bar height.
        /// Left order: club · money · CP. Right order mirrored.
        /// </summary>
        public static void LayoutTeamHeaderFields(
            Text club, Text money, Text cp, bool leftSide)
        {
            const float edge = 0.02f;
            const float gap = 0.02f;
            // 90% of bar height, vertically centered.
            const float y0 = 0.05f;
            const float y1 = 0.95f;
            float slot = (1f - edge * 2f - gap * 2f) / 3f;

            Text[] fields = leftSide
                ? new[] { club, money, cp }
                : new[] { cp, money, club };
            TextAnchor[] anchors = leftSide
                ? new[] { TextAnchor.MiddleLeft, TextAnchor.MiddleCenter, TextAnchor.MiddleRight }
                : new[] { TextAnchor.MiddleLeft, TextAnchor.MiddleCenter, TextAnchor.MiddleRight };

            for (int i = 0; i < 3; i++)
            {
                Text field = fields[i];
                if (field == null)
                    continue;
                float x0 = edge + i * (slot + gap);
                float x1 = x0 + slot;
                SetRect(field.rectTransform, new Vector2(x0, y0), new Vector2(x1, y1));
                field.alignment = anchors[i];
                field.fontStyle = FontStyle.Bold;
                field.horizontalOverflow = HorizontalWrapMode.Overflow;
                field.verticalOverflow = VerticalWrapMode.Truncate;
                field.resizeTextForBestFit = true;
                field.resizeTextMinSize = 8;
                field.resizeTextMaxSize = 96;
            }
        }

        public static void Bind(BattlePlayerCardView view, BattlePlayerCardData data, Color panelDark, Color accentLight)
        {
            if (view?.Root == null || data == null)
                return;

            view.PlayerId = data.PlayerId ?? string.Empty;
            bool alive = data.Alive;
            Color primary = alive ? TextPrimary : DeadText;
            Color muted = alive ? TextMuted : DeadText;
            Color money = alive ? MoneyGreen : DeadText;
            Color panel = alive ? Opaque(panelDark) : DeadRowBg;
            Color accent = alive ? Opaque(accentLight) : DeadAccent;

            int hp = alive ? Mathf.Clamp(data.Health, 0, 100) : 0;
            if (view.HealthText != null)
            {
                view.HealthText.text = hp.ToString();
                view.HealthText.color = primary;
            }

            ApplyHealthFrost(view, alive, hp, panel);

            int litStars = alive ? Mathf.Clamp(data.RoundKills, 0, 5) : 0;
            if (view.RoundKillStars != null)
            {
                for (int i = 0; i < view.RoundKillStars.Length; i++)
                {
                    Text star = view.RoundKillStars[i];
                    if (star == null)
                        continue;
                    bool show = i < litStars;
                    star.gameObject.SetActive(show);
                    if (show)
                        star.color = accent;
                }
            }

            SetIconText(view.KillIcon, view.KillText, data.Kills.ToString(), primary);
            SetIconText(view.DeathIcon, view.DeathText, data.Deaths.ToString(), primary);

            if (view.PrimaryWeapon != null || view.PrimaryWeaponIcon != null)
                BindWeaponSlot(view.PrimaryWeapon, view.PrimaryWeaponIcon,
                    data.PrimaryWeaponId, data.PrimaryWeaponName, primary, view.LeftSide);
            if (view.SecondaryWeapon != null || view.SecondaryWeaponIcon != null)
                BindWeaponSlot(view.SecondaryWeapon, view.SecondaryWeaponIcon,
                    data.SecondaryWeaponId, data.SecondaryWeaponName, muted, view.LeftSide);
            if (view.MoneyText != null)
            {
                view.MoneyText.text = "$" + Mathf.Max(0, data.Money);
                view.MoneyText.color = money;
            }

            BindUtilities(view, data, alive, accent);
            SetIconText(view.MentalityIcon, view.MentalityText, Mathf.RoundToInt(data.Mentality).ToString(), primary);
            SetIconText(view.FormIcon, view.FormText, Mathf.RoundToInt(data.Form).ToString(), primary);
            SetIconText(view.StaminaIcon, view.StaminaText, Mathf.RoundToInt(data.Stamina).ToString(), primary);

            if (view.PlayerName != null)
            {
                view.PlayerName.text = data.DisplayName ?? string.Empty;
                view.PlayerName.color = primary;
            }
            if (view.NumberBar != null)
                view.NumberBar.color = accent;
            if (view.NumberText != null)
            {
                view.NumberText.text = data.JerseyNumber > 0 ? data.JerseyNumber.ToString() : string.Empty;
                view.NumberText.color = alive ? panel : DeadText;
            }
        }

        private static void ApplyHealthFrost(BattlePlayerCardView view, bool alive, int hp, Color panel)
        {
            if (view.Root == null)
                return;

            if (!alive)
            {
                // Dead: full opaque gray card; no frost strip.
                view.Root.type = Image.Type.Simple;
                view.Root.fillAmount = 1f;
                view.Root.color = DeadRowBg;
                if (view.FrostOverlay != null)
                    view.FrostOverlay.fillAmount = 0f;
                return;
            }

            float remain = Mathf.Clamp01(hp / 100f);
            float lost = 1f - remain;

            // Opaque team color remains from the number-bar side; lost strip eats from HP side.
            // Left card: shrink from right. Right card: shrink from left.
            view.Root.type = Image.Type.Filled;
            view.Root.fillMethod = Image.FillMethod.Horizontal;
            view.Root.fillOrigin = view.LeftSide
                ? (int)Image.OriginHorizontal.Left
                : (int)Image.OriginHorizontal.Right;
            view.Root.fillAmount = remain;
            view.Root.color = panel;

            if (view.FrostOverlay == null)
                return;

            view.FrostOverlay.type = Image.Type.Filled;
            view.FrostOverlay.fillMethod = Image.FillMethod.Horizontal;
            view.FrostOverlay.fillOrigin = view.LeftSide
                ? (int)Image.OriginHorizontal.Right
                : (int)Image.OriginHorizontal.Left;
            view.FrostOverlay.color = FrostGlass;
            view.FrostOverlay.fillAmount = lost;
        }

        public static void Clear(BattlePlayerCardView view)
        {
            if (view == null)
                return;
            view.PlayerId = string.Empty;
            if (view.Root != null)
            {
                view.Root.type = Image.Type.Filled;
                view.Root.fillAmount = 1f;
                view.Root.color = CtDark;
            }
            if (view.NumberBar != null)
                view.NumberBar.color = CtLight;
            if (view.HealthText != null)
                view.HealthText.text = string.Empty;
            if (view.FrostOverlay != null)
                view.FrostOverlay.fillAmount = 0f;
            if (view.PlayerName != null)
                view.PlayerName.text = string.Empty;
            if (view.PrimaryWeapon != null)
                view.PrimaryWeapon.text = string.Empty;
            if (view.SecondaryWeapon != null)
                view.SecondaryWeapon.text = string.Empty;
            if (view.PrimaryWeaponIcon != null)
            {
                view.PrimaryWeaponIcon.enabled = false;
                view.PrimaryWeaponIcon.sprite = null;
            }
            if (view.SecondaryWeaponIcon != null)
            {
                view.SecondaryWeaponIcon.enabled = false;
                view.SecondaryWeaponIcon.sprite = null;
            }
            if (view.MoneyText != null)
                view.MoneyText.text = string.Empty;
            if (view.NumberText != null)
                view.NumberText.text = string.Empty;
            if (view.RoundKillStars != null)
            {
                for (int i = 0; i < view.RoundKillStars.Length; i++)
                {
                    if (view.RoundKillStars[i] != null)
                        view.RoundKillStars[i].gameObject.SetActive(false);
                }
            }
            SetIconText(view.KillIcon, view.KillText, string.Empty, TextPrimary);
            SetIconText(view.DeathIcon, view.DeathText, string.Empty, TextPrimary);
            SetIconText(view.MentalityIcon, view.MentalityText, string.Empty, TextPrimary);
            SetIconText(view.FormIcon, view.FormText, string.Empty, TextPrimary);
            SetIconText(view.StaminaIcon, view.StaminaText, string.Empty, TextPrimary);
            if (view.UtilitySlots == null)
                return;
            for (int i = 0; i < view.UtilitySlots.Length; i++)
            {
                Image slot = view.UtilitySlots[i];
                if (slot == null)
                    continue;
                slot.enabled = false;
                slot.sprite = null;
                slot.color = Color.clear;
            }
        }

        private static void BuildWeaponSlot(
            BattlePlayerCardView view, Transform root, Font font, bool leftSide,
            Vector2 textMin, Vector2 textMax, Vector2 iconMin, Vector2 iconMax, bool primary)
        {
            TextAnchor align = leftSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            Text label = CreateText(primary ? "Primary" : "Secondary", root, 17, align,
                primary ? TextPrimary : TextMuted, font);
            SetRect(label.rectTransform, textMin, textMax);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            Image icon = CreateUiObject<Image>((primary ? "Primary" : "Secondary") + "Icon", root);
            SetRect(icon.rectTransform, iconMin, iconMax);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;
            // Right column HP is on the left — flip so barrel points at HP; both at 80% size.
            float sx = leftSide ? WeaponIconScale : -WeaponIconScale;
            icon.rectTransform.localScale = new Vector3(sx, WeaponIconScale, 1f);

            if (primary)
            {
                view.PrimaryWeapon = label;
                view.PrimaryWeaponIcon = icon;
            }
            else
            {
                view.SecondaryWeapon = label;
                view.SecondaryWeaponIcon = icon;
            }
        }

        private static void BindWeaponSlot(
            Text label, Image icon, string weaponId, string weaponName,
            Color tint, bool leftSide)
        {
            Sprite sprite = WeaponIconAtlas.Get(weaponId);
            bool useIcon = sprite != null;
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = tint;
                icon.enabled = useIcon;
                float sx = leftSide ? WeaponIconScale : -WeaponIconScale;
                icon.rectTransform.localScale = new Vector3(sx, WeaponIconScale, 1f);
            }
            if (label == null)
                return;
            label.color = tint;
            RectTransform labelRt = label.rectTransform;
            if (useIcon)
            {
                label.text = string.Empty;
                return;
            }

            label.text = FormatWeaponLabel(weaponName);
            if (leftSide)
                SetRect(labelRt, new Vector2(0.40f, labelRt.anchorMin.y), new Vector2(0.78f, labelRt.anchorMax.y));
            else
                SetRect(labelRt, new Vector2(0.20f, labelRt.anchorMin.y), new Vector2(0.58f, labelRt.anchorMax.y));
        }

        private static string FormatWeaponLabel(string weaponName) =>
            string.IsNullOrWhiteSpace(weaponName) ? "—" : weaponName.Trim();

        private static void BindUtilities(
            BattlePlayerCardView view, BattlePlayerCardData data, bool alive, Color teamAccent)
        {
            if (view.UtilitySlots == null || view.UtilitySlots.Length < 6)
                return;

            bool[] owned =
            {
                data.HasFlash, data.HasSmoke, data.HasHe, data.HasFire,
                data.HasBomb || data.HasDefuseKit, data.Armor != BattleArmorHudState.None
            };

            for (int i = 0; i < 6; i++)
            {
                Image slot = view.UtilitySlots[i];
                if (slot == null)
                    continue;
                Sprite icon = ResolveUtilityIcon(i, data);
                bool ownedFlag = alive && owned[i];
                bool show = ownedFlag && (icon != null || (i == 5));
                slot.enabled = show;
                if (!show)
                {
                    slot.sprite = null;
                    slot.color = Color.clear;
                    continue;
                }
                slot.sprite = icon;
                if (icon == null)
                {
                    // Armor placeholder until armor sprite exists.
                    slot.color = new Color(0.75f, 0.75f, 0.8f, 0.9f);
                }
                else if (i == 4 && data.HasDefuseKit && !data.HasBomb)
                {
                    // Sprite is already tinted #1b99ff; keep full opacity.
                    slot.color = Color.white;
                }
                else if (i <= 3)
                {
                    // Flash / smoke / HE / fire: white silhouette tinted by holder side.
                    slot.color = Opaque(teamAccent);
                }
                else
                {
                    slot.color = i == 5 && data.Armor == BattleArmorHudState.Half
                        ? new Color(1f, 1f, 1f, 0.55f)
                        : Color.white;
                }
            }
        }

        private static Sprite ResolveUtilityIcon(int slot, BattlePlayerCardData data)
        {
            switch (slot)
            {
                case 0:
                    return _utilityFlashIcon ?? (_utilityIcons != null && _utilityIcons.Length > 0
                        ? _utilityIcons[0] : null);
                case 1:
                    return _utilitySmokeIcon ?? (_utilityIcons != null && _utilityIcons.Length > 1
                        ? _utilityIcons[1] : null);
                case 2:
                    return _utilityHeIcon ?? (_utilityIcons != null && _utilityIcons.Length > 2
                        ? _utilityIcons[2] : null);
                case 3:
                    if (data.FireIsIncendiary)
                        return _utilityIncendiaryIcon ?? _utilityMolotovIcon;
                    return _utilityMolotovIcon ?? _utilityIncendiaryIcon
                        ?? (_utilityIcons != null && _utilityIcons.Length > 3 ? _utilityIcons[3] : null);
                case 4:
                    if (data.HasBomb)
                        return _utilityBombIcon;
                    if (data.HasDefuseKit)
                        return _utilityKitIcon;
                    return null;
                default:
                    return null;
            }
        }

        private static void BuildStars(
            BattlePlayerCardView view, Transform root, Font font,
            float x0, float x1, float y0, float y1, bool growFromRight, Color tint)
        {
            float span = (x1 - x0) / 5f;
            for (int s = 0; s < 5; s++)
            {
                float a = growFromRight ? x1 - (s + 1) * span : x0 + s * span;
                float b = growFromRight ? x1 - s * span : x0 + (s + 1) * span;
                Text star = CreateText("Star" + s, root, 14, TextAnchor.MiddleCenter, tint, font);
                SetRect(star.rectTransform, new Vector2(a, y0), new Vector2(b, y1));
                star.text = "★";
                star.raycastTarget = false;
                star.gameObject.SetActive(false);
                view.RoundKillStars[s] = star;
            }
        }

        private static void BuildUtilityRow(
            BattlePlayerCardView view, Transform root,
            float x0, float x1, float y0, float y1, bool reverse)
        {
            for (int u = 0; u < 6; u++)
            {
                float t0 = u / 6f;
                float t1 = (u + 1) / 6f;
                float a = Mathf.Lerp(x0, x1, reverse ? 1f - t1 : t0);
                float b = Mathf.Lerp(x0, x1, reverse ? 1f - t0 : t1);
                Image util = CreateUiObject<Image>("Util" + u, root);
                SetRect(util.rectTransform, new Vector2(a, y0), new Vector2(b, y1));
                util.color = Color.clear;
                util.preserveAspect = true;
                util.raycastTarget = false;
                util.enabled = false;
                view.UtilitySlots[u] = util;
            }
        }

        private static void BuildKdRow(
            BattlePlayerCardView view, Transform root, Font font,
            Sprite killIcon, Sprite deathIcon,
            float x0, float x1, float y0, float y1)
        {
            Image row = CreateUiObject<Image>("KdRow", root);
            row.color = Color.clear;
            row.raycastTarget = false;
            SetRect(row.rectTransform, new Vector2(x0, y0), new Vector2(x1, y1));

            // Tight pack: [Kicon][21][Dicon][12] — no large gap.
            view.KillIcon = CreateUiObject<Image>("KillIcon", row.transform);
            view.KillIcon.sprite = killIcon;
            view.KillIcon.preserveAspect = true;
            view.KillIcon.raycastTarget = false;
            SetRect(view.KillIcon.rectTransform, new Vector2(0.00f, 0.10f), new Vector2(0.18f, 0.90f));

            view.KillText = CreateText("Kill", row.transform, 14, TextAnchor.MiddleLeft, TextPrimary, font);
            SetRect(view.KillText.rectTransform, new Vector2(0.18f, 0f), new Vector2(0.46f, 1f));
            view.KillText.raycastTarget = false;

            view.DeathIcon = CreateUiObject<Image>("DeathIcon", row.transform);
            view.DeathIcon.sprite = deathIcon;
            view.DeathIcon.preserveAspect = true;
            view.DeathIcon.raycastTarget = false;
            SetRect(view.DeathIcon.rectTransform, new Vector2(0.50f, 0.10f), new Vector2(0.68f, 0.90f));

            view.DeathText = CreateText("Death", row.transform, 14, TextAnchor.MiddleLeft, TextPrimary, font);
            SetRect(view.DeathText.rectTransform, new Vector2(0.68f, 0f), new Vector2(1f, 1f));
            view.DeathText.raycastTarget = false;
        }

        private static void BuildConditionColumn(
            BattlePlayerCardView view, Transform root, Font font,
            Sprite mentalityIcon, Sprite formIcon, Sprite staminaIcon,
            float x0, float x1, float y0, float y1, bool iconOnRight)
        {
            Image col = CreateUiObject<Image>("Conditions", root);
            col.color = Color.clear;
            col.raycastTarget = false;
            SetRect(col.rectTransform, new Vector2(x0, y0), new Vector2(x1, y1));

            // Top→bottom: 心態 手感 體力
            BuildConditionRow(col.transform, font, mentalityIcon, "Mentality", 0.68f, 1.00f, iconOnRight,
                out view.MentalityIcon, out view.MentalityText);
            BuildConditionRow(col.transform, font, formIcon, "Form", 0.34f, 0.66f, iconOnRight,
                out view.FormIcon, out view.FormText);
            BuildConditionRow(col.transform, font, staminaIcon, "Stamina", 0.00f, 0.32f, iconOnRight,
                out view.StaminaIcon, out view.StaminaText);
        }

        private static void BuildConditionRow(
            Transform col, Font font, Sprite iconSprite, string name,
            float y0, float y1, bool iconOnRight, out Image icon, out Text value)
        {
            // Tight pack: icon ~36% of a narrow edge column, value immediately beside it.
            const float iconSpan = 0.36f;
            const float gap = 0.01f;

            icon = CreateUiObject<Image>(name + "Icon", col);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            value = CreateText(name, col, 14,
                iconOnRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                TextPrimary, font);
            value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Overflow;

            if (iconOnRight)
            {
                SetRect(value.rectTransform, new Vector2(0f, y0), new Vector2(1f - iconSpan - gap, y1));
                SetRect(icon.rectTransform, new Vector2(1f - iconSpan, y0), new Vector2(1f, y1));
            }
            else
            {
                SetRect(icon.rectTransform, new Vector2(0f, y0), new Vector2(iconSpan, y1));
                SetRect(value.rectTransform, new Vector2(iconSpan + gap, y0), new Vector2(1f, y1));
            }
        }

        private static void SetIconText(Image icon, Text text, string value, Color tint)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
                text.color = tint;
                text.enabled = true;
            }
            if (icon != null)
            {
                icon.color = tint;
                icon.enabled = true;
            }
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }

        private static void AddPointerTrigger(
            EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private static T CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static Text CreateText(
            string name, Transform parent, int size, TextAnchor alignment, Color color, Font font)
        {
            Text text = CreateUiObject<Text>(name, parent);
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
