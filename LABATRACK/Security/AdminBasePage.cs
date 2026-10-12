namespace LABATRACK.security
{
    public class AdminBasePage : BasePage
    {
        protected override void CheckAccess()
        {
            if (CurrentUser == null || !CurrentUser.IsOwner)
                RedirectToLogin("~/admin/Login.aspx");
        }
    }
}
