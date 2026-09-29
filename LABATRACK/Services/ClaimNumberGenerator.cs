using System;
using System.Data.SqlClient;
using System.Globalization;
using LABATRACK.DataAccess;

namespace LABATRACK.Services
{
    /// <summary>
    /// Gives each new job its claim number: LT-yyMMdd-### (open decision 2), for example
    /// LT-260929-004 for the fourth job dropped off on 29 Sep 2026. Short and easy to read out
    /// at the counter, and it tells staff at a glance which day a load came in.
    ///
    /// Two jobs saved in the same second never get the same number: the lookup of the day's
    /// latest number locks that day's numbers until the saving transaction ends, so the second
    /// save waits and then takes the next number. The unique index on ClaimNumber would refuse
    /// a duplicate anyway.
    /// </summary>
    public static class ClaimNumberGenerator
    {
        public static string Next(SqlConnection conn, SqlTransaction tx, DateTime now)
        {
            string prefix = "LT-" + now.ToString("yyMMdd", CultureInfo.InvariantCulture) + "-";
            string last = JobOrderRepository.GetLastClaimNumber(conn, tx, prefix);

            int number = 0;
            if (last != null)
                int.TryParse(last.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out number);

            // Three digits (001 to 999); a 1,000th job in one day would simply get four.
            return prefix + (number + 1).ToString("000", CultureInfo.InvariantCulture);
        }
    }
}
