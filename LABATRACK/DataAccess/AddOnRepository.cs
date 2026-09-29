using System;
using System.Collections.Generic;
using System.Data;
using LABATRACK.Models;

namespace LABATRACK.DataAccess
{
    /// <summary>The AddOns table (service add-ons and product add-ons), through its stored procedures.</summary>
    public static class AddOnRepository
    {
        /// <summary>Every add-on: service add-ons first, then products; active ones first in each.</summary>
        public static List<AddOn> GetAll()
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_AddOns_GetAll"))
            {
                var list = new List<AddOn>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new AddOn
                        {
                            AddOnId = Db.GetInt(r, "AddOnId"),
                            Name = Db.GetText(r, "Name"),
                            Price = Db.GetDecimal(r, "Price"),
                            Kind = Db.GetText(r, "Kind"),
                            IsActive = Db.GetBool(r, "IsActive"),
                        });
                    }
                }
                return list;
            }
        }

        public static int Insert(AddOn a)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_AddOns_Insert"))
            {
                Db.AddText(cmd, "@Name", a.Name, 60);
                Db.AddMoney(cmd, "@Price", a.Price);
                Db.AddText(cmd, "@Kind", a.Kind, 10);
                Db.AddBit(cmd, "@IsActive", a.IsActive);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static bool Update(AddOn a)
        {
            using (var conn = Db.Open())
            using (var cmd = Db.Proc(conn, null, "dbo.usp_AddOns_Update"))
            {
                Db.AddInt(cmd, "@AddOnId", a.AddOnId);
                Db.AddText(cmd, "@Name", a.Name, 60);
                Db.AddMoney(cmd, "@Price", a.Price);
                Db.AddBit(cmd, "@IsActive", a.IsActive);
                return Db.RowsAffected(cmd) == 1;
            }
        }
    }
}
