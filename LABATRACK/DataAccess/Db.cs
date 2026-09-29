using System.Configuration;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The one place that reads the LABATRACK_conn connection string from Web.config. It holds
    /// nothing else: every repository writes out its own SqlConnection, SqlCommand, parameters and
    /// reader in full, so each query can be read from top to bottom in one place.
    ///
    /// Every query goes through a stored procedure (CommandType.StoredProcedure) and every value
    /// travels as a typed SqlParameter, so no SQL text is ever built in C# and typed input can
    /// never change what a query does (no SQL injection).
    /// </summary>
    public static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["LABATRACK_conn"].ConnectionString; }
        }
    }
}
