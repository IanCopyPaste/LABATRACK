using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using LABATRACK.DataAccess;
using LABATRACK.Models;

namespace LABATRACK.Services
{
    /// <summary>
    /// The rules for customer records. The counter finds people by contact number, so each
    /// number belongs to one customer only: a second record with the same number would split
    /// their history in two. Name and contact number are required; email is optional (without
    /// it, the drop-off and ready emails are logged as Skipped). There is no address, because
    /// the shop does not deliver. Staff can add customers; only the owner can edit one.
    /// </summary>
    public static class CustomerService
    {
        /// <summary>What is wrong with each field; all null when the details are fine.</summary>
        public class Problems
        {
            public string Name { get; set; }
            public string Contact { get; set; }
            public string Email { get; set; }

            public bool Any
            {
                get { return Name != null || Contact != null || Email != null; }
            }

            public string First
            {
                get { return Name ?? Contact ?? Email; }
            }
        }

        /// <summary>The digits of a contact number only: "0917 555-0123" becomes "09175550123".</summary>
        public static string Digits(string contact)
        {
            return new string((contact ?? "").Where(char.IsDigit).ToArray());
        }

        /// <summary>The customer with this contact number (typed with or without spaces), or null.</summary>
        public static Customer FindByContact(string contact)
        {
            string digits = Digits(contact);
            return digits.Length == 0 ? null : CustomerRepository.GetByContactDigits(digits);
        }

        /// <summary>
        /// Checks a customer's details. <paramref name="editingCustomerId"/> is the customer being
        /// edited, whose own number is of course allowed; null for a new customer.
        /// </summary>
        public static Problems Check(string name, string contact, string email, int? editingCustomerId)
        {
            var p = new Problems();
            name = (name ?? "").Trim();
            contact = (contact ?? "").Trim();
            email = (email ?? "").Trim();

            if (name == "")
                p.Name = "Enter the customer's name.";
            else if (name.Length > 100)
                p.Name = "Keep the name to 100 characters.";

            string digits = Digits(contact);
            if (contact == "")
                p.Contact = "Enter a contact number. It is how the customer is found next time.";
            else if (contact.Length > 20 || digits.Length < 7 || digits.Length > 15
                     || contact.Any(ch => !char.IsDigit(ch) && " +-()".IndexOf(ch) < 0))
                p.Contact = "Use digits only, for example 0917 555 0123.";
            else
            {
                Customer owner = CustomerRepository.GetByContactDigits(digits);
                if (owner != null && owner.CustomerId != editingCustomerId)
                    p.Contact = "This number already belongs to " + owner.Name + ". Search for it instead of adding a new customer.";
            }

            if (email != "" && !EmailService.IsValidAddress(email))
                p.Email = "That does not look like an email address. Leave it blank if they have none.";

            return p;
        }

        /// <summary>Adds a new customer and returns its id. Throws BusinessRuleException if a detail is wrong.</summary>
        public static int Add(string name, string contact, string email)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                return Add(conn, null, name, contact, email);
            }
        }

        /// <summary>The same, as one step of a larger save (a new job for a new customer).</summary>
        public static int Add(SqlConnection conn, SqlTransaction tx, string name, string contact, string email)
        {
            Problems p = Check(name, contact, email, null);
            if (p.Any) throw new BusinessRuleException(p.First);
            try
            {
                return CustomerRepository.Insert(conn, tx, name.Trim(), contact.Trim(), EmptyToNull(email));
            }
            catch (SqlException ex) when (IsDuplicate(ex))
            {
                // Someone at the other counter added the same number a moment ago.
                throw new BusinessRuleException("This number was just added for another customer. Search for it instead.");
            }
        }

        /// <summary>The owner corrects a customer's details. The change shows on every past and future job.</summary>
        public static void Update(int customerId, string name, string contact, string email)
        {
            using (SqlConnection conn = new SqlConnection(Db.ConnectionString))
            {
                conn.Open();
                Update(conn, null, customerId, name, contact, email);
            }
        }

        public static void Update(SqlConnection conn, SqlTransaction tx, int customerId, string name, string contact, string email)
        {
            Problems p = Check(name, contact, email, customerId);
            if (p.Any) throw new BusinessRuleException(p.First);
            try
            {
                CustomerRepository.Update(conn, tx, customerId, name.Trim(), contact.Trim(), EmptyToNull(email));
            }
            catch (SqlException ex) when (IsDuplicate(ex))
            {
                throw new BusinessRuleException("This number was just added for another customer.");
            }
        }

        /// <summary>
        /// The Customers page search: part of a name or email, or at least four digits of a
        /// contact number. An empty search lists everyone, A to Z.
        /// </summary>
        public static List<Customer> Search(string query)
        {
            query = (query ?? "").Trim();
            if (query == "") return CustomerRepository.Search(null, null);
            string digits = Digits(query);
            return CustomerRepository.Search(query, digits.Length >= 4 ? digits : null);
        }

        private static string EmptyToNull(string s)
        {
            s = (s ?? "").Trim();
            return s == "" ? null : s;
        }

        // 2601 and 2627: SQL Server refused a second row with the same unique value.
        private static bool IsDuplicate(SqlException ex)
        {
            return ex.Number == 2601 || ex.Number == 2627;
        }
    }
}
