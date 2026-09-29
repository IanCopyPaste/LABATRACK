using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string key = (string)r["SettingKey"];
                        string value = r["SettingValue"] as string;
                        values[key] = value;
                    }
                }
            }

            // A missing or unreadable row keeps the default from ShopSettings.
            ShopSettings s = new ShopSettings();
            s.MinimumCharge = ReadDecimal(values, ShopSettings.MinimumChargeKey, s.MinimumCharge);
            s.RoundingIncrementKg = ReadDecimal(values, ShopSettings.RoundingIncrementKgKey, s.RoundingIncrementKg);
            s.MaxWeightKg = ReadDecimal(values, ShopSettings.MaxWeightKgKey, s.MaxWeightKg);
            s.MinimumDownPaymentPercent = Math.Min(100, Math.Max(0, ReadInt(values, ShopSettings.MinimumDownPaymentPercentKey, s.MinimumDownPaymentPercent)));
            s.UnclaimedThresholdDays = ReadInt(values, ShopSettings.UnclaimedThresholdDaysKey, s.UnclaimedThresholdDays);
            s.DefaultTurnaroundHours = ReadInt(values, ShopSettings.DefaultTurnaroundHoursKey, s.DefaultTurnaroundHours);
            s.ShopName = ReadText(values, ShopSettings.ShopNameKey, s.ShopName);
            s.SlipHeader = ReadText(values, ShopSettings.SlipHeaderKey, s.SlipHeader);
            s.ReworkStages = ReadText(values, ShopSettings.ReworkStagesKey, s.ReworkStages);
            return s;
        }

        public static void Set(string key, string value)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_Set", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SettingKey", SqlDbType.NVarChar, 50).Value = key;
                cmd.Parameters.Add("@SettingValue", SqlDbType.NVarChar, 400).Value = value;

                conn.Open();
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
