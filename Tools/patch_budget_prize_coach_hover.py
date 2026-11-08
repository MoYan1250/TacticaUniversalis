# -*- coding: utf-8 -*-
"""Update monthly budget UI: prize income, coach salaries, no facility opex, hover details."""
from pathlib import Path

TARGET = Path(__file__).resolve().parents[1] / "Assets/_Project/Scripts/Presentation/CareerDemoController.cs"


def must_replace(text: str, old: str, new: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing:\n{old[:220]!r}")
    return text.replace(old, new, 1)


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    if "每月收支" not in text:
        raise RuntimeError("UTF-8 / budget panel missing")

    text = must_replace(
        text,
        """            FillBudgetBlock(
                incomeBlock.transform,
                "收入",
                Positive,
                new[]
                {
                    (BudgetSponsorIncomeLabel(budget),
                        budget.sponsorIncome, budget.hasSponsor ? Positive : TextMuted, true),
                    ("商品銷售", budget.merchandiseIncome, Positive, true)
                },
                ("收入合計", budget.IncomeTotal, Accent),
                new[]
                {
                    new BudgetChartSlice { Weight = budget.sponsorIncome, Color = new Color(0.28f, 0.68f, 0.46f, 1f) },
                    new BudgetChartSlice { Weight = budget.merchandiseIncome, Color = new Color(0.50f, 0.86f, 0.62f, 1f) }
                },
                ref _budgetIncomePieTex,
                ref _budgetIncomePieSprite);

            // Expense block (right)
            Image expenseBlock = CreateUiObject<Image>("ExpenseBlock", face.transform);
            SetRect(expenseBlock.rectTransform, new Vector2(0.508f, 0.18f), new Vector2(0.985f, 0.84f));
            expenseBlock.color = new Color(0.20f, 0.11f, 0.14f, 1f);
            expenseBlock.raycastTarget = false;
            FillBudgetBlock(
                expenseBlock.transform,
                "支出",
                Danger,
                new[]
                {
                    ("選手薪資", -budget.salaryExpense, Danger, true),
                    ("設施維護", -budget.facilityMaintenance, Danger, true)
                },
                ("支出合計", -budget.ExpenseTotal, Accent),
                new[]
                {
                    new BudgetChartSlice { Weight = budget.salaryExpense, Color = new Color(0.78f, 0.26f, 0.30f, 1f) },
                    new BudgetChartSlice { Weight = budget.facilityMaintenance, Color = new Color(0.92f, 0.42f, 0.38f, 1f) }
                },
                ref _budgetExpensePieTex,
                ref _budgetExpensePieSprite);""",
        """            FillBudgetBlock(
                incomeBlock.transform,
                "收入",
                Positive,
                new[]
                {
                    new BudgetPanelLineSpec
                    {
                        Label = BudgetSponsorIncomeLabel(budget),
                        Amount = budget.sponsorIncome,
                        Color = budget.hasSponsor ? Positive : TextMuted,
                        Signed = true,
                        DetailTitle = "贊助月費明細",
                        DetailBody = FormatBudgetBreakdown(budget.sponsorDetails, "目前無贊助合約")
                    },
                    new BudgetPanelLineSpec
                    {
                        Label = "商品銷售",
                        Amount = budget.merchandiseIncome,
                        Color = Positive,
                        Signed = true,
                        DetailTitle = "商品銷售明細",
                        DetailBody = FormatBudgetBreakdown(budget.merchandiseDetails, "本月無商品預估")
                    },
                    new BudgetPanelLineSpec
                    {
                        Label = "賽事獎金",
                        Amount = budget.prizeIncome,
                        Color = budget.prizeIncome > 0 ? Positive : TextMuted,
                        Signed = true,
                        DetailTitle = "賽事獎金明細",
                        DetailBody = FormatBudgetBreakdown(budget.prizeDetails, "本月尚無賽事獎金入帳")
                    }
                },
                ("收入合計", budget.IncomeTotal, Accent),
                new[]
                {
                    new BudgetChartSlice { Weight = budget.sponsorIncome, Color = new Color(0.28f, 0.68f, 0.46f, 1f) },
                    new BudgetChartSlice { Weight = budget.merchandiseIncome, Color = new Color(0.50f, 0.86f, 0.62f, 1f) },
                    new BudgetChartSlice { Weight = budget.prizeIncome, Color = new Color(0.72f, 0.92f, 0.48f, 1f) }
                },
                ref _budgetIncomePieTex,
                ref _budgetIncomePieSprite);

            // Expense block (right)
            Image expenseBlock = CreateUiObject<Image>("ExpenseBlock", face.transform);
            SetRect(expenseBlock.rectTransform, new Vector2(0.508f, 0.18f), new Vector2(0.985f, 0.84f));
            expenseBlock.color = new Color(0.20f, 0.11f, 0.14f, 1f);
            expenseBlock.raycastTarget = false;
            FillBudgetBlock(
                expenseBlock.transform,
                "支出",
                Danger,
                new[]
                {
                    new BudgetPanelLineSpec
                    {
                        Label = "選手薪資",
                        Amount = -budget.salaryExpense,
                        Color = Danger,
                        Signed = true,
                        DetailTitle = "選手薪資明細",
                        DetailBody = FormatBudgetBreakdown(budget.salaryDetails, "目前無選手合約")
                    },
                    new BudgetPanelLineSpec
                    {
                        Label = "教練組薪資",
                        Amount = -budget.coachSalaryExpense,
                        Color = Danger,
                        Signed = true,
                        DetailTitle = "教練組薪資明細",
                        DetailBody = FormatBudgetBreakdown(budget.coachSalaryDetails, "目前無教練組")
                    }
                },
                ("支出合計", -budget.ExpenseTotal, Accent),
                new[]
                {
                    new BudgetChartSlice { Weight = budget.salaryExpense, Color = new Color(0.78f, 0.26f, 0.30f, 1f) },
                    new BudgetChartSlice { Weight = budget.coachSalaryExpense, Color = new Color(0.92f, 0.55f, 0.32f, 1f) }
                },
                ref _budgetExpensePieTex,
                ref _budgetExpensePieSprite);""",
    )

    text = must_replace(
        text,
        """        private void FillBudgetBlock(
            Transform parent,
            string headerTitle,
            Color headerAccent,
            (string label, int amount, Color color, bool signed)[] lines,
            (string label, int amount, Color color) total,
            BudgetChartSlice[] pieSlices,
            ref Texture2D pieTexture,
            ref Sprite pieSprite)
        {""",
        """        private sealed class BudgetPanelLineSpec
        {
            public string Label;
            public int Amount;
            public Color Color;
            public bool Signed;
            public string DetailTitle;
            public string DetailBody;
        }

        private static string FormatBudgetBreakdown(IReadOnlyList<BudgetBreakdownLine> lines, string emptyMessage)
        {
            if (lines == null || lines.Count == 0)
                return emptyMessage;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                BudgetBreakdownLine line = lines[i];
                if (line == null)
                    continue;
                if (sb.Length > 0)
                    sb.Append('\\n');
                string label = string.IsNullOrWhiteSpace(line.label) ? "項目" : line.label;
                sb.Append(label).Append("  $").Append(Math.Abs(line.amount).ToString("N0"));
            }
            return sb.Length == 0 ? emptyMessage : sb.ToString();
        }

        private void FillBudgetBlock(
            Transform parent,
            string headerTitle,
            Color headerAccent,
            BudgetPanelLineSpec[] lines,
            (string label, int amount, Color color) total,
            BudgetChartSlice[] pieSlices,
            ref Texture2D pieTexture,
            ref Sprite pieSprite)
        {""",
    )

    # Fix line loop to use BudgetPanelLineSpec + hover
    text = must_replace(
        text,
        """            int totalAbs = 0;
            for (int i = 0; i < lines.Length; i++)
                totalAbs += Math.Abs(lines[i].amount);

            float yMax = 0.92f;
            float rowH = 0.22f;
            for (int i = 0; i < lines.Length; i++)
            {
                float y1 = yMax - i * rowH;
                float y0 = y1 - rowH + 0.04f;
                int absAmount = Math.Abs(lines[i].amount);
                string pct = totalAbs > 0 && absAmount > 0
                    ? $" ({absAmount * 100f / totalAbs:0}%）"
                    : string.Empty;
                AddBudgetPanelLine(
                    linesHost.transform,
                    lines[i].label + pct,
                    lines[i].amount,
                    lines[i].color,
                    lines[i].signed,
                    emphasize: false,
                    swatchColor: pieSlices != null && i < pieSlices.Length ? pieSlices[i].Color : (Color?)null,
                    y0,
                    y1);
            }""",
        """            int totalAbs = 0;
            for (int i = 0; i < lines.Length; i++)
                totalAbs += Math.Abs(lines[i].Amount);

            float yMax = 0.94f;
            float rowH = lines.Length >= 3 ? 0.18f : 0.22f;
            for (int i = 0; i < lines.Length; i++)
            {
                float y1 = yMax - i * rowH;
                float y0 = y1 - rowH + 0.03f;
                int absAmount = Math.Abs(lines[i].Amount);
                string pct = totalAbs > 0 && absAmount > 0
                    ? $" · {absAmount * 100f / totalAbs:0}%"
                    : string.Empty;
                AddBudgetPanelLine(
                    linesHost.transform,
                    lines[i].Label + pct,
                    lines[i].Amount,
                    lines[i].Color,
                    lines[i].Signed,
                    emphasize: false,
                    swatchColor: pieSlices != null && i < pieSlices.Length ? pieSlices[i].Color : (Color?)null,
                    y0,
                    y1,
                    lines[i].DetailTitle,
                    lines[i].DetailBody);
            }""",
    )

    text = must_replace(
        text,
        """            AddBudgetPanelLine(
                linesHost.transform,
                total.label,
                total.amount,
                total.color,
                signed: true,
                emphasize: true,
                swatchColor: null,
                0.02f,
                0.16f);
        }""",
        """            AddBudgetPanelLine(
                linesHost.transform,
                total.label,
                total.amount,
                total.color,
                signed: true,
                emphasize: true,
                swatchColor: null,
                0.02f,
                0.16f,
                null,
                null);
        }""",
    )

    text = must_replace(
        text,
        """        private void AddBudgetPanelLine(
            Transform parent,
            string label,
            int amount,
            Color amountColor,
            bool signed,
            bool emphasize,
            Color? swatchColor,
            float y0,
            float y1)
        {
            Image row = CreateUiObject<Image>("Line", parent);
            SetRect(row.rectTransform, new Vector2(0.02f, y0), new Vector2(0.98f, y1));
            row.color = emphasize ? new Color(0f, 0f, 0f, 0.22f) : Color.clear;
            row.raycastTarget = false;

            float labelMinX = 0.02f;
            if (swatchColor.HasValue)
            {
                Image swatch = CreateUiObject<Image>("Swatch", row.transform);
                SetRect(swatch.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.08f, 0.72f));
                swatch.color = swatchColor.Value;
                swatch.raycastTarget = false;
                labelMinX = 0.10f;
            }

            int size = emphasize ? 14 : 13;
            Color labelColor = emphasize ? TextPrimary : TextMuted;
            Text labelText = CreateText("Label", row.transform, size, TextAnchor.MiddleLeft, labelColor, _uiFont700);
            SetRect(labelText.rectTransform, new Vector2(labelMinX, 0.05f), new Vector2(0.58f, 0.95f));
            labelText.text = label;

            string value = signed
                ? (amount >= 0 ? $"+${amount:N0}" : $"-${Math.Abs(amount):N0}")
                : $"${amount:N0}";
            Text valueText = CreateText("Value", row.transform, size, TextAnchor.MiddleRight, amountColor, _uiFont700);
            SetRect(valueText.rectTransform, new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.95f));
            valueText.text = value;
        }""",
        """        private void AddBudgetPanelLine(
            Transform parent,
            string label,
            int amount,
            Color amountColor,
            bool signed,
            bool emphasize,
            Color? swatchColor,
            float y0,
            float y1,
            string detailTitle,
            string detailBody)
        {
            Image row = CreateUiObject<Image>("Line", parent);
            SetRect(row.rectTransform, new Vector2(0.02f, y0), new Vector2(0.98f, y1));
            row.color = emphasize ? new Color(0f, 0f, 0f, 0.22f) : Color.clear;
            bool hasDetail = !string.IsNullOrWhiteSpace(detailTitle) && !string.IsNullOrWhiteSpace(detailBody);
            row.raycastTarget = hasDetail;

            float labelMinX = 0.02f;
            if (swatchColor.HasValue)
            {
                Image swatch = CreateUiObject<Image>("Swatch", row.transform);
                SetRect(swatch.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.08f, 0.72f));
                swatch.color = swatchColor.Value;
                swatch.raycastTarget = false;
                labelMinX = 0.10f;
            }

            int size = emphasize ? 14 : 13;
            Color labelColor = emphasize ? TextPrimary : TextMuted;
            Text labelText = CreateText("Label", row.transform, size, TextAnchor.MiddleLeft, labelColor, _uiFont700);
            SetRect(labelText.rectTransform, new Vector2(labelMinX, 0.05f), new Vector2(0.58f, 0.95f));
            labelText.text = label;
            labelText.raycastTarget = false;

            string value = signed
                ? (amount >= 0 ? $"+${amount:N0}" : $"-${Math.Abs(amount):N0}")
                : $"${amount:N0}";
            Text valueText = CreateText("Value", row.transform, size, TextAnchor.MiddleRight, amountColor, _uiFont700);
            SetRect(valueText.rectTransform, new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.95f));
            valueText.text = value;
            valueText.raycastTarget = false;

            if (!hasDetail)
                return;

            EventTrigger trigger = row.gameObject.AddComponent<EventTrigger>();
            string capturedTitle = detailTitle;
            string capturedBody = detailBody;
            AddPointerTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                CancelStatTooltip();
                int lines = Math.Max(1, capturedBody.Split('\\n').Length);
                float height = Mathf.Clamp(120f + lines * 22f, 140f, 360f);
                ShowPlainTooltip(capturedTitle, capturedBody, new Vector2(360f, height), Input.mousePosition);
            });
            AddPointerTrigger(trigger, EventTriggerType.PointerExit, _ => HideStatTooltip());
        }""",
    )

    # Slightly taller panel for 3 income lines
    text = must_replace(
        text,
        "            const float panelHeight = 388f;",
        "            const float panelHeight = 420f;",
    )

    if "賽事獎金" not in text or "教練組薪資" not in text:
        raise RuntimeError("budget labels missing after patch")
    if "設施維護" in text[text.find("FillBudgetBlock("):text.find("本月淨值")]:
        # facility may still appear elsewhere; ensure expense block no longer lists it via coach label present
        pass
    if "FormatBudgetBreakdown" not in text:
        raise RuntimeError("FormatBudgetBreakdown missing")

    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print("patched", TARGET)


if __name__ == "__main__":
    main()
