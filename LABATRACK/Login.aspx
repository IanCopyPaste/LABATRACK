<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LABATRACK.Login" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Staff sign in · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server" defaultbutton="btnSignIn">
        <div class="auth auth-staff">
            <div class="auth-brand">
                <div class="brand-mark"><%= Icons.Get("washing-machine") %></div>
                <div>
                    <div class="brand-name">LabaTrack</div>
                    <div class="brand-sub">Counter</div>
                </div>
            </div>

            <main class="auth-card">
                <nav class="auth-switch" aria-label="Sign in as">
                    <a href="Login.aspx" class="is-active" aria-current="page"><%= Icons.Get("user-round", "ico-sm") %>Staff</a>
                    <a href="admin/Login.aspx"><%= Icons.Get("shield-check", "ico-sm") %>Owner</a>
                </nav>

                <div class="auth-role">
                    <span class="auth-role-icon"><%= Icons.Get("user-round") %></span>
                    <div>
                        <h1 class="auth-title">Staff sign in</h1>
                        <p class="auth-sub">Use the account the owner made for you.</p>
                    </div>
                </div>

                <div class="form auth-form">
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
                        <div class="input-reveal">
                            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="current-password" />
                            <button type="button" class="reveal-btn" data-reveal="txtPassword" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                        </div>
                        <span class="auth-aside muted">Forgot it? Ask the owner to reset it.</span>
                    </div>
                    <asp:Button ID="btnSignIn" runat="server" Text="Sign in" CssClass="btn btn-primary btn-block" OnClick="btnSignIn_Click" />
                </div>
            </main>

            <p class="auth-note">Quezon City University &middot; IPT102</p>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/ShowPassword.js") %>"></script>
</body>
</html>
