using HarmonyLib;
using System.Numerics;
using System.Reflection;
using Panik;

namespace CloverAddictivePatches.Patches
{
    [HarmonyPatch]
    public class SmartDeposit
    {
        private static bool isCalculating = false;

        private static bool lastShiftState = false;

        private static BigInteger cachedSmartAmount = BigInteger.Zero;
        private static BigInteger cachedCoins = BigInteger.MinusOne;
        private static BigInteger cachedDeposit = BigInteger.MinusOne;
        private static BigInteger cachedDebt = BigInteger.MinusOne;
        private static bool cacheValid = false;

        public enum StopReason
        {
            None,
            Crown,
            SkullWarning
        }
        private static StopReason lastStopReason = StopReason.None;

        public static void InvalidateCache()
        {
            cacheValid = false;
            lastStopReason = StopReason.None;
        }

        private static BigInteger CalculateSmartDepositAmount()
        {
            BigInteger coins = GameplayData.CoinsGet();
            BigInteger deposit = GameplayData.DepositGet();
            BigInteger debt = GameplayData.DebtGet();

            if (cacheValid &&
                cachedCoins == coins &&
                cachedDeposit == deposit &&
                cachedDebt == debt)
            {
                return cachedSmartAmount;
            }

            if (isCalculating)
            {
                return GameplayData.NextDepositAmmountGet(forceDefault: true);
            }

            isCalculating = true;

            try
            {
                BigInteger defaultStep = GameplayData.NextDepositAmmountGet(forceDefault: true);

                BigInteger debtMissing = debt - deposit;

                BigInteger normalAmount = defaultStep;

                if (coins > 0 && coins < defaultStep && coins < debtMissing)
                    normalAmount = coins;
                else if (debtMissing < defaultStep)
                    normalAmount = debtMissing;

                BigInteger spinCostSingle = GameplayData.SpinCostGet_Single();
                if (GameplayData.RoundsLeftToDeadline() > 0 && normalAmount > spinCostSingle)
                {
                    BigInteger maxSpinCost = GameplayData.SpinCostMax_Get();
                    if (coins - normalAmount < maxSpinCost)
                        normalAmount = coins > maxSpinCost ? coins - maxSpinCost : spinCostSingle;
                }

                bool crownNext = normalAmount >= debtMissing;

                bool skullNext = false;
                if (GameplayData.RoundsLeftToDeadline() > 0)
                {
                    BigInteger maxSpinCost = GameplayData.SpinCostMax_Get();
                    skullNext = (coins - normalAmount) <= maxSpinCost;
                }

                if (crownNext || skullNext)
                {
                    cachedSmartAmount = normalAmount;
                    cachedCoins = coins;
                    cachedDeposit = deposit;
                    cachedDebt = debt;
                    cacheValid = true;
                    return normalAmount;
                }

                BigInteger simulatedCoins = coins;
                BigInteger simulatedDeposit = deposit;
                BigInteger totalSmartDeposit = BigInteger.Zero;
                int maxIterations = 1000;
                int iterations = 0;

                while (iterations < maxIterations)
                {
                    iterations++;

                    bool wouldShowSkull = false;
                    if (GameplayData.RoundsLeftToDeadline() > 0)
                    {
                        BigInteger maxSpinCost = GameplayData.SpinCostMax_Get();
                        wouldShowSkull = simulatedCoins <= maxSpinCost;
                    }

                    BigInteger simulatedDebtMissing = debt - simulatedDeposit;
                    BigInteger nextDepositAmount = defaultStep;

                    if (simulatedCoins > 0 && simulatedCoins < defaultStep && simulatedCoins < simulatedDebtMissing)
                        nextDepositAmount = simulatedCoins;
                    else if (simulatedDebtMissing < defaultStep)
                        nextDepositAmount = simulatedDebtMissing;

                    if (GameplayData.RoundsLeftToDeadline() > 0 && nextDepositAmount > spinCostSingle)
                    {
                        BigInteger maxSpinCost = GameplayData.SpinCostMax_Get();
                        if (simulatedCoins - nextDepositAmount < maxSpinCost)
                            nextDepositAmount = simulatedCoins > maxSpinCost ? simulatedCoins - maxSpinCost : spinCostSingle;
                    }

                    bool wouldShowCrown = nextDepositAmount >= simulatedDebtMissing;

                    if (wouldShowCrown || wouldShowSkull)
                    {
                        lastStopReason = wouldShowCrown ? StopReason.Crown : StopReason.SkullWarning;
                        break;
                    }

                    totalSmartDeposit += nextDepositAmount;
                    simulatedCoins -= nextDepositAmount;
                    simulatedDeposit += nextDepositAmount;

                    if (simulatedCoins <= 0 || simulatedDeposit >= debt)
                        break;
                }

                BigInteger smartAmount = totalSmartDeposit > 0 ? totalSmartDeposit : normalAmount;

                cachedSmartAmount = smartAmount;
                cachedCoins = coins;
                cachedDeposit = deposit;
                cachedDebt = debt;
                cacheValid = true;

                return smartAmount;
            }
            finally
            {
                isCalculating = false;
            }
        }

        private static bool IsShiftHeld()
        {
            return Controls.KeyboardButton_HoldGet(0, Controls.KeyboardElement.LeftShift) ||
                   Controls.KeyboardButton_HoldGet(0, Controls.KeyboardElement.RightShift);
        }

        [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.NextDepositAmmountGet))]
        [HarmonyPrefix]
        static bool NextDepositAmmountGet_Prefix(ref BigInteger __result, bool forceDefault)
        {
            if (!Plugin.SmartDepositPatch.Value)
                return true;

            if (forceDefault)
                return true;

            if (isCalculating)
                return true;

            if (IsShiftHeld())
            {
                __result = CalculateSmartDepositAmount();
                return false;
            }

            lastStopReason = StopReason.None;
            return true;
        }

        [HarmonyPatch(typeof(ATMScript), "Update")]
        [HarmonyPostfix]
        static void ATMUpdate_Postfix()
        {
            if (!Plugin.SmartDepositPatch.Value)
                return;

            bool currentShiftState = IsShiftHeld();

            if (currentShiftState != lastShiftState)
            {
                lastShiftState = currentShiftState;

                InvalidateCache();

                if (PromptGuideScript.GetGuideType() == PromptGuideScript.GuideType.atm_insertCoin)
                {
                    PromptGuideScript.SetGuideType(PromptGuideScript.GuideType.atm_insertCoin);
                }
            }
        }

        [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.DepositAdd))]
        [HarmonyPostfix]
        static void DepositAdd_Postfix()
        {
            if (!Plugin.SmartDepositPatch.Value)
                return;

            InvalidateCache();
        }

        [HarmonyPatch(typeof(PromptGuideScript), nameof(PromptGuideScript.SetGuideType))]
        [HarmonyPostfix]
        static void SetGuideType_Postfix(PromptGuideScript.GuideType type)
        {
            if (!Plugin.SmartDepositPatch.Value)
                return;

            if (type != PromptGuideScript.GuideType.atm_insertCoin)
                return;

            if (!IsShiftHeld() || lastStopReason == StopReason.None)
                return;

            var textField = typeof(PromptGuideScript).GetField("text",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (textField == null)
                return;

            var textComponent = textField.GetValue(PromptGuideScript.instance) as TMPro.TextMeshProUGUI;
            if (textComponent == null)
                return;

            string currentText = textComponent.text;

            string stopIndicator = "";
            if (lastStopReason == StopReason.Crown)
            {
                stopIndicator = "<color=#888888>(</color><sprite name=\"CardSymb_Victory\"><color=#888888>)</color> ";
            }
            else if (lastStopReason == StopReason.SkullWarning)
            {
                stopIndicator = "<color=#888888>(</color><sprite name=\"SlotWarning\"><color=#888888>)</color> ";
            }

            textComponent.text = stopIndicator + currentText;
        }
    }
}
