<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LABATRACK.admin.Login" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Owner sign in · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="auth auth-owner">
            <div class="auth-brand">
                <div class="brand-mark"><%= Icons.Get("washing-machine") %></div>
                <div>
                    <div class="brand-name">LabaTrack</div>
                    <div class="brand-sub">Owner console</div>
                </div>
            </div>

            <main class="auth-card">
                <asp:Panel ID="pnlSignIn" runat="server" DefaultButton="btnSignIn">
                    <nav class="auth-switch" aria-label="Sign in as">
                        <a href="../Login.aspx"><%= Icons.Get("user-round", "ico-sm") %>Staff</a>
                        <a href="Login.aspx" class="is-active" aria-current="page"><%= Icons.Get("shield-check", "ico-sm") %>Owner</a>
                    </nav>

                    <div class="auth-role">
                        <span class="auth-role-icon"><%= Icons.Get("shield-check") %></span>
                        <div>
                            <h1 class="auth-title">Owner sign in</h1>
                            <p class="auth-sub">Pricing, staff accounts, and sales reports.</p>
                        </div>
                    </div>

                    <div class="form auth-form">
                        <asp:Panel ID="pnlResetDone" runat="server" Visible="false" CssClass="notice notice-info">
                            <%= Icons.Get("circle-check", "ico-sm") %>
                            <span>Your password was changed. Sign in with the new one.</span>
                        </asp:Panel>
                        <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="notice notice-danger">
                            <%= Icons.Get("circle-alert", "ico-sm") %>
                            <asp:Label ID="lblError" runat="server" Text="Invalid username or password" />
                        </asp:Panel>
                        <asp:Panel ID="pnlStaffSignedIn" runat="server" Visible="false" CssClass="notice notice-warn">
                            <%= Icons.Get("lock", "ico-sm") %>
                            <span>That page is for the owner. You are signed in as <strong><asp:Literal ID="litSignedInAs" runat="server" /></strong> (staff). Sign in with the owner account to open it.</span>
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
                            <asp:LinkButton ID="lnkForgot" runat="server" CssClass="auth-aside" Text="Forgot password?" OnClick="lnkForgot_Click" />
                        </div>
                        <asp:Button ID="btnSignIn" runat="server" Text="Sign in" CssClass="btn btn-primary btn-block" OnClick="btnSignIn_Click" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlForgot" runat="server" Visible="false" DefaultButton="btnReset">
                    <div class="auth-role">
                        <span class="auth-role-icon"><%= Icons.Get("key-round") %></span>
                        <div>
                            <h1 class="auth-title">Reset your password</h1>
                            <p class="auth-sub">Owner account only.</p>
                        </div>
                    </div>

                    <div class="form auth-form">
                        <asp:Panel ID="pnlResetError" runat="server" Visible="false" CssClass="notice notice-danger">
                            <%= Icons.Get("circle-alert", "ico-sm") %>
                            <asp:Label ID="lblResetError" runat="server" />
                        </asp:Panel>
                        <div class="field">
                            <label for="txtResetUsername">Username</label>
                            <asp:TextBox ID="txtResetUsername" runat="server" MaxLength="50" autocomplete="username" />
                        </div>
                        <div class="field">
                            <label for="txtResetEmail">Recovery email</label>
                            <asp:TextBox ID="txtResetEmail" runat="server" MaxLength="254" />
                        </div>
                        <div class="field">
                            <label for="txtResetNew">New password</label>
                            <div class="input-reveal">
                                <asp:TextBox ID="txtResetNew" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <button type="button" class="reveal-btn" data-reveal="txtResetNew" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                            </div>
                            <span class="hint">At least 8 characters.</span>
                        </div>
                        <div class="field">
                            <label for="txtResetConfirm">Confirm new password</label>
                            <div class="input-reveal">
                                <asp:TextBox ID="txtResetConfirm" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <button type="button" class="reveal-btn" data-reveal="txtResetConfirm" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                            </div>
                        </div>
                        <asp:Button ID="btnReset" runat="server" Text="Set new password" CssClass="btn btn-primary btn-block" OnClick="btnReset_Click" />
                    </div>

                    <p class="auth-foot"><asp:LinkButton ID="lnkBack" runat="server" Text="Back to sign in" OnClick="lnkBack_Click" />. Staff who forgot their password ask the owner to reset it.</p>
                </asp:Panel>
            </main>

            <p class="auth-note">Quezon City University &middot; IPT102</p>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/ShowPassword.js") %>"></script>
</body>
</html>
