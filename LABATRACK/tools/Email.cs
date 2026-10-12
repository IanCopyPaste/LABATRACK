using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text;
using LABATRACK.db_conn;
using LABATRACK.models;

namespace LABATRACK.tools
{
    public static class Email
    {
        private const string PlaceholderMark = "PUT-YOUR-";

        public static bool IsConfigured
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Setting("Smtp.PickupDirectory")))
                    return Sender() != null;
                return !string.IsNullOrWhiteSpace(Setting("Smtp.Host"))
                    && Sender() != null
                    && !string.IsNullOrWhiteSpace(Setting("Smtp.Password"));
            }
        }

        public static bool IsValidAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
            try
            {
                MailAddress address = new MailAddress(email);
                return address.Address == email && address.Host.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void SendDropOff(int jobId, int userId, bool isResend)
        {
            SendToCustomer(jobId, NotificationLogEntry.DropOffKind, userId, isResend, null, null);
        }

        public static void SendReady(int jobId, int userId, bool isResend)
        {
            SendToCustomer(jobId, NotificationLogEntry.ReadyKind, userId, isResend, null, null);
        }

        public static void SendModified(int jobId, int userId, EditSummary summary)
        {
            SendToCustomer(jobId, NotificationLogEntry.ModifiedKind, userId, false, summary, null);
        }

        public static string SendVoided(int jobId, int userId, bool isResend, string refundCode)
        {
            return SendToCustomer(jobId, NotificationLogEntry.VoidedKind, userId, isResend, null, refundCode);
        }

        private static string SendToCustomer(int jobId, string kind, int userId, bool isResend, EditSummary summary, string refundCode)
        {
            string recipient = null;
            try
            {
                JobOrder job = null;
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_JobOrders_GetById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                            job = JobOrder.FromRow(r);
                    }
                }
                if (job == null) return null;
                recipient = job.CustomerEmail;

                if (string.IsNullOrWhiteSpace(recipient))
                {
                    Log(jobId, kind, null, NotificationLogEntry.Skipped, "The customer has no email address.", isResend, userId);
                    return NotificationLogEntry.Skipped;
                }
                if (!IsConfigured)
                {
                    Log(jobId, kind, recipient, NotificationLogEntry.Failed, "Email is not set up yet: put the shop's email address and app password in Secrets.config.", isResend, userId);
                    return NotificationLogEntry.Failed;
                }

                ShopSettings settings = new ShopSettings();
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Settings_GetAll", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            if ((string)r["SettingKey"] == ShopSettings.ShopNameKey && r["SettingValue"] as string != null)
                                settings.ShopName = (string)r["SettingValue"];
                        }
                    }
                }

                string subject;
                string body;
                switch (kind)
                {
                    case NotificationLogEntry.ReadyKind:
                        subject = EmailTemplates.ReadySubject(settings, job);
                        body = EmailTemplates.ReadyBody(settings, job);
                        break;
                    case NotificationLogEntry.ModifiedKind:
                        subject = EmailTemplates.ModifiedSubject(settings, job);
                        body = EmailTemplates.ModifiedBody(settings, job, summary);
                        break;
                    case NotificationLogEntry.VoidedKind:
                        decimal cash = 0m;
                        decimal ewallet = 0m;
                        if (job.RefundPending)
                        {
                            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetJobTotals", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                                conn.Open();
                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    if (r.Read())
                                    {
                                        cash = (decimal)r["CashPaid"];
                                        ewallet = (decimal)r["EwalletPaid"];
                                    }
                                }
                            }
                        }
                        else if (job.RefundStatus == Refund.Released)
                        {
                            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                            using (SqlCommand cmd = new SqlCommand("dbo.usp_Payments_GetByJob", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;

                                conn.Open();
                                using (SqlDataReader r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        if ((string)r["Kind"] != Payment.VoidRefund) continue;
                                        decimal amount = (decimal)r["Amount"];
                                        if ((string)r["Method"] == Payment.Cash) cash -= amount; else ewallet -= amount;
                                    }
                                }
                            }
                        }
                        subject = EmailTemplates.VoidedSubject(settings, job);
                        body = EmailTemplates.VoidedBody(settings, job, cash, ewallet, refundCode);
                        break;
                    default:
                        subject = EmailTemplates.DropOffSubject(settings, job);
                        body = EmailTemplates.DropOffBody(settings, job);
                        break;
                }

                Send(recipient, subject, body);
                Log(jobId, kind, recipient, NotificationLogEntry.Sent, null, isResend, userId);
                return NotificationLogEntry.Sent;
            }
            catch (Exception ex)
            {
                try
                {
                    Log(jobId, kind, recipient, NotificationLogEntry.Failed, ex.Message, isResend, userId);
                }
                catch (Exception)
                {
                }
                return NotificationLogEntry.Failed;
            }
        }

        private static void Log(int jobId, string kind, string recipient, string status, string detail, bool isResend, int userId)
        {
            if (detail != null && detail.Length > 400) detail = detail.Substring(0, 400);

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_NotificationLog_Insert", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@Kind", SqlDbType.NVarChar, 10).Value = kind;
                cmd.Parameters.Add("@Recipient", SqlDbType.NVarChar, 254).Value = recipient == null ? (object)DBNull.Value : recipient;
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 10).Value = status;
                cmd.Parameters.Add("@Detail", SqlDbType.NVarChar, 400).Value = detail == null ? (object)DBNull.Value : detail;
                cmd.Parameters.Add("@IsResend", SqlDbType.Bit).Value = isResend;
                cmd.Parameters.Add("@AttemptedByUserId", SqlDbType.Int).Value = userId;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private static void Send(string to, string subject, string htmlBody)
        {
            using (MailMessage message = new MailMessage())
            {
                message.From = new MailAddress(Sender(), Setting("Smtp.FromName") ?? "LabaTrack");
                message.To.Add(new MailAddress(to));
                message.Subject = subject;
                message.SubjectEncoding = Encoding.UTF8;
                message.Body = htmlBody;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;

                using (SmtpClient client = new SmtpClient())
                {
                    string pickup = Setting("Smtp.PickupDirectory");
                    if (!string.IsNullOrWhiteSpace(pickup))
                    {
                        client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                        client.PickupDirectoryLocation = pickup;
                    }
                    else
                    {
                        int port;
                        client.Host = Setting("Smtp.Host");
                        if (int.TryParse(Setting("Smtp.Port"), out port))
                            client.Port = port;
                        else
                            client.Port = 587;
                        client.EnableSsl = !string.Equals(Setting("Smtp.EnableSsl"), "false", StringComparison.OrdinalIgnoreCase);
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.Timeout = 15000;
                        string user = Setting("Smtp.User");
                        if (!string.IsNullOrWhiteSpace(user))
                        {
                            string password = Setting("Smtp.Password") ?? "";
                            if (client.Host.EndsWith("gmail.com", StringComparison.OrdinalIgnoreCase))
                                password = password.Replace(" ", "");
                            client.Credentials = new NetworkCredential(user, password);
                        }
                    }
                    client.Send(message);
                }
            }
        }

        private static string Sender()
        {
            return Setting("Smtp.From") ?? Setting("Smtp.User");
        }

        private static string Setting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf(PlaceholderMark, StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            return value.Trim();
        }
    }
}
