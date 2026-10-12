using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using LABATRACK.db_conn;
using LABATRACK.tools;
using LABATRACK.models;
using LABATRACK.security;

namespace LABATRACK.pages.shared
{
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
            if (int.TryParse(Request.QueryString["saved"], out id))
                SavedId = id;
            else
                SavedId = 0;
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

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string nameProblem, contactProblem, emailProblem;
            CheckCustomer(txtName.Text, txtContact.Text, txtEmail.Text, null, out nameProblem, out contactProblem, out emailProblem);
            if (nameProblem != null || contactProblem != null || emailProblem != null)
            {
                if (nameProblem != null) Fail(lblNameError, nameProblem);
                if (contactProblem != null) Fail(lblContactError, contactProblem);
                if (emailProblem != null) Fail(lblEmailError, emailProblem);
                return;
            }

            string email = txtEmail.Text.Trim();
            int savedId;
            try
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Insert", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = txtName.Text.Trim();
                    cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = txtContact.Text.Trim();
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = email == "" ? (object)DBNull.Value : email;

                    conn.Open();
                    savedId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                Fail(lblContactError, "This number was just added for another customer. Search for it instead.");
                return;
            }

            Response.Redirect("Customers.aspx?saved=" + savedId, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        private void SaveEdit()
        {
            if (!IsOwner) return;

            int id;
            if (!int.TryParse(Request.Form["cdId"], out id) || GetCustomerName(id) == null)
                return;

            string name = Request.Form["cdName"] ?? "";
            string contact = Request.Form["cdContact"] ?? "";
            string email = Request.Form["cdEmail"] ?? "";

            string nameProblem, contactProblem, emailProblem;
            CheckCustomer(name, contact, email, id, out nameProblem, out contactProblem, out emailProblem);

            if (nameProblem == null && contactProblem == null && emailProblem == null)
            {
                try
                {
                    string cleanEmail = email.Trim();
                    using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                    using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Update", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = id;
                        cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name.Trim();
                        cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 20).Value = contact.Trim();
                        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 254).Value = cleanEmail == "" ? (object)DBNull.Value : cleanEmail;

                        conn.Open();
                        cmd.ExecuteScalar();
                    }

                    Response.Redirect("Customers.aspx?saved=" + id + "&was=edit", false);
                    Context.ApplicationInstance.CompleteRequest();
                    return;
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    DialogError = "This number was just added for another customer.";
                }
            }
            else
            {
                NameError = nameProblem;
                ContactError = contactProblem;
                EmailError = emailProblem;
            }

            DialogOpen = true;
            DialogId = id.ToString();
            DialogName = name;
            DialogContact = contact;
            DialogEmail = email;
        }

        private void CheckCustomer(string name, string contact, string email, int? editingCustomerId,
                                   out string nameProblem, out string contactProblem, out string emailProblem)
        {
            nameProblem = null;
            contactProblem = null;
            emailProblem = null;
            name = (name ?? "").Trim();
            contact = (contact ?? "").Trim();
            email = (email ?? "").Trim();

            if (name == "")
                nameProblem = "Enter the customer's name.";
            else if (name.Length > 100)
                nameProblem = "Keep the name to 100 characters.";

            string digits = new string(contact.Where(char.IsDigit).ToArray());
            if (contact == "")
            {
                contactProblem = "Enter a contact number. It is how the customer is found next time.";
            }
            else if (contact.Length > 20 || digits.Length < 7 || digits.Length > 15
                     || contact.Any(ch => !char.IsDigit(ch) && " +-()".IndexOf(ch) < 0))
            {
                contactProblem = "Use digits only, for example 0917 555 0123.";
            }
            else
            {
                using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetByContactDigits", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@ContactDigits", SqlDbType.NVarChar, 20).Value = digits;

                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read() && (int)r["CustomerId"] != editingCustomerId)
                            contactProblem = "This number already belongs to " + (string)r["Name"] + ". Search for it instead of adding a new customer.";
                    }
                }
            }

            if (email != "" && !Email.IsValidAddress(email))
                emailProblem = "That does not look like an email address. Leave it blank if they have none.";
        }

        private string GetCustomerName(int customerId)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_GetById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                        return (string)r["Name"];
                    return null;
                }
            }
        }

        private static void Fail(Label label, string message)
        {
            label.Text = HttpUtility.HtmlEncode(message);
            label.Visible = true;
        }

        private void ShowSavedNotice()
        {
            if (SavedId == 0) return;
            string savedName = GetCustomerName(SavedId);
            if (savedName == null) return;
            string verb;
            if (Request.QueryString["was"] == "edit")
                verb = "Saved changes to ";
            else
                verb = "Added ";
            litNotice.Text = "<div class=\"notice notice-info\">" + Icons.Get("circle-check", "ico-sm")
                           + "<span>" + HttpUtility.HtmlEncode(verb + savedName + ".") + "</span></div>";
        }

        private void BindList()
        {
            string text = null;
            string digits = null;
            if (Query != "")
            {
                text = Query;
                string typedDigits = new string(Query.Where(char.IsDigit).ToArray());
                if (typedDigits.Length >= 4) digits = typedDigits;
            }

            List<Customer> rows = new List<Customer>();
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Search", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 100).Value = text == null ? (object)DBNull.Value : text;
                cmd.Parameters.Add("@Digits", SqlDbType.NVarChar, 20).Value = digits == null ? (object)DBNull.Value : digits;

                conn.Open();
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Customer customer = new Customer();
                        customer.CustomerId = (int)r["CustomerId"];
                        customer.Name = (string)r["Name"];
                        customer.ContactNumber = (string)r["ContactNumber"];
                        customer.Email = r["Email"] as string;
                        customer.CreatedAt = (DateTime)r["CreatedAt"];
                        customer.JobCount = Convert.ToInt32(r["JobCount"]);
                        customer.LastDropOff = r["LastDropOff"] as DateTime?;
                        rows.Add(customer);
                    }
                }
            }

            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            using (SqlCommand cmd = new SqlCommand("dbo.usp_Customers_Count", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                TotalCustomers = Convert.ToInt32(cmd.ExecuteScalar());
            }

            ShownCustomers = rows.Count;
            rptCustomers.DataSource = rows;
            rptCustomers.DataBind();
            pnlEmpty.Visible = rows.Count == 0;
        }

        protected string RowCss(int id)
        {
            if (id == SavedId)
                return "is-saved";
            else
                return "";
        }

        protected static string LastVisit(DateTime? at)
        {
            if (!at.HasValue) return "<span class=\"muted\">No orders yet</span>";
            DateTime d = at.Value;
            if (d.Date == DateTime.Today)
                return "Today";
            else
                return d.ToString("MMM d, yyyy");
        }
    }
}
