using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The Services table (the main services and their routes), through its stored procedures.</summary>
    public static class ServiceRepository
    {
        /// <summary>Every service, active ones first, each with how many of its loads are still in the shop.</summary>
        public static List<LaundryService> GetAll()
        {
            List<LaundryService> list = new List<LaundryService>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

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

        public static LaundryService GetById(int serviceId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = serviceId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return Map(r);
                }
            }
        }

        public static int Insert(LaundryService s)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_Insert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = s.Name;
                cmd.Parameters.Add(new SqlParameter("@RatePerKg", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = s.RatePerKg });
                cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = s.IncludesWashing;
                cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = s.IncludesDrying;
                cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = s.IncludesFolding;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = s.IsActive;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());   // the new ServiceId
            }
        }

        /// <summary>
        /// Saves a change to a service. Returns false if nothing was saved because a load using
        /// the service is still in the shop (the procedure checks this in the same statement).
        /// </summary>
        public static bool Update(LaundryService s)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Services_Update", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = s.ServiceId;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 60).Value = s.Name;
                cmd.Parameters.Add(new SqlParameter("@RatePerKg", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = s.RatePerKg });
                cmd.Parameters.Add("@IncludesWashing", SqlDbType.Bit).Value = s.IncludesWashing;
                cmd.Parameters.Add("@IncludesDrying", SqlDbType.Bit).Value = s.IncludesDrying;
                cmd.Parameters.Add("@IncludesFolding", SqlDbType.Bit).Value = s.IncludesFolding;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = s.IsActive;

                conn.Open();
                int rowsAffected = Convert.ToInt32(cmd.ExecuteScalar());
                return rowsAffected == 1;
            }
        }

        private static LaundryService Map(SqlDataReader r)
        {
            LaundryService service = new LaundryService();
            service.ServiceId = (int)r["ServiceId"];
            service.Name = (string)r["Name"];
            service.RatePerKg = (decimal)r["RatePerKg"];
            service.IncludesWashing = (bool)r["IncludesWashing"];
            service.IncludesDrying = (bool)r["IncludesDrying"];
            service.IncludesFolding = (bool)r["IncludesFolding"];
            service.IsActive = (bool)r["IsActive"];
            service.LoadsInShop = Convert.ToInt32(r["LoadsInShop"]);
            return service;
        }
    }
}
