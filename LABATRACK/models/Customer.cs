using System;

namespace LABATRACK.models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }

        public int JobCount { get; set; }
        public DateTime? LastDropOff { get; set; }
    }
}
