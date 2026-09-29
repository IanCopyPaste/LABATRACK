using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The AddOns table (service add-ons and product add-ons), through its stored procedures.</summary>
    public static class AddOnRepository
    {
        /// <summary>Every add-on: service add-ons first, then products; active ones first in each.</summary>
        public static List<AddOn> GetAll()
        {
            List<AddOn> list = new List<AddOn>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        AddOn addOn = new AddOn();
                        addOn.AddOnId = (int)r["AddOnId"];
                        addOn.Name = (string)r["Name"];
                        addOn.Price = (decimal)r["Price"];
                        addOn.Kind = (string)r["Kind"];
                        addOn.IsActive = (bool)r["IsActive"];
                        list.Add(addOn);
                    }
                }
            }
            return list;
        }

        public static int Insert(AddOn a)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_Insert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = a.Name;
                cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = a.Price });
                cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 10).Value = a.Kind;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = a.IsActive;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());   // the new AddOnId
            }
        }

        public static bool Update(AddOn a)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_AddOns_Update", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@AddOnId", SqlDbType.Int).Value = a.AddOnId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = a.Name;
                cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = a.Price });
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = a.IsActive;

                conn.Open();
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }
    }
}
