using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.EconomyPanelPatches
{
    internal static class EconomyCorruptionRepair
    {
        private const int EconomyPeriodCount = 17;

        private static readonly FieldInfo IncomeField =
            AccessTools.Field(typeof(EconomyManager), "m_income");

        private static readonly FieldInfo ExpensesField =
            AccessTools.Field(typeof(EconomyManager), "m_expenses");

        private static readonly FieldInfo TotalIncomeField =
            AccessTools.Field(typeof(EconomyManager), "m_totalIncome");

        private static readonly FieldInfo TotalExpensesField =
            AccessTools.Field(typeof(EconomyManager), "m_totalExpenses");

        private static readonly FieldInfo LastCashDeltaField =
            AccessTools.Field(typeof(EconomyManager), "m_lastCashDelta");

        private static readonly MethodInfo ClassIndexMethod =
            AccessTools.Method(
                typeof(EconomyManager),
                "ClassIndex",
                new[]
                {
                    typeof(ItemClass.Service),
                    typeof(ItemClass.SubService),
                    typeof(ItemClass.Level)
                });

        public static void RepairNegativePublicTransportValues()
        {
            try
            {
                var economyManager = Singleton<EconomyManager>.instance;
                if (economyManager == null ||
                    IncomeField == null ||
                    ExpensesField == null ||
                    TotalIncomeField == null ||
                    TotalExpensesField == null ||
                    LastCashDeltaField == null ||
                    ClassIndexMethod == null)
                {
                    Utils.LogWarning("EconomyCorruptionRepair: economy internals are unavailable; repair skipped.");
                    return;
                }

                var income = (long[])IncomeField.GetValue(economyManager);
                var expenses = (long[])ExpensesField.GetValue(economyManager);
                var totalIncome = (long[])TotalIncomeField.GetValue(economyManager);
                var totalExpenses = (long[])TotalExpensesField.GetValue(economyManager);
                if (income == null || expenses == null || totalIncome == null || totalExpenses == null)
                {
                    return;
                }

                var repairedIncomeEntries = 0;
                var repairedExpenseEntries = 0;
                long repairedIncomeAmount = 0;
                long repairedExpenseAmount = 0;

                foreach (var classIndex in GetPublicTransportClassIndexes())
                {
                    for (var period = 0; period < EconomyPeriodCount; period++)
                    {
                        var valueIndex = classIndex * EconomyPeriodCount + period;
                        if (valueIndex < 0 || valueIndex >= income.Length || valueIndex >= expenses.Length)
                        {
                            continue;
                        }

                        RepairNegativeValue(
                            income,
                            totalIncome,
                            valueIndex,
                            period,
                            ref repairedIncomeEntries,
                            ref repairedIncomeAmount);

                        RepairNegativeValue(
                            expenses,
                            totalExpenses,
                            valueIndex,
                            period,
                            ref repairedExpenseEntries,
                            ref repairedExpenseAmount);
                    }
                }

                if (repairedIncomeEntries != 0 || repairedExpenseEntries != 0)
                {
                    LastCashDeltaField.SetValue(
                        economyManager,
                        CalculateLastCashDelta(totalIncome, totalExpenses));

                    Utils.LogWarning(
                        $"EconomyCorruptionRepair: repaired saved public-transport economy data " +
                        $"(incomeEntries={repairedIncomeEntries}, incomeAmount={repairedIncomeAmount}, " +
                        $"expenseEntries={repairedExpenseEntries}, expenseAmount={repairedExpenseAmount}).");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"EconomyCorruptionRepair: repair failed: {ex.Message}");
            }
        }

        private static IEnumerable<int> GetPublicTransportClassIndexes()
        {
            var indexes = new HashSet<int>();
            AddClassIndex(indexes, ItemClass.SubService.None);

            foreach (ItemClass.SubService subService in Enum.GetValues(typeof(ItemClass.SubService)))
            {
                if (subService.ToString().StartsWith("PublicTransport", StringComparison.Ordinal))
                {
                    AddClassIndex(indexes, subService);
                }
            }

            return indexes;
        }

        private static void AddClassIndex(HashSet<int> indexes, ItemClass.SubService subService)
        {
            var index = (int)ClassIndexMethod.Invoke(
                null,
                new object[]
                {
                    ItemClass.Service.PublicTransport,
                    subService,
                    ItemClass.Level.None
                });

            if (index >= 0)
            {
                indexes.Add(index);
            }
        }

        private static void RepairNegativeValue(
            long[] values,
            long[] totals,
            int valueIndex,
            int period,
            ref int repairedEntries,
            ref long repairedAmount)
        {
            var value = values[valueIndex];
            if (value >= 0)
            {
                return;
            }

            var correction = value == long.MinValue ? long.MaxValue : -value;
            values[valueIndex] = 0;
            if (period < totals.Length)
            {
                totals[period] = SaturatingAdd(totals[period], correction);
            }

            repairedEntries++;
            repairedAmount = SaturatingAdd(repairedAmount, correction);
        }

        private static long SaturatingAdd(long left, long right)
        {
            if (right > 0 && left > long.MaxValue - right)
            {
                return long.MaxValue;
            }

            if (right < 0 && left < long.MinValue - right)
            {
                return long.MinValue;
            }

            return left + right;
        }

        private static long SaturatingSubtract(long left, long right)
        {
            if (right > 0 && left < long.MinValue + right)
            {
                return long.MinValue;
            }

            if (right < 0 && left > long.MaxValue + right)
            {
                return long.MaxValue;
            }

            return left - right;
        }

        private static long CalculateLastCashDelta(long[] totalIncome, long[] totalExpenses)
        {
            long result = 0;
            var periodCount = Math.Min(16, Math.Min(totalIncome.Length, totalExpenses.Length));
            for (var period = 0; period < periodCount; period++)
            {
                result = SaturatingAdd(result, totalIncome[period]);
                result = SaturatingSubtract(result, totalExpenses[period]);
            }

            return result;
        }
    }
}
