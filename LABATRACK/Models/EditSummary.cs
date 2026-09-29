using System.Collections.Generic;

namespace LABATRACK.Models
{
    /// <summary>
    /// What the owner's correction of a job changed, in words, for the email that tells the
    /// customer (Services/EmailService.SendModified). It is built by JobOrderService.SaveEdit at
    /// the moment of the save, because the "before" values are gone afterwards, and is not stored.
    /// </summary>
    public class EditSummary
    {
        public EditSummary()
        {
            Changes = new List<string>();
        }

        /// <summary>One line per thing that changed, for example "Weight: 5.1 kg → 6.5 kg". Empty = nothing to tell.</summary>
        public List<string> Changes { get; private set; }

        /// <summary>If the new total is below what the customer had paid: the excess given back (0 if none).</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>How that refund is given back: Cash or E-wallet.</summary>
        public string RefundMethod { get; set; }
    }
}
