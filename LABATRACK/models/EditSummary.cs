using System.Collections.Generic;

namespace LABATRACK.models
{
    public class EditSummary
    {
        public EditSummary()
        {
            Changes = new List<string>();
        }

        public List<string> Changes { get; private set; }

        public decimal RefundAmount { get; set; }

        public string RefundMethod { get; set; }
    }
}
