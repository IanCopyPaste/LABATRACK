using System;

namespace LABATRACK.Models
{
    /// <summary>One row of the Customers table, with how many jobs they have had.</summary>
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }          // optional: without it, emails are logged as Skipped
        public DateTime CreatedAt { get; set; }

        // Worked out by the query, not stored: jobs that were not voided, and the latest drop-off.
        public int JobCount { get; set; }
        public DateTime? LastDropOff { get; set; }
    }
}
