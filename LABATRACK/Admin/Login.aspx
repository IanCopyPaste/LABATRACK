<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LABATRACK.Admin.Login" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Owner sign in · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server" defaultbutton="btnSignIn">
        <div class="auth">
            <aside class="auth-side">
                <div class="brand" style="padding:0;">
                    <div class="brand-mark"><%= Icons.Get("washing-machine") %></div>
                    <div class="brand-name">LabaTrack</div>
                </div>
                <div>
                    <h2>Every load tracked, from drop-off to pick-up.</h2>
                    <p>The owner console for pricing, staff accounts, and sales reports.</p>
                    <ul class="auth-points">
                        <li><%= Icons.Get("tags") %>Prices frozen on every job order</li>
                        <li><%= Icons.Get("clipboard-list") %>A timestamp and staff name on every status change</li>
                        <li><%= Icons.Get("receipt") %>Daily cash and e-wallet totals for the drawer check</li>
                    </ul>
                </div>
                <div class="small" style="opacity:.75;">Quezon City University &middot; IPT102</div>
            </aside>

            <main class="auth-main">
                <div class="auth-card">
                    <span class="pill pill-plain" style="background:var(--primary-soft); color:var(--primary);"><%= Icons.Get("lock", "ico-sm") %>Owner access</span>
                    <h1 style="font-size:24px; margin-top:14px;">Sign in</h1>
                    <p class="muted" style="margin-top:4px;">Use your owner account to manage the shop.</p>

                    <div class="form" style="margin-top:28px;">
                        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="notice notice-danger">
                            <%= Icons.Get("circle-alert", "ico-sm") %>
                            <asp:Label ID="lblError" runat="server" Text="Invalid username or password" />
                        </asp:Panel>
                        <div class="field">
                            <label for="txtUsername">Username</label>
                            <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" autocomplete="username" />
                        </div>
                        <div class="field">
                            <label for="txtPassword">Password</label>
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="current-password" />
                        </div>
                        <asp:Button ID="btnSignIn" runat="server" Text="Sign in" CssClass="btn btn-primary btn-block" OnClick="btnSignIn_Click" style="height:40px;" />
                    </div>

                    <p class="small muted" style="margin-top:28px;">Counter staff sign in on the staff login page.</p>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
