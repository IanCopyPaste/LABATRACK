namespace LABATRACK.Security
{
    /// <summary>
    /// Base class for every page in Admin/ (except Admin/Login.aspx). Only the owner may run
    /// these pages. There are two locks on the door, both on the server:
    ///   1. Web.config allows the Admin folder to the Owner role only, so ASP.NET turns a staff
    ///      account away before the page is even created.
    ///   2. This class checks again when the page starts, in case the folder rule is ever
    ///      changed or a page is moved.
    /// Hiding the menu link is not security; these checks are. Anyone who is not the signed-in
    /// owner is sent to the owner's login page (Forms authentication knows only one login page,
    /// the staff one, so this class names the owner's itself).
    /// </summary>
    public class AdminBasePage : BasePage
    {
        protected override void CheckAccess()
        {
            if (CurrentUser == null || !CurrentUser.IsOwner)
                RedirectToLogin("~/Admin/Login.aspx");
        }
    }
}
