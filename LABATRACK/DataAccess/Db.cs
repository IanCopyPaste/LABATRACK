using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LABATRACK.DataAccess
{
    /// <summary>
    /// The one place that knows how to reach the database. It reads the LABATRACK_conn
    /// connection string from Web.config (no other code does), opens connections, and builds
    /// commands that run one stored procedure each.
    ///
    /// Every query goes through a stored procedure (CommandType.StoredProcedure) and every value
    /// travels as a typed SqlParameter, so no SQL text is ever built in C# and typed input can
    /// never change what a query does (no SQL injection).
    /// </summary>
    public static class Db
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["LABATRACK_conn"].ConnectionString; }
        }

        /// <summary>An open connection. Always use it in a using block so it is closed again.</summary>
        public static SqlConnection Open()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// A command that runs the stored procedure <paramref name="procedure"/>. Pass the
        /// transaction when the call is part of a save that must happen all together or not at all.
        /// </summary>
        public static SqlCommand Proc(SqlConnection conn, SqlTransaction tx, string procedure)
        {
            return new SqlCommand(procedure, conn, tx) { CommandType = CommandType.StoredProcedure };
        }

        /// <summary>
        /// Runs a guarded UPDATE procedure and returns how many rows it changed. The procedure
        /// ends with SELECT @@ROWCOUNT AS RowsAffected; ExecuteNonQuery cannot be used for this
        /// because it returns -1 under SET NOCOUNT ON.
        /// </summary>
        public static int RowsAffected(SqlCommand cmd)
        {
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ------------------------------------------------------------ Parameters
        // Each one states its SQL type, matching the procedure, so nothing is guessed from the
        // C# value. A null becomes DBNull (SQL NULL).

        public static void AddInt(SqlCommand cmd, string name, int? value)
        {
            cmd.Parameters.Add(name, SqlDbType.Int).Value = (object)value ?? DBNull.Value;
        }

        public static void AddText(SqlCommand cmd, string name, string value, int size)
        {
            cmd.Parameters.Add(name, SqlDbType.NVarChar, size).Value = (object)value ?? DBNull.Value;
        }

        /// <summary>A peso amount: DECIMAL(10,2).</summary>
        public static void AddMoney(SqlCommand cmd, string name, decimal? value)
        {
            AddDecimal(cmd, name, value, 10, 2);
        }

        /// <summary>A weight in kilos: DECIMAL(6,2).</summary>
        public static void AddKg(SqlCommand cmd, string name, decimal value)
        {
            AddDecimal(cmd, name, value, 6, 2);
        }

        public static void AddDecimal(SqlCommand cmd, string name, decimal? value, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = (object)value ?? DBNull.Value;
        }

        public static void AddBit(SqlCommand cmd, string name, bool value)
        {
            cmd.Parameters.Add(name, SqlDbType.Bit).Value = value;
        }

        public static void AddDateTime(SqlCommand cmd, string name, DateTime? value)
        {
            cmd.Parameters.Add(name, SqlDbType.DateTime2).Value = (object)value ?? DBNull.Value;
        }

        public static void AddBinary(SqlCommand cmd, string name, byte[] value, int size)
        {
            cmd.Parameters.Add(name, SqlDbType.VarBinary, size).Value = (object)value ?? DBNull.Value;
        }

        // ------------------------------------------------------------ Reading a row

        public static string GetText(IDataRecord r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? null : (string)v;
        }

        public static int GetInt(IDataRecord r, string column)
        {
            return Convert.ToInt32(r[column]);
        }

        public static decimal GetDecimal(IDataRecord r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? 0m : Convert.ToDecimal(v);
        }

        public static decimal? GetDecimalOrNull(IDataRecord r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? (decimal?)null : Convert.ToDecimal(v);
        }

        public static bool GetBool(IDataRecord r, string column)
        {
            object v = r[column];
            return v != DBNull.Value && Convert.ToBoolean(v);
        }

        public static DateTime GetDateTime(IDataRecord r, string column)
        {
            return (DateTime)r[column];
        }

        public static DateTime? GetDateTimeOrNull(IDataRecord r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? (DateTime?)null : (DateTime)v;
        }

        public static byte[] GetBytes(IDataRecord r, string column)
        {
            object v = r[column];
            return v == DBNull.Value ? null : (byte[])v;
        }
    }
}
