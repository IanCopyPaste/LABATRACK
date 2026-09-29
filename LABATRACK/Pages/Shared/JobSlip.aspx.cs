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
    // stored on the payment, so a reprint shows the same cash and change as the first print.
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
        protected bool HasLaterPayments { get; private set; }   // a correction or a void refund after drop-off

        /// <summary>Change = cash received - total, from the stored drop-off payment.</summary>
        protected decimal Change
        {
            get { return DropOff != null && DropOff.CashReceived.HasValue ? DropOff.CashReceived.Value - DropOff.Amount : 0m; }
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
            HasLaterPayments = payments.Any(p => p.Kind != Payment.DropOff);

            rptAddOns.DataSource = JobOrderRepository.GetAddOns(Job.JobId);
            rptAddOns.DataBind();
            rptLaterPayments.DataSource = payments.Where(p => p.Kind != Payment.DropOff);
            rptLaterPayments.DataBind();

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

        protected static string LaterLabel(Payment p)
        {
            if (p.Kind == Payment.VoidRefund) return "Refunded on void (" + p.Method + ")";
            return (p.Amount > 0 ? "Collected on correction (" : "Refunded on correction (") + p.Method + ")";
        }
    }
}
