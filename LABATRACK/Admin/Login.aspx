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

            <%-- This is the only page under Admin/ that anyone can open, so the owner's password reset
                 lives here too, as three more states of the same card: ask for a link (?forgot=1),
                 choose a new password from the emailed link (?token=...), and a link that no longer
                 works. Only the owner resets by email; staff ask the owner. --%>
            <main class="auth-main">
                <div class="auth-card">
                    <span class="pill pill-plain" style="background:var(--primary-soft); color:var(--primary);"><%= Icons.Get("lock", "ico-sm") %>Owner access</span>

                    <asp:Panel ID="pnlSignIn" runat="server">
                        <h1 style="font-size:24px; margin-top:14px;">Sign in</h1>
                        <p class="muted" style="margin-top:4px;">Use your owner account to manage the shop.</p>

                        <div class="form" style="margin-top:28px;">
                            <asp:Panel ID="pnlResetDone" runat="server" Visible="false" CssClass="notice notice-info">
                                <%= Icons.Get("circle-check", "ico-sm") %>
                                <span>Your password was changed. Sign in with the new one.</span>
                            </asp:Panel>
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
                                <a class="small" href="Login.aspx?forgot=1" style="align-self:flex-end;">Forgot password?</a>
                            </div>
                            <asp:Button ID="btnSignIn" runat="server" Text="Sign in" CssClass="btn btn-primary btn-block" OnClick="btnSignIn_Click" style="height:40px;" />
                        </div>

                        <p class="small muted" style="margin-top:28px;">Counter staff sign in on the <a href="../Login.aspx">staff login page</a>.</p>
                    </asp:Panel>

                    <%-- Step 1: ask for a link. The answer is the same whatever is typed, so the page never
                         shows which usernames exist or which accounts are owners. --%>
                    <asp:Panel ID="pnlForgot" runat="server" Visible="false">
                        <h1 style="font-size:24px; margin-top:14px;">Reset your password</h1>
                        <p class="muted" style="margin-top:4px;">Enter your owner username. A link to choose a new password is sent to the recovery email set in Settings.</p>

                        <div class="form" style="margin-top:28px;">
                            <asp:Panel ID="pnlSent" runat="server" Visible="false" CssClass="notice notice-info">
                                <%= Icons.Get("mail", "ico-sm") %>
                                <span>If that is the owner account, a reset link is on its way to its recovery email. The link works once, within 30 minutes.
                                    <span class="muted" style="display:block; margin-top:6px;">Draft: no email is sent yet. <a href="Login.aspx?token=draft">Open the link it would contain</a>.</span></span>
                            </asp:Panel>
                            <asp:Panel ID="pnlForgotForm" runat="server" CssClass="form">
                                <div class="field">
                                    <label for="txtResetUsername">Username</label>
                                    <asp:TextBox ID="txtResetUsername" runat="server" MaxLength="50" autocomplete="username" />
                                    <asp:Label ID="lblResetUsernameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                </div>
                                <asp:Button ID="btnSendReset" runat="server" Text="Send reset link" CssClass="btn btn-primary btn-block" OnClick="btnSendReset_Click" style="height:40px;" />
                            </asp:Panel>
                        </div>

                        <p class="small muted" style="margin-top:28px;"><a href="Login.aspx">Back to sign in</a>. Staff who forgot their password ask the owner to reset it.</p>
                    </asp:Panel>

                    <%-- Step 2: the page the emailed link opens. --%>
                    <asp:Panel ID="pnlReset" runat="server" Visible="false">
                        <h1 style="font-size:24px; margin-top:14px;">Choose a new password</h1>
                        <p class="muted" style="margin-top:4px;">For the owner account. You'll sign in with it straight after.</p>

                        <div class="form" style="margin-top:28px;">
                            <div class="field">
                                <label for="txtResetNew">New password</label>
                                <asp:TextBox ID="txtResetNew" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <asp:Label ID="lblResetNewError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                <span class="hint">At least 8 characters.</span>
                            </div>
                            <div class="field">
                                <label for="txtResetConfirm">Confirm new password</label>
                                <asp:TextBox ID="txtResetConfirm" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                <asp:Label ID="lblResetConfirmError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                            </div>
                            <asp:Button ID="btnSetPassword" runat="server" Text="Set new password" CssClass="btn btn-primary btn-block" OnClick="btnSetPassword_Click" style="height:40px;" />
                        </div>

                        <p class="small muted" style="margin-top:28px;"><a href="Login.aspx">Back to sign in</a></p>
                    </asp:Panel>

                    <%-- A link that is expired, already used, or made up. --%>
                    <asp:Panel ID="pnlExpired" runat="server" Visible="false">
                        <h1 style="font-size:24px; margin-top:14px;">This link no longer works</h1>
                        <div class="notice notice-warn" style="margin-top:20px;">
                            <%= Icons.Get("circle-alert", "ico-sm") %>
                            <span>The reset link has expired or was already used. Each link works once, within 30 minutes of being sent.</span>
                        </div>
                        <a class="btn btn-primary btn-block" href="Login.aspx?forgot=1" style="height:40px; margin-top:20px;">Send a new link</a>
                        <p class="small muted" style="margin-top:28px;"><a href="Login.aspx">Back to sign in</a></p>
                    </asp:Panel>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
