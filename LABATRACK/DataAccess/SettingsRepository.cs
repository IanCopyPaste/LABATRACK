using System;
using System.Collections.Generic;
using System.Globalization;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The Settings table. The rows are key/value text; Get() turns them into one ShopSettings
    /// object with numbers where numbers belong. Numbers are stored with a dot for the decimal
    /// point (invariant culture), whatever the computer's regional settings are.
    /// </summary>
    public static class SettingsRepository
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static ShopSettings Get()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Settings_GetAll"))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    values[Db.GetText(r, "SettingKey")] = Db.GetText(r, "SettingValue");
            }

            // A missing or unreadable row keeps the default from ShopSettings.
            var s = new ShopSettings();
            s.MinimumCharge = ReadDecimal(values, ShopSettings.MinimumChargeKey, s.MinimumCharge);
            s.RoundingIncrementKg = ReadDecimal(values, ShopSettings.RoundingIncrementKgKey, s.RoundingIncrementKg);
            s.MaxWeightKg = ReadDecimal(values, ShopSettings.MaxWeightKgKey, s.MaxWeightKg);
            s.UnclaimedThresholdDays = ReadInt(values, ShopSettings.UnclaimedThresholdDaysKey, s.UnclaimedThresholdDays);
            s.DefaultTurnaroundHours = ReadInt(values, ShopSettings.DefaultTurnaroundHoursKey, s.DefaultTurnaroundHours);
            s.ShopName = ReadText(values, ShopSettings.ShopNameKey, s.ShopName);
            s.SlipHeader = ReadText(values, ShopSettings.SlipHeaderKey, s.SlipHeader);
            s.ReworkStages = ReadText(values, ShopSettings.ReworkStagesKey, s.ReworkStages);
            return s;
        }

        public static void Set(string key, string value)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Settings_Set"))
            {
                Db.AddText(cmd, "@SettingKey", key, 50);
                Db.AddText(cmd, "@SettingValue", value, 400);
                cmd.ExecuteNonQuery();
            }
        }

        public static void Set(string key, decimal value)
        {
            Set(key, value.ToString("0.00##", Inv));   // 120 -> "120.00", 0.5 -> "0.50"
        }

        public static void Set(string key, int value)
        {
            Set(key, value.ToString(Inv));
        }

        private static decimal ReadDecimal(Dictionary<string, string> values, string key, decimal fallback)
        {
            string text;
            decimal d;
            return values.TryGetValue(key, out text) && decimal.TryParse(text, NumberStyles.Number, Inv, out d) ? d : fallback;
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int fallback)
        {
            string text;
            int i;
            return values.TryGetValue(key, out text) && int.TryParse(text, NumberStyles.Integer, Inv, out i) ? i : fallback;
        }

        private static string ReadText(Dictionary<string, string> values, string key, string fallback)
        {
            string text;
            return values.TryGetValue(key, out text) && text != null ? text : fallback;
        }
    }
}
