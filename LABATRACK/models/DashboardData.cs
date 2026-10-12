using System;
using System.Collections.Generic;
using System.Linq;

namespace LABATRACK.models
{
    public class DashboardData
    {
        public List<JobOrder> Active { get; set; }
        public DaySales Today { get; set; }
        public List<JobOrder> ClaimedToday { get; set; }
        public List<JobOrder> VoidedToday { get; set; }
        public List<JobOrder> Unclaimed { get; set; }
        public int UnclaimedDays { get; set; }
        public List<JobOrder> RefundsWaiting { get; set; }
        public DateTime Now { get; set; }

        public int CountAt(string stage)
        {
            return Active.Count(j => j.CurrentStatus == stage);
        }

        public List<JobOrder> RefundsWaitingEarlier
        {
            get { return RefundsWaiting.Where(j => VoidedToday.All(v => v.JobId != j.JobId)).ToList(); }
        }


        public int NeedsAttention
        {
            get { return Unclaimed.Count + VoidedToday.Count + RefundsWaitingEarlier.Count; }
        }
    }
}
