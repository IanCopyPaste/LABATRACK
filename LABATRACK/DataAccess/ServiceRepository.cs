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
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Services_GetAll"))
            {
                var list = new List<LaundryService>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(Map(r));
                }
                return list;
            }
        }

        public static LaundryService GetById(int serviceId)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Services_GetById"))
            {
                Db.AddInt(cmd, "@ServiceId", serviceId);
                using (var r = cmd.ExecuteReader())
                {
                    return r.Read() ? Map(r) : null;
                }
            }
        }

        public static int Insert(LaundryService s)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Services_Insert"))
            {
                AddFields(cmd, s);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Saves a change to a service. Returns false if nothing was saved because a load using
        /// the service is still in the shop (the procedure checks this in the same statement).
        /// </summary>
        public static bool Update(LaundryService s)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_Services_Update"))
            {
                Db.AddInt(cmd, "@ServiceId", s.ServiceId);
                AddFields(cmd, s);
                return Db.RowsAffected(cmd) == 1;
            }
        }

        private static void AddFields(SqlCommand cmd, LaundryService s)
        {
            Db.AddText(cmd, "@Name", s.Name, 60);
            Db.AddMoney(cmd, "@RatePerKg", s.RatePerKg);
            Db.AddBit(cmd, "@IncludesWashing", s.IncludesWashing);
            Db.AddBit(cmd, "@IncludesDrying", s.IncludesDrying);
            Db.AddBit(cmd, "@IncludesFolding", s.IncludesFolding);
            Db.AddBit(cmd, "@IsActive", s.IsActive);
        }

        private static LaundryService Map(IDataRecord r)
        {
            return new LaundryService
            {
                ServiceId = Db.GetInt(r, "ServiceId"),
                Name = Db.GetText(r, "Name"),
                RatePerKg = Db.GetDecimal(r, "RatePerKg"),
                IncludesWashing = Db.GetBool(r, "IncludesWashing"),
                IncludesDrying = Db.GetBool(r, "IncludesDrying"),
                IncludesFolding = Db.GetBool(r, "IncludesFolding"),
                IsActive = Db.GetBool(r, "IsActive"),
                LoadsInShop = Db.GetInt(r, "LoadsInShop"),
            };
        }
    }
}
