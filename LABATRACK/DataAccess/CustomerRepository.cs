using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The Customers table, through its stored procedures. The rules are in CustomerService.</summary>
    public static class CustomerRepository
    {
        public static Customer GetById(int customerId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Customers_GetById"))
            {
                Db.AddInt(cmd, "@CustomerId", customerId);
                return ReadOne(cmd);
            }
        }

        /// <summary>The customer with this contact number (digits only), or null.</summary>
        public static Customer GetByContactDigits(string digits)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Customers_GetByContactDigits"))
            {
                Db.AddText(cmd, "@ContactDigits", digits, 20);
                return ReadOne(cmd);
            }
        }

        /// <summary>
        /// Customers whose name or email contains <paramref name="text"/>, or whose number
        /// contains <paramref name="digits"/>. Both null: every customer. A to Z.
        /// </summary>
        public static List<Customer> Search(string text, string digits)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Customers_Search"))
            {
                Db.AddText(cmd, "@Text", text, 100);
                Db.AddText(cmd, "@Digits", digits, 20);
                var list = new List<Customer>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(Map(r));
                }
                return list;
            }
        }

        public static int Count()
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Customers_Count"))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int Insert(SqlConnection conn, SqlTransaction tx, string name, string contactNumber, string email)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_Customers_Insert"))
            {
                Db.AddText(cmd, "@Name", name, 100);
                Db.AddText(cmd, "@ContactNumber", contactNumber, 20);
                Db.AddText(cmd, "@Email", email, 254);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void Update(SqlConnection conn, SqlTransaction tx, int customerId, string name, string contactNumber, string email)
        {
            using (var cmd = Db.Proc(conn, tx, "dbo.usp_Customers_Update"))
            {
                Db.AddInt(cmd, "@CustomerId", customerId);
                Db.AddText(cmd, "@Name", name, 100);
                Db.AddText(cmd, "@ContactNumber", contactNumber, 20);
                Db.AddText(cmd, "@Email", email, 254);
                Db.RowsAffected(cmd);
            }
        }

        private static Customer ReadOne(SqlCommand cmd)
        {
            using (var r = cmd.ExecuteReader())
            {
                return r.Read() ? Map(r) : null;
            }
        }

        private static Customer Map(IDataRecord r)
        {
            return new Customer
            {
                CustomerId = Db.GetInt(r, "CustomerId"),
                Name = Db.GetText(r, "Name"),
                ContactNumber = Db.GetText(r, "ContactNumber"),
                Email = Db.GetText(r, "Email"),
                CreatedAt = Db.GetDateTime(r, "CreatedAt"),
                JobCount = Db.GetInt(r, "JobCount"),
                LastDropOff = Db.GetDateTimeOrNull(r, "LastDropOff"),
            };
        }
    }
}
