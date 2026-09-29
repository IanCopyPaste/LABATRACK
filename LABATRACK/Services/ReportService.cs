using System;
using System.Collections.Generic;
using System.Linq;
using LABATRACK.DataAccess;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// Gathers the figures for the dashboards, the daily sales report and the owner's reports.
    /// Two date rules (CLAUDE.md, Screens): money is counted on the day it was paid
    /// (Payments.PaidAt), which is what the cash drawer check needs; services, add-ons, load
    /// sizes, staff and customers are counted on the day the job was created. A day is always
    /// the half-open range [midnight, next midnight).
    /// </summary>
    public static class ReportService
    {
        // ------------------------------------------------------------ Dashboards

        /// <summary>Everything on the staff job board and the owner's dashboard.</summary>
        public class DashboardData
        {
            public List<JobOrder> Active { get; set; }         // Queued to Ready for pick-up, oldest first
            public DaySales Today { get; set; }                // today's payments, net of refunds
            public List<JobOrder> ClaimedToday { get; set; }
            public List<JobOrder> VoidedToday { get; set; }
            public List<JobOrder> Unclaimed { get; set; }      // ready for UnclaimedDays days or more
            public int UnclaimedDays { get; set; }
            public List<JobOrder> RefundsWaiting { get; set; } // voided jobs whose refund the customer has not collected yet
            public DateTime Now { get; set; }

            public int CountAt(string stage)
            {
                return Active.Count(j => j.CurrentStatus == stage);
            }

            /// <summary>Refunds waiting from an earlier day. Today's voids are already listed as voided today.</summary>
            public List<JobOrder> RefundsWaitingEarlier
            {
                get { return RefundsWaiting.Where(j => VoidedToday.All(v => v.JobId != j.JobId)).ToList(); }
            }

            /// <summary>The money the shop is holding that belongs to customers whose refund is waiting.</summary>
            public decimal RefundsWaitingTotal
            {
                get { return RefundsWaiting.Sum(j => j.RefundAmount); }
            }

            public int NeedsAttention
            {
                get { return Unclaimed.Count + VoidedToday.Count + RefundsWaitingEarlier.Count; }
            }
        }

        /// <summary>
        /// The staff board and the owner's dashboard both call this one method, so they always
        /// agree. The board is filtered by status, not by date: a load dropped off last week stays
        /// on it until it is claimed or voided.
        /// </summary>
        public static DashboardData GetDashboard(DateTime now)
        {
            DateTime today = now.Date, tomorrow = today.AddDays(1);
            ShopSettings settings = SettingsRepository.Get();
            int days = Math.Max(1, settings.UnclaimedThresholdDays);

            return new DashboardData
            {
                Now = now,
                Active = JobOrderRepository.GetActive(),
                Today = PaymentRepository.GetDaySummary(today, tomorrow),
                ClaimedToday = JobOrderRepository.GetClosedBetween(StatusService.Claimed, today, tomorrow),
                VoidedToday = JobOrderRepository.GetClosedBetween(StatusService.Voided, today, tomorrow),
                // Flagged once it has waited that many days: ready on the 15th, flagged on the 22nd
                // with a 7-day threshold. So "ready before midnight of (today - days + 1)".
                Unclaimed = JobOrderRepository.GetReadyBefore(today.AddDays(1 - days)),
                UnclaimedDays = days,
                RefundsWaiting = JobOrderRepository.GetPendingRefunds(),
            };
        }

        /// <summary>Whole days a load has waited since it became ready (Sep 13 to Sep 22 is 9).</summary>
        public static int DaysWaiting(DateTime readySince, DateTime now)
        {
            return (now.Date - readySince.Date).Days;
        }

        // ------------------------------------------------------------ Daily sales report

        public class DailyReport
        {
            public DateTime Day { get; set; }
            public DateTime Cutoff { get; set; }               // the end of the day, or "now" while it is still open
            public DaySales Sales { get; set; }                // counted on PaidAt
            public List<Payment> Ledger { get; set; }          // every payment and refund, oldest first
            public List<JobOrder> Jobs { get; set; }           // jobs dropped off that day, voided included
            public DaySales SameDayLastWeek { get; set; }      // the same weekday a week before, up to the same time
        }

        /// <summary>
        /// One day of payments for the printed report. While the day is still open it runs up to
        /// now, and is compared with the same weekday a week before up to the same time, so a
        /// morning is never compared with a whole day.
        /// Hand check: the net total equals SELECT SUM(Amount) FROM Payments
        /// WHERE PaidAt >= @day AND PaidAt &lt; @nextDay.
        /// </summary>
        public static DailyReport GetDailyReport(DateTime day, DateTime now)
        {
            DateTime from = day.Date, to = from.AddDays(1);
            DateTime cutoff = now < to ? now : to;
            return new DailyReport
            {
                Day = from,
                Cutoff = cutoff,
                Sales = PaymentRepository.GetDaySummary(from, to),
                Ledger = PaymentRepository.GetBetween(from, to),
                Jobs = JobOrderRepository.GetCreatedBetween(from, to),
                SameDayLastWeek = PaymentRepository.GetDaySummary(from.AddDays(-7), cutoff.AddDays(-7)),
            };
        }

        // ------------------------------------------------------------ Owner's reports

        /// <summary>The day of the first job, the start of every report; today if there are no jobs yet.</summary>
        public static DateTime FirstDay(DateTime now)
        {
            DateTime? first = ReportRepository.GetFirstJobDate();
            return first.HasValue && first.Value < now ? first.Value.Date : now.Date;
        }

        /// <summary>Payments and refunds within [from, to), for the sales charts (by payment date).</summary>
        public static List<ReportPayment> Payments(DateTime from, DateTime to)
        {
            return from < to ? ReportRepository.GetPaymentsBetween(from, to) : new List<ReportPayment>();
        }

        /// <summary>Jobs dropped off within [from, to), for everything counted per job.</summary>
        public static List<ReportJob> Jobs(DateTime from, DateTime to)
        {
            return from < to ? ReportRepository.GetJobsBetween(from, to) : new List<ReportJob>();
        }

        /// <summary>Sales and job orders per calendar year.</summary>
        public static List<YearTotal> YearTotals()
        {
            return ReportRepository.GetYearTotals();
        }

        /// <summary>Loads ready for pick-up longer than the owner's threshold, longest waiting first.</summary>
        public static List<JobOrder> Unclaimed(DateTime now, out int days)
        {
            days = Math.Max(1, SettingsRepository.Get().UnclaimedThresholdDays);
            return JobOrderRepository.GetReadyBefore(now.Date.AddDays(1 - days));
        }
    }
}
