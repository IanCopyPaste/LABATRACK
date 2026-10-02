using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using LABATRACK.db_conn;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
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
        protected bool PaidInOneGo { get; private set; }

        protected string Stamp
        {
            get
            {
                if (IsVoided) return Job.RefundPending ? "VOIDED · REFUND WAITING" : Job.RefundStatus == null ? "VOIDED" : "VOIDED · REFUNDED";
                if (Job.PaymentStatus == JobOrder.PaidStatus) return "PAID IN FULL";
                if (Job.PaymentStatus == JobOrder.PartiallyPaidStatus)
                    return "PARTIALLY PAID";
                else
                    return "UNPAID";
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(Request.QueryString["id"], out id))
            {
                Response.Redirect("History.aspx", true);
                return;
            }

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = id;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        Job = JobOrder.FromRow(r);
                }
            }

            if (Job == null)
            {
                Response.Redirect("History.aspx", true);
                return;
            }

            Shop = new ShopSettings();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string key = (string)r["SettingKey"];
                        string value = r["SettingValue"] as string;
                        if (value == null) continue;

                        if (key == ShopSettings.ShopNameKey)
                            Shop.ShopName = value;
                        else if (key == ShopSettings.SlipHeaderKey)
                            Shop.SlipHeader = value;
                    }
                }
            }

            IsVoided = Job.CurrentStatus == JobStatus.Voided;

            decimal chargeBeforeMinimum = Math.Round(Job.WeightKg * Job.RatePerKgAtCreation, 2, MidpointRounding.AwayFromZero);
            MinimumApplied = chargeBeforeMinimum < Job.MinimumChargeAtCreation;

            List<Payment> payments = new List<Payment>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = Job.JobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Payment payment = new Payment();
                        payment.PaymentId = (int)r["PaymentId"];
                        payment.JobId = (int)r["JobId"];
                        payment.Kind = (string)r["Kind"];
                        payment.Amount = (decimal)r["Amount"];
                        payment.Method = (string)r["Method"];
                        payment.CashReceived = r["CashReceived"] as decimal?;
                        payment.PaidAt = (DateTime)r["PaidAt"];
                        payment.RecordedByUserId = (int)r["RecordedByUserId"];
                        payment.RecordedBy = (string)r["RecordedBy"];
                        payments.Add(payment);
                    }
                }
            }
            DropOff = payments.FirstOrDefault(p => p.Kind == Payment.DropOff);
            NothingPaid = payments.Count == 0;
            PaidInOneGo = payments.Count == 1 && DropOff != null && DropOff.Amount == Job.TotalAmount && !IsVoided;

            List<JobAddOn> lines = new List<JobAddOn>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_JobAddOns_GetByJob", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = Job.JobId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        JobAddOn line = new JobAddOn();
                        line.JobAddOnId = (int)r["JobAddOnId"];
                        line.JobId = (int)r["JobId"];
                        line.AddOnId = (int)r["AddOnId"];
                        line.Name = (string)r["Name"];
                        line.Kind = (string)r["Kind"];
                        line.Quantity = (int)r["Quantity"];
                        line.PriceAtCreation = (decimal)r["PriceAtCreation"];
                        lines.Add(line);
                    }
                }
            }

            rptAddOns.DataSource = lines;
            rptAddOns.DataBind();
            rptPayments.DataSource = payments;
            rptPayments.DataBind();

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
            return Math.Round(line.Quantity * line.PriceAtCreation, 2, MidpointRounding.AwayFromZero);
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
