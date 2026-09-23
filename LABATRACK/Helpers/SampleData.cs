using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.SessionState;

namespace LABATRACK.Helpers
{
    /// <summary>
    /// DRAFT ONLY: the sample customers and jobs that History.aspx and Customers.aspx share,
    /// so the two pages always agree with each other. Customers are copied into Session on
    /// first use, so a customer added or edited on one page shows up on the other for the
    /// rest of the visit. Delete this file when Phase 3 and 4 add the repositories.
    /// </summary>
    public static class SampleData
    {
        private const string SessionKey = "DraftCustomers";

        /// <summary>The draft "now": the sample data is a snapshot of Sep 22, 2026 at noon.</summary>
        public static readonly DateTime Now = new DateTime(2026, 9, 22, 12, 0, 0);

        public static List<SampleCustomer> Customers(HttpSessionState session)
        {
            var list = session[SessionKey] as List<SampleCustomer>;
            if (list == null)
            {
                list = SeedCustomers();
                session[SessionKey] = list;
            }
            return list;
        }

        public static List<SampleJob> Jobs()
        {
            return SeedJobs();
        }

        /// <summary>Digits only, so "0917 555 0123" and "09175550123" are the same number.</summary>
        public static string Digits(string contact)
        {
            return new string((contact ?? "").Where(char.IsDigit).ToArray());
        }

        private static List<SampleCustomer> SeedCustomers()
        {
            return new List<SampleCustomer>
            {
                C(1,  "Maria Santos",     "0917 555 0123", "maria.santos@example.com",  At(3, 10, 2)),
                C(2,  "Jose Dela Cruz",   "0918 555 0177", "",                          At(5, 14, 30)),
                C(3,  "Ana Reyes",        "0917 555 0101", "ana.reyes@example.com",     At(6, 9, 15)),
                C(4,  "Carlo Mendoza",    "0927 555 0102", "",                          At(8, 11, 0)),
                C(5,  "Liza Peñaflor",    "0917 555 0142", "liza.penaflor@example.com", At(8, 16, 45)),
                C(6,  "Ramon Bautista",   "0939 555 0104", "",                          At(22, 10, 15)),
                C(7,  "Grace Villanueva", "0917 555 0105", "grace.v@example.com",       At(10, 8, 20)),
                C(8,  "Paolo Garcia",     "0917 555 0108", "paolo.garcia@example.com",  At(11, 13, 5)),
                C(9,  "Nena Aquino",      "0998 555 0109", "",                          At(12, 9, 40)),
                C(10, "Rosa Lim",         "0917 555 0110", "",                          At(12, 16, 20)),
                C(11, "Dennis Uy",        "0918 555 0111", "dennis.uy@example.com",     At(14, 10, 0)),
                C(12, "Mark Tan",         "0917 555 0112", "",                          At(22, 11, 5)),
            };
        }

        // Totals follow the sample rates: Wash-Dry-Fold 40/kg, Wash-Dry 35/kg, Comforter 60/kg,
        // weight rounded up to the next kg, minimum 120, add-ons added after.
        private static List<SampleJob> SeedJobs()
        {
            return new List<SampleJob>
            {
                // Still on the board
                J("LT-260921-004", 1,  "Wash-Dry-Fold",     5.1m, 290m, "Cash",     At(21, 9, 12),  "jcruz",   "Ready for pick-up", null, "", ""),
                J("LT-260921-007", 2,  "Wash-Dry-Fold",     3.2m, 160m, "Cash",     At(21, 15, 40), "jcruz",   "Inspection",        null, "", ""),
                J("LT-260922-001", 3,  "Comforter / Bulky", 2.0m, 120m, "Cash",     At(22, 8, 5),   "jcruz",   "Folding",           null, "", ""),
                J("LT-260922-002", 4,  "Wash-Dry-Fold",     2.3m, 135m, "Cash",     At(22, 8, 31),  "jcruz",   "Drying",            null, "", ""),
                J("LT-260922-003", 5,  "Wash-Dry",          4.6m, 195m, "E-wallet", At(22, 9, 2),   "jcruz",   "Washing",           null, "", ""),
                J("LT-260922-004", 6,  "Wash-Dry-Fold",     7.0m, 280m, "Cash",     At(22, 10, 15), "jcruz",   "Queued",            null, "", ""),
                J("LT-260922-005", 7,  "Wash-Dry-Fold",     1.5m, 120m, "E-wallet", At(22, 10, 48), "jcruz",   "Queued",            null, "", ""),
                J("LT-260912-003", 10, "Wash-Dry-Fold",     4.0m, 160m, "Cash",     At(12, 16, 20), "arivera", "Ready for pick-up", null, "", ""),
                J("LT-260914-008", 11, "Wash-Dry",          5.5m, 210m, "E-wallet", At(14, 10, 0),  "jcruz",   "Ready for pick-up", null, "", ""),

                // Claimed
                J("LT-260920-011", 8,  "Wash-Dry-Fold",     6.0m, 240m, "Cash",     At(20, 14, 10), "arivera", "Claimed", At(22, 10, 5),  "jcruz",   ""),
                J("LT-260921-002", 9,  "Wash-Dry-Fold",     3.0m, 150m, "Cash",     At(21, 8, 40),  "jcruz",   "Claimed", At(22, 11, 42), "jcruz",   ""),
                J("LT-260920-006", 2,  "Wash-Dry-Fold",     2.8m, 120m, "Cash",     At(20, 9, 30),  "jcruz",   "Claimed", At(21, 17, 10), "arivera", ""),
                J("LT-260919-003", 3,  "Wash-Dry-Fold",     5.0m, 220m, "E-wallet", At(19, 11, 0),  "arivera", "Claimed", At(20, 15, 25), "arivera", ""),
                J("LT-260918-001", 5,  "Wash-Dry-Fold",     6.2m, 280m, "Cash",     At(18, 8, 50),  "jcruz",   "Claimed", At(19, 16, 0),  "jcruz",   ""),
                J("LT-260916-005", 4,  "Comforter / Bulky", 3.0m, 180m, "E-wallet", At(16, 13, 15), "arivera", "Claimed", At(17, 10, 30), "jcruz",   ""),
                J("LT-260915-002", 1,  "Wash-Dry-Fold",     4.0m, 160m, "Cash",     At(15, 9, 5),   "jcruz",   "Claimed", At(16, 14, 40), "arivera", ""),
                J("LT-260912-001", 1,  "Wash-Dry",          3.0m, 120m, "Cash",     At(12, 8, 30),  "jcruz",   "Claimed", At(13, 11, 15), "jcruz",   ""),

                // Voided (refunded in full)
                J("LT-260922-006", 12, "Wash-Dry-Fold",     2.5m, 120m, "Cash",     At(22, 11, 5),  "jcruz",   "Voided",  At(22, 11, 20), "jcruz",   "Customer cancelled, wrong bag"),
                J("LT-260918-004", 7,  "Wash-Dry",          2.0m, 120m, "Cash",     At(18, 15, 0),  "arivera", "Voided",  At(18, 15, 8),  "arivera", "Duplicate order, the load was entered twice"),
            };
        }

        private static DateTime At(int day, int hour, int minute)
        {
            return new DateTime(2026, 9, day, hour, minute, 0);
        }

        private static SampleCustomer C(int id, string name, string contact, string email, DateTime created)
        {
            return new SampleCustomer { Id = id, Name = name, Contact = contact, Email = email, CreatedAt = created };
        }

        private static SampleJob J(string claim, int customerId, string service, decimal kg, decimal total, string method,
                                   DateTime droppedOff, string createdBy, string status, DateTime? closedAt, string closedBy, string voidReason)
        {
            return new SampleJob
            {
                Claim = claim, CustomerId = customerId, Service = service, WeightKg = kg, Total = total, Method = method,
                DroppedOff = droppedOff, CreatedBy = createdBy, Status = status, ClosedAt = closedAt, ClosedBy = closedBy,
                VoidReason = voidReason,
            };
        }
    }

    [Serializable]
    public class SampleCustomer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SampleJob
    {
        public string Claim { get; set; }
        public int CustomerId { get; set; }
        public string Service { get; set; }
        public decimal WeightKg { get; set; }
        public decimal Total { get; set; }
        public string Method { get; set; }
        public DateTime DroppedOff { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }       // Queued .. Ready for pick-up, Claimed, or Voided
        public DateTime? ClosedAt { get; set; }  // when it was claimed or voided
        public string ClosedBy { get; set; }
        public string VoidReason { get; set; }
    }
}
