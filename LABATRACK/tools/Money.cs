using System.Globalization;

namespace LABATRACK.tools
{
    public static class Money
    {
        public static string Peso(decimal amount)
        {
            return "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}
