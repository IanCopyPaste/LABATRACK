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
        <%-- Owner sign-in, in dark slate (.auth-owner) so it can't be mistaken for the staff page in
             the counter blue. This is the only page under Admin/ that anyone can open, so the owner's
             password reset lives here too, as three more states of the same card: ask for a link
             (?forgot=1), choose a new password from the emailed link (?token=...), and a link that no
             longer works. Only the owner resets by email; staff ask the owner. --%>
        <div class="auth auth-owner">
            <div class="auth-brand">
                <div class="brand-mark"><%= Icons.Get("washing-machine") %></div>
                <div>
                    <div class="brand-name">LabaTrack</div>
                    <div class="brand-sub">Owner console</div>
                </div>
            </div>

            <main class="auth-card">
                <asp:Panel ID="pnlSignIn" runat="server">
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
                        <%-- A staff account that opened an owner page is sent here; it says so plainly. --%>
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
                            <a class="auth-aside" href="Login.aspx?forgot=1">Forgot password?</a>
                        </div>
                        <asp:Button ID="btnSignIn" runat="server" Text="Sign in" CssClass="btn btn-primary btn-block" OnClick="btnSignIn_Click" />
                    </div>
                </asp:Panel>

                <%-- Step 1: ask for a link. The answer is the same whatever is typed, so the page never
                     shows which usernames exist or which accounts are owners. --%>
                <asp:Panel ID="pnlForgot" runat="server" Visible="false">
                    <div class="auth-role">
                        <span class="auth-role-icon"><%= Icons.Get("key-round") %></span>
                        <div>
                            <h1 class="auth-title">Reset your password</h1>
                            <p class="auth-sub">Owner account only.</p>
                        </div>
                    </div>

                    <div class="form auth-form">
                        <asp:Panel ID="pnlSent" runat="server" Visible="false" CssClass="notice notice-info">
                            <%= Icons.Get("mail", "ico-sm") %>
                            <span>If that is the owner account, a reset link is on its way to its recovery email. The link works once, within 30 minutes.</span>
                        </asp:Panel>
                        <asp:Panel ID="pnlForgotForm" runat="server" CssClass="form">
                            <p class="small muted">Enter your owner username. A link to choose a new password is sent to the recovery email set in Settings.</p>
                            <div class="field">
                                <label for="txtResetUsername">Username</label>
                                <asp:TextBox ID="txtResetUsername" runat="server" MaxLength="50" autocomplete="username" />
                                <asp:Label ID="lblResetUsernameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                            </div>
                            <asp:Button ID="btnSendReset" runat="server" Text="Send reset link" CssClass="btn btn-primary btn-block" OnClick="btnSendReset_Click" />
                        </asp:Panel>
                    </div>

                    <p class="auth-foot"><a href="Login.aspx">Back to sign in</a>. Staff who forgot their password ask the owner to reset it.</p>
                </asp:Panel>

                <%-- Step 2: the page the emailed link opens. --%>
                <asp:Panel ID="pnlReset" runat="server" Visible="false">
                    <div class="auth-role">
                        <span class="auth-role-icon"><%= Icons.Get("key-round") %></span>
                        <div>
                            <h1 class="auth-title">Choose a new password</h1>
                            <p class="auth-sub">For the owner account. You'll sign in with it next.</p>
                        </div>
                    </div>

                    <div class="form auth-form">
                        <div class="field">
                            <label for="txtResetNew">New password</label>
                            <div class="input-reveal">
                                <asp:TextBox ID="txtResetNew" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <button type="button" class="reveal-btn" data-reveal="txtResetNew" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                            </div>
                            <asp:Label ID="lblResetNewError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                            <span class="hint">At least 8 characters.</span>
                        </div>
                        <div class="field">
                            <label for="txtResetConfirm">Confirm new password</label>
                            <div class="input-reveal">
                                <asp:TextBox ID="txtResetConfirm" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <button type="button" class="reveal-btn" data-reveal="txtResetConfirm" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                            </div>
                            <asp:Label ID="lblResetConfirmError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                        </div>
                        <asp:Button ID="btnSetPassword" runat="server" Text="Set new password" CssClass="btn btn-primary btn-block" OnClick="btnSetPassword_Click" />
                    </div>

                    <p class="auth-foot"><a href="Login.aspx">Back to sign in</a></p>
                </asp:Panel>

                <%-- A link that is expired, already used, or made up. --%>
                <asp:Panel ID="pnlExpired" runat="server" Visible="false">
                    <div class="auth-role">
                        <span class="auth-role-icon"><%= Icons.Get("circle-alert") %></span>
                        <div>
                            <h1 class="auth-title">This link no longer works</h1>
                            <p class="auth-sub">Owner password reset</p>
                        </div>
                    </div>
                    <div class="form auth-form">
                        <div class="notice notice-warn">
                            <%= Icons.Get("circle-alert", "ico-sm") %>
                            <span>The reset link has expired or was already used. Each link works once, within 30 minutes of being sent.</span>
                        </div>
                        <a class="btn btn-primary btn-block" href="Login.aspx?forgot=1">Send a new link</a>
                    </div>
                    <p class="auth-foot"><a href="Login.aspx">Back to sign in</a></p>
                </asp:Panel>
            </main>

            <p class="auth-note">Quezon City University &middot; IPT102</p>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/ShowPassword.js") %>"></script>
</body>
</html>
