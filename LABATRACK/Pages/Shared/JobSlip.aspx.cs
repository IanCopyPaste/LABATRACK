using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LABATRACK.DataAccess;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Pages.Shared
{
    // The printable job order slip and bag tag for one job (?id=<JobId>). Everything on it comes
    // from the saved job: the frozen prices, its add-ons, and its payments. The cash received is
    // stored on each payment, so a reprint shows the same cash and change as the first print.
    // A job with a balance says what is still due, so the customer knows before pick-up.
    public partial class JobSlip : BasePage
    {
        protected override string PageStyleSheet
        {
            get { return "JobSlip.css"; }
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected JobOrder Job { get; private set; }
        protected ShopSettings Shop { get; private set; }
        protected Payment DropOff { get; private set; }
        protected bool IsVoided { get; private set; }
        protected bool MinimumApplied { get; private set; }
        protected bool NothingPaid { get; private set; }

        /// <summary>The usual case: one payment at drop-off for the whole total, and nothing since.</summary>
        protected bool PaidInOneGo { get; private set; }

        /// <summary>The stamp under the payments: how the job stands.</summary>
        protected string Stamp
        {
            get
            {
                if (IsVoided) return "VOIDED · REFUNDED";
                if (Job.PaymentStatus == PaymentService.Paid) return "PAID IN FULL";
                return Job.PaymentStatus == PaymentService.PartiallyPaid ? "PARTIALLY PAID" : "UNPAID";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            Job = int.TryParse(Request.QueryString["id"], out id) ? JobOrderRepository.GetById(id) : null;
            if (Job == null)
            {
                Response.Redirect("History.aspx", true);
                return;
            }

            Shop = SettingsRepository.Get();
            IsVoided = Job.CurrentStatus == StatusService.Voided;
            MinimumApplied = PricingService.Quote(Job.WeightKg, Job.RatePerKgAtCreation, Job.MinimumChargeAtCreation,
                                                  Job.RoundingIncrementKgAtCreation, null).MinimumApplied;

            List<Payment> payments = PaymentRepository.GetByJob(Job.JobId);
            DropOff = payments.FirstOrDefault(p => p.Kind == Payment.DropOff);
            NothingPaid = payments.Count == 0;
            PaidInOneGo = payments.Count == 1 && DropOff != null && DropOff.Amount == Job.TotalAmount && !IsVoided;

            rptAddOns.DataSource = JobOrderRepository.GetAddOns(Job.JobId);
            rptAddOns.DataBind();
            rptPayments.DataSource = payments;
            rptPayments.DataBind();

            // The claim number goes in the title, so a saved PDF or a browser tab says which job it is.
            Title = "Slip " + Job.ClaimNumber + " · LabaTrack";
        }

        protected static string Money(decimal amount)
        {
            return amount.ToString("N2", Inv);
        }

        protected static string SignedMoney(decimal amount)
        {
            return (amount < 0 ? "−" : "+") + Math.Abs(amount).ToString("N2", Inv);
        }

        protected static string Kg(decimal kg)
        {
            return kg.ToString("0.0#", Inv);
        }

        protected static decimal LineAmount(JobAddOn line)
        {
            return PricingService.LineAmount(line.Quantity, line.PriceAtCreation);
        }

        protected static string PaymentLabel(Payment p)
        {
            if (p.Kind == Payment.DropOff) return "Paid at drop-off (" + p.Method + ")";
            if (p.Kind == Payment.BalancePayment) return "Balance paid " + p.PaidAt.ToString("MMM d", Inv) + " (" + p.Method + ")";
            if (p.Kind == Payment.VoidRefund) return "Refunded on void (" + p.Method + ")";
            return (p.Amount > 0 ? "Collected on correction (" : "Refunded on correction (") + p.Method + ")";
        }
    }
}
