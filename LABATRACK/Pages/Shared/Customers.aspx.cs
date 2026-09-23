using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using LABATRACK.Helpers;
using LABATRACK.Security;

namespace LABATRACK.Pages.Shared
{
    // DRAFT: customers from Helpers/SampleData, kept in Session so an added or edited customer
    // lasts for the visit and shows on History too. Phase 3 replaces SampleData with
    // CustomerRepository; the validation below stays as it is and moves into the save path.
    //
    // The page state lives in the query string: ?q= is the search, ?edit=<id> opens a
    // customer in the form (owner only), and ?saved=<id> marks the row just saved.
    public partial class Customers : BasePage
    {
        protected override string ActiveNav
        {
            get { return "Customers"; }
        }

        protected override string PageStyleSheet
        {
            get { return "Customers.css"; }
        }

        // DRAFT: Phase 2 takes the role from the signed-in account. Staff can add and search
        // customers; only the owner can edit one (CLAUDE.md, Roles).
        protected bool IsOwner { get { return true; } }

        protected int TotalCustomers { get; private set; }
        protected int ShownCustomers { get; private set; }
        protected string Query { get; private set; }
        protected int SavedId { get; private set; }
        protected SampleCustomer Editing { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Query = (Request.QueryString["q"] ?? "").Trim();
            int id;
            SavedId = int.TryParse(Request.QueryString["saved"], out id) ? id : 0;

            // Editing is the owner's. A staff account that types ?edit= just gets the add form.
            if (IsOwner && int.TryParse(Request.QueryString["edit"], out id))
                Editing = SampleData.Customers(Session).FirstOrDefault(c => c.Id == id);
            btnSave.Text = Editing != null ? "Save changes" : "Add customer";

            if (!IsPostBack)
            {
                txtSearch.Text = Query;
                if (Editing != null)
                {
                    txtName.Text = Editing.Name;
                    txtContact.Text = Editing.Contact;
                    txtEmail.Text = Editing.Email;
                }
                ShowSavedNotice();
            }

            BindList();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim();
            Response.Redirect("Customers.aspx" + (q == "" ? "" : "?q=" + HttpUtility.UrlEncode(q)), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            var customers = SampleData.Customers(Session);
            string name = txtName.Text.Trim();
            string contact = txtContact.Text.Trim();
            string email = txtEmail.Text.Trim();
            bool ok = true;

            if (name == "")
                ok = Fail(lblNameError, "Enter the customer's name.");

            string digits = SampleData.Digits(contact);
            if (contact == "")
                ok = Fail(lblContactError, "Enter a contact number. It is how the customer is found next time.");
            else if (digits.Length < 7 || digits.Length > 15 || contact.Any(ch => !char.IsDigit(ch) && " +-()".IndexOf(ch) < 0))
                ok = Fail(lblContactError, "Use digits only, for example 0917 555 0123.");
            else
            {
                // One customer per number: the counter finds people by contact number, so a
                // second record with the same number would split their history in two.
                var taken = customers.FirstOrDefault(c => SampleData.Digits(c.Contact) == digits
                                                       && (Editing == null || c.Id != Editing.Id));
                if (taken != null)
                    ok = Fail(lblContactError, "This number already belongs to " + taken.Name + ". Search for it instead of adding a new customer.");
            }

            if (email != "" && !IsEmail(email))
                ok = Fail(lblEmailError, "That does not look like an email address. Leave it blank if they have none.");

            if (!ok) return;

            SampleCustomer saved;
            if (Editing != null)
            {
                saved = Editing;
            }
            else
            {
                saved = new SampleCustomer { Id = customers.Max(c => c.Id) + 1, CreatedAt = SampleData.Now };
                customers.Add(saved);
            }
            saved.Name = name;
            saved.Contact = contact;
            saved.Email = email;

            Response.Redirect("Customers.aspx?saved=" + saved.Id + (Editing != null ? "&was=edit" : ""), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static bool Fail(System.Web.UI.WebControls.Label label, string message)
        {
            label.Text = HttpUtility.HtmlEncode(message);
            label.Visible = true;
            return false;
        }

        private static bool IsEmail(string value)
        {
            try
            {
                var address = new MailAddress(value);
                return address.Address == value && address.Host.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ShowSavedNotice()
        {
            var saved = SampleData.Customers(Session).FirstOrDefault(c => c.Id == SavedId);
            if (saved == null) return;
            string verb = Request.QueryString["was"] == "edit" ? "Saved changes to " : "Added ";
            litNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(verb + saved.Name + ".") + "</span></div>";
        }

        private void BindList()
        {
            var customers = SampleData.Customers(Session);
            // Voided jobs are not visits, so they do not count toward a customer's orders.
            var jobs = SampleData.Jobs().Where(j => j.Status != "Voided").ToList();

            string term = Query.ToLowerInvariant();
            string digits = SampleData.Digits(Query);
            var rows = customers
                .Where(c => term == ""
                         || c.Name.ToLowerInvariant().Contains(term)
                         || (digits.Length >= 4 && SampleData.Digits(c.Contact).Contains(digits))
                         || (c.Email ?? "").ToLowerInvariant().Contains(term))
                .Select(c =>
                {
                    var theirs = jobs.Where(j => j.CustomerId == c.Id).ToList();
                    return new CustomerRow
                    {
                        Id = c.Id, Name = c.Name, Contact = c.Contact, Email = c.Email, CreatedAt = c.CreatedAt,
                        JobCount = theirs.Count,
                        LastDropOff = theirs.Count == 0 ? (DateTime?)null : theirs.Max(j => j.DroppedOff),
                    };
                })
                .OrderBy(r => r.Name)
                .ToList();

            TotalCustomers = customers.Count;
            ShownCustomers = rows.Count;
            rptCustomers.DataSource = rows;
            rptCustomers.DataBind();
            pnlEmpty.Visible = rows.Count == 0;
        }

        // ---- Used by the markup ----

        protected string RowCss(int id)
        {
            if (Editing != null && Editing.Id == id) return "is-editing";
            return id == SavedId ? "is-saved" : "";
        }

        protected static string LastVisit(DateTime? at)
        {
            if (!at.HasValue) return "<span class=\"muted\">No orders yet</span>";
            DateTime d = at.Value;
            return d.Date == SampleData.Now.Date ? "Today" : d.ToString("MMM d, yyyy");
        }

        public class CustomerRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Contact { get; set; }
            public string Email { get; set; }
            public DateTime CreatedAt { get; set; }
            public int JobCount { get; set; }
            public DateTime? LastDropOff { get; set; }
        }
    }
}
