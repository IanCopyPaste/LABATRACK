using System.Configuration;

namespace LABATRACK.db_conn
{
    public static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["LABATRACK_conn"].ConnectionString; }
        }
    }
}
