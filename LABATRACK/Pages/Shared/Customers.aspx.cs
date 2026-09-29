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
    // (CLAUDE.md, Roles), in a window of its own (Customers.js). The rules (required fields, one
    // customer per contact number, a valid email) are in CustomerService.
    //
    // The page state lives in the query string: ?q= is the search, and ?saved=<id> marks the row
    // just saved. The Edit window posts cdId, cdName, cdContact and cdEmail with
    // action=save-customer; a save that goes through redirects back here (post, then redirect),
    // and a refused one draws the window open again with what was typed and what is wrong.
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

        // The Edit window's state when the server draws it (open again after a refused save).
        protected bool DialogOpen { get; private set; }
        protected string DialogId { get; private set; }
        protected string DialogName { get; private set; }
        protected string DialogContact { get; private set; }
        protected string DialogEmail { get; private set; }
        protected string DialogError { get; private set; }
        protected string NameError { get; private set; }
        protected string ContactError { get; private set; }
        protected string EmailError { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Query = (Request.QueryString["q"] ?? "").Trim();
            int id;
            SavedId = int.TryParse(Request.QueryString["saved"], out id) ? id : 0;
            DialogId = "";
            DialogName = "";
            DialogContact = "";
            DialogEmail = "";

            if (!IsPostBack)
            {
                txtSearch.Text = Query;
                ShowSavedNotice();
            }
            else if (Request.Form["action"] == "save-customer")
            {
                SaveEdit();
            }

            BindList();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim();
            Response.Redirect("Customers.aspx" + (q == "" ? "" : "?q=" + HttpUtility.UrlEncode(q)), false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ------------------------------------------------------------ Add (the form on top)

        protected void btnSave_Click(object sender, EventArgs e)
        {
            CustomerService.Problems p = CustomerService.Check(txtName.Text, txtContact.Text, txtEmail.Text, null);
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
                savedId = CustomerService.Add(txtName.Text, txtContact.Text, txtEmail.Text);
            }
            catch (BusinessRuleException ex)
            {
                Fail(lblContactError, ex.Message);
                return;
            }

            Response.Redirect("Customers.aspx?saved=" + savedId, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // ------------------------------------------------------------ Edit (the owner's window)

        private void SaveEdit()
        {
            // Editing is the owner's. Staff have no window, and a hand-made post from a staff account is ignored.
            if (!IsOwner) return;

            int id;
            if (!int.TryParse(Request.Form["cdId"], out id) || CustomerRepository.GetById(id) == null)
                return;

            string name = Request.Form["cdName"] ?? "";
            string contact = Request.Form["cdContact"] ?? "";
            string email = Request.Form["cdEmail"] ?? "";

            CustomerService.Problems p = CustomerService.Check(name, contact, email, id);
            if (!p.Any)
            {
                try
                {
                    CustomerService.Update(id, name, contact, email);
                    Response.Redirect("Customers.aspx?saved=" + id + "&was=edit", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                catch (BusinessRuleException ex)
                {
                    DialogError = ex.Message;
                }
            }
            else
            {
                NameError = p.Name;
                ContactError = p.Contact;
                EmailError = p.Email;
            }

            // Draw the window open again, as it was sent.
            DialogOpen = true;
            DialogId = id.ToString();
            DialogName = name;
            DialogContact = contact;
            DialogEmail = email;
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
