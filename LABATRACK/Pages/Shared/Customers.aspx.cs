using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.DataAccess;
using LABATRACK.Helpers;
using LABATRACK.Models;
using LABATRACK.Security;
using LABATRACK.Services;

namespace LABATRACK.Pages.Shared
{
    // Customers (staff and owner). Staff can add and search; only the owner can edit a customer
    // (CLAUDE.md, Roles). The rules (required fields, one customer per contact number, a valid
    // email) are in CustomerService.
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

        protected int TotalCustomers { get; private set; }
        protected int ShownCustomers { get; private set; }
        protected string Query { get; private set; }
        protected int SavedId { get; private set; }
        protected Customer Editing { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Query = (Request.QueryString["q"] ?? "").Trim();
            int id;
            SavedId = int.TryParse(Request.QueryString["saved"], out id) ? id : 0;

            // Editing is the owner's. A staff account that types ?edit= just gets the add form.
            if (IsOwner && int.TryParse(Request.QueryString["edit"], out id))
                Editing = CustomerRepository.GetById(id);
            btnSave.Text = Editing != null ? "Save changes" : "Add customer";

            if (!IsPostBack)
            {
                txtSearch.Text = Query;
                if (Editing != null)
                {
                    txtName.Text = Editing.Name;
                    txtContact.Text = Editing.ContactNumber;
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
            CustomerService.Problems p = CustomerService.Check(txtName.Text, txtContact.Text, txtEmail.Text,
                                                               Editing != null ? Editing.CustomerId : (int?)null);
            if (p.Any)
            {
                if (p.Name != null) Fail(lblNameError, p.Name);
                if (p.Contact != null) Fail(lblContactError, p.Contact);
                if (p.Email != null) Fail(lblEmailError, p.Email);
                return;
            }

            int savedId;
            try
            {
                if (Editing != null)
                {
                    CustomerService.Update(Editing.CustomerId, txtName.Text, txtContact.Text, txtEmail.Text);
                    savedId = Editing.CustomerId;
                }
                else
                {
                    savedId = CustomerService.Add(txtName.Text, txtContact.Text, txtEmail.Text);
                }
            }
            catch (BusinessRuleException ex)
            {
                Fail(lblContactError, ex.Message);
                return;
            }

            Response.Redirect("Customers.aspx?saved=" + savedId + (Editing != null ? "&was=edit" : ""), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static void Fail(Label label, string message)
        {
            label.Text = HttpUtility.HtmlEncode(message);
            label.Visible = true;
        }

        private void ShowSavedNotice()
        {
            if (SavedId == 0) return;
            Customer saved = CustomerRepository.GetById(SavedId);
            if (saved == null) return;
            string verb = Request.QueryString["was"] == "edit" ? "Saved changes to " : "Added ";
            litNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(verb + saved.Name + ".") + "</span></div>";
        }

        private void BindList()
        {
            // Voided jobs are not visits, so they do not count toward a customer's orders (see the procedure).
            List<Customer> rows = CustomerService.Search(Query);
            TotalCustomers = CustomerRepository.Count();
            ShownCustomers = rows.Count;
            rptCustomers.DataSource = rows;
            rptCustomers.DataBind();
            pnlEmpty.Visible = rows.Count == 0;
        }

        // ---- Used by the markup ----

        protected string RowCss(int id)
        {
            if (Editing != null && Editing.CustomerId == id) return "is-editing";
            return id == SavedId ? "is-saved" : "";
        }

        protected static string LastVisit(DateTime? at)
        {
            if (!at.HasValue) return "<span class=\"muted\">No orders yet</span>";
            DateTime d = at.Value;
            return d.Date == DateTime.Today ? "Today" : d.ToString("MMM d, yyyy");
        }
    }
}
