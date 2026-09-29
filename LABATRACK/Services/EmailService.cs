using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// Sends the shop's emails through SMTP (no SMS): four to customers (at drop-off, with the
    /// claim number; when the owner corrects the order; when the order is cancelled; and when the
    /// load is ready for pick-up) and one to the owner (the password reset link). The message
    /// bodies are built in Helpers/EmailTemplates.cs.
    ///
    /// A failed email never blocks or undoes a job: these methods never throw. Every attempt to
    /// reach a customer is written to NotificationLog as Sent, Failed (with the error) or Skipped
    /// (the customer has no email), and staff can resend from the job page.
    ///
    /// The SMTP settings are secrets, so they are not in Web.config or in source control. They
    /// live in Secrets.config next to Web.config (listed in .gitignore; see README.md):
    ///   Smtp.Host, Smtp.Port, Smtp.EnableSsl, Smtp.User, Smtp.Password, Smtp.FromName
    /// Smtp.User is the shop's email address (it is also the sender) and Smtp.Password is its
    /// app password. Until the two placeholders in Secrets.config are replaced, email counts as
    /// not set up, and every attempt is logged as Failed so it can be resent later.
    /// For testing without an email account, Smtp.PickupDirectory saves each email as a .eml file
    /// in that folder instead of sending it.
    /// </summary>
    public static class EmailService
    {
        // Secrets.config ships with values that start with this, to be replaced by the real ones.
        private const string PlaceholderMark = "PUT-YOUR-";

        /// <summary>
        /// True when Secrets.config has what is needed to send: a mail server with a real address
        /// and app password (not the placeholders), or a test folder.
        /// </summary>
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

        /// <summary>The address emails are sent from: Smtp.From if given, else the login (Smtp.User).</summary>
        private static string Sender()
        {
            return Setting("Smtp.From") ?? Setting("Smtp.User");
        }

        /// <summary>True if <paramref name="email"/> is a single, plain email address.</summary>
        public static bool IsValidAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
            try
            {
                var address = new MailAddress(email);
                return address.Address == email && address.Host.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>The drop-off email: the claim number, the load, the total, and the expected pick-up.</summary>
        public static void SendDropOff(int jobId, int userId, bool isResend)
        {
            SendToCustomer(jobId, NotificationLogEntry.DropOffKind, userId, isResend, null);
        }

        /// <summary>The ready-for-pick-up email, sent when a load passes inspection.</summary>
        public static void SendReady(int jobId, int userId, bool isResend)
        {
            SendToCustomer(jobId, NotificationLogEntry.ReadyKind, userId, isResend, null);
        }

        /// <summary>
        /// The order-updated email, sent after the owner corrects a job: what changed and how the
        /// order stands now. <paramref name="summary"/> is built by JobOrderService.SaveEdit.
        /// </summary>
        public static void SendModified(int jobId, int userId, EditSummary summary)
        {
            SendToCustomer(jobId, NotificationLogEntry.ModifiedKind, userId, false, summary);
        }

        /// <summary>The order-cancelled email, sent when a job is voided: the reason, the refund and what to do next.</summary>
        public static void SendVoided(int jobId, int userId, bool isResend)
        {
            SendToCustomer(jobId, NotificationLogEntry.VoidedKind, userId, isResend, null);
        }

        /// <summary>
        /// The owner's password reset link. It is not about a job, so it is not written to
        /// NotificationLog; a failure is simply swallowed (the page says the same thing anyway).
        /// </summary>
        public static void SendOwnerResetLink(string to, string link)
        {
            try
            {
                if (!IsConfigured) return;
                ShopSettings settings = SettingsRepository.Get();
                Send(to, EmailTemplates.OwnerResetSubject(settings), EmailTemplates.OwnerResetBody(settings, link, AuthService.ResetLinkMinutes));
            }
            catch (Exception)
            {
                // Never let a mail problem show which accounts exist, or break the page.
            }
        }

        private static void SendToCustomer(int jobId, string kind, int userId, bool isResend, EditSummary summary)
        {
            string recipient = null;
            try
            {
                JobOrder job = JobOrderRepository.GetById(jobId);
                if (job == null) return;
                recipient = job.CustomerEmail;

                if (string.IsNullOrWhiteSpace(recipient))
                {
                    Log(jobId, kind, null, NotificationLogEntry.Skipped, "The customer has no email address.", isResend, userId);
                    return;
                }
                if (!IsConfigured)
                {
                    Log(jobId, kind, recipient, NotificationLogEntry.Failed, "Email is not set up yet: put the shop's email address and app password in Secrets.config.", isResend, userId);
                    return;
                }

                ShopSettings settings = SettingsRepository.Get();
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
                        // The refunds come from the job's own payment rows, so a resend says the same as the first email.
                        List<Payment> refunds = PaymentRepository.GetByJob(jobId).Where(p => p.Kind == Payment.VoidRefund).ToList();
                        subject = EmailTemplates.VoidedSubject(settings, job);
                        body = EmailTemplates.VoidedBody(settings, job, refunds);
                        break;
                    default:
                        subject = EmailTemplates.DropOffSubject(settings, job);
                        body = EmailTemplates.DropOffBody(settings, job);
                        break;
                }

                Send(recipient, subject, body);
                Log(jobId, kind, recipient, NotificationLogEntry.Sent, null, isResend, userId);
            }
            catch (Exception ex)
            {
                try
                {
                    Log(jobId, kind, recipient, NotificationLogEntry.Failed, ex.Message, isResend, userId);
                }
                catch (Exception)
                {
                    // Even the log could not be written (the database is down). The job is saved; move on.
                }
            }
        }

        private static void Log(int jobId, string kind, string recipient, string status, string detail, bool isResend, int userId)
        {
            if (detail != null && detail.Length > 400) detail = detail.Substring(0, 400);
            NotificationRepository.Insert(jobId, kind, recipient, status, detail, isResend, userId);
        }

        private static void Send(string to, string subject, string htmlBody)
        {
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(Sender(), Setting("Smtp.FromName") ?? "LabaTrack");
                message.To.Add(new MailAddress(to));
                message.Subject = subject;
                message.SubjectEncoding = Encoding.UTF8;
                message.Body = htmlBody;
                message.BodyEncoding = Encoding.UTF8;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient())
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
                        client.Port = int.TryParse(Setting("Smtp.Port"), out port) ? port : 587;
                        client.EnableSsl = !string.Equals(Setting("Smtp.EnableSsl"), "false", StringComparison.OrdinalIgnoreCase);
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        client.Timeout = 15000;   // milliseconds: a dead mail server must not hold the counter up for long
                        string user = Setting("Smtp.User");
                        if (!string.IsNullOrWhiteSpace(user))
                        {
                            // Google shows an app password as four groups of four letters. The spaces
                            // are only for reading it, so they are taken out.
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

        /// <summary>A value from Secrets.config, or null when it is missing, empty, or still a placeholder.</summary>
        private static string Setting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf(PlaceholderMark, StringComparison.OrdinalIgnoreCase) >= 0)
                return null;
            return value.Trim();
        }
    }
}
