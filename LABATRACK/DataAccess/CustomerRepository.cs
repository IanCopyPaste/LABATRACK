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
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        /// <summary>The customer with this contact number (digits only), or null.</summary>
        public static Customer GetByContactDigits(string digits)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetByContactDigits", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ContactDigits", SqlDbType.NVarChar, 20).Value = digits;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        /// <summary>
        /// Customers whose name or email contains <paramref name="text"/>, or whose number
        /// contains <paramref name="digits"/>. Both null: every customer. A to Z.
        /// </summary>
        public static List<Customer> Search(string text, string digits)
        {
            List<Customer> list = new List<Customer>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = (object)text ?? DBNull.Value;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = (object)digits ?? DBNull.Value;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(Map(r));
                    }
                }
            }
            return list;
        }

        public static int Count()
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Count", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Adds a customer as one step of a larger save (the connection and transaction come from the caller).</summary>
        public static int Insert(SqlConnection conn, SqlTransaction tx, string name, string contactNumber, string email)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Insert", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = contactNumber;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = (object)email ?? DBNull.Value;

                return Convert.ToInt32(cmd.ExecuteScalar());   // the new CustomerId
            }
        }

        public static void Update(SqlConnection conn, SqlTransaction tx, int customerId, string name, string contactNumber, string email)
        {
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Update", conn, tx))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = contactNumber;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = (object)email ?? DBNull.Value;

                cmd.ExecuteScalar();
            }
        }

        /// <summary>One row of the customer queries. JobCount and LastDropOff are counted by the procedure.</summary>
        private static Customer Map(SqlDataReader r)
        {
            Customer customer = new Customer();
            customer.CustomerId = (int)r["CustomerId"];
            customer.Name = (string)r["Name"];
            customer.ContactNumber = (string)r["ContactNumber"];
            customer.Email = r["Email"] as string;
            customer.CreatedAt = (DateTime)r["CreatedAt"];
            customer.JobCount = Convert.ToInt32(r["JobCount"]);
            customer.LastDropOff = r["LastDropOff"] as DateTime?;
            return customer;
        }
    }
}
