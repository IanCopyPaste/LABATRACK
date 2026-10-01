<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="LABATRACK.admin.Settings" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Settings · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Settings</h1>
                        <p class="sub">Shop details, the rules the counter screens follow, and your password. Pricing rules are on the Pricing page.</p>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <div class="grid grid-2" style="align-items:start;">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Shop and job slip</h2>
                                        <div class="sub">Printed on every claim slip</div>
                                    </div>
                                    <span class="kpi-icon"><%= Icons.Get("store", "ico-sm") %></span>
                                </div>
                                <div class="card-b form">
                                    <div class="field">
                                        <label for="txtShopName">Shop name</label>
                                        <asp:TextBox ID="txtShopName" runat="server" MaxLength="100" />
                                        <asp:Label ID="lblShopNameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                    </div>
                                    <div class="field">
                                        <label for="txtSlipHeader">Slip header</label>
                                        <asp:TextBox ID="txtSlipHeader" runat="server" TextMode="MultiLine" MaxLength="400" />
                                        <asp:Label ID="lblSlipHeaderError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                    </div>
                                    <div class="field">
                                        <label for="txtClaimFormat">Claim number format</label>
                                        <asp:TextBox ID="txtClaimFormat" runat="server" Text="LT-yyMMdd-###" ReadOnly="true" CssClass="input mono" />
                                        <span class="hint">Example: <%: ClaimExample %>. Fixed in code.</span>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="submit" name="action" value="shop" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save</button>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Your password</h2>
                                        <div class="sub">For the owner account <strong><%: SignedInUser %></strong>, used at the owner login</div>
                                    </div>
                                    <span class="kpi-icon"><%= Icons.Get("key-round", "ico-sm") %></span>
                                </div>
                                <div class="card-b form">
                                    <asp:Literal ID="litPasswordNotice" runat="server" EnableViewState="false" />
                                    <div class="field">
                                        <label for="txtCurrentPassword">Current password</label>
                                        <div class="input-reveal">
                                            <asp:TextBox ID="txtCurrentPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="current-password" />
                                            <button type="button" class="reveal-btn" data-reveal="txtCurrentPassword" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                                        </div>
                                        <asp:Label ID="lblCurrentError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                    </div>
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtNewPassword">New password</label>
                                            <div class="input-reveal">
                                                <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                                <button type="button" class="reveal-btn" data-reveal="txtNewPassword" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                                            </div>
                                            <asp:Label ID="lblNewError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                            <span class="hint">At least 8 characters, and not the same as the current one.</span>
                                        </div>
                                        <div class="field">
                                            <label for="txtConfirmPassword">Confirm new password</label>
                                            <div class="input-reveal">
                                                <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
                                                <button type="button" class="reveal-btn" data-reveal="txtConfirmPassword" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                                            </div>
                                            <asp:Label ID="lblConfirmError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <asp:Button ID="btnChangePassword" runat="server" Text="Change password" CssClass="btn btn-primary" OnClick="btnChangePassword_Click" />
                                </div>
                            </section>
                        </div>

                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Job timing</h2>
                                        <div class="sub">Drives the expected pick-up and the unclaimed flag</div>
                                    </div>
                                    <span class="kpi-icon"><%= Icons.Get("calendar", "ico-sm") %></span>
                                </div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtTurnaroundHours">Default turnaround (hours)</label>
                                            <asp:TextBox ID="txtTurnaroundHours" runat="server" TextMode="Number" />
                                            <asp:Label ID="lblTurnaroundError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                        <div class="field">
                                            <label for="txtUnclaimedDays">Flag unclaimed after (days)</label>
                                            <asp:TextBox ID="txtUnclaimedDays" runat="server" TextMode="Number" />
                                            <asp:Label ID="lblUnclaimedError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">A failed inspection may return the load to</span>
                                        <div class="grid grid-3" style="gap:10px;">
                                            <label class="check"><input type="checkbox" name="rework" value="W"<%= ReworkChecked('W') %> />Washing</label>
                                            <label class="check"><input type="checkbox" name="rework" value="D"<%= ReworkChecked('D') %> />Drying</label>
                                            <label class="check"><input type="checkbox" name="rework" value="F"<%= ReworkChecked('F') %> />Folding</label>
                                        </div>
                                        <asp:Label ID="lblReworkError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">Open decision 5. Default: any of the three. Only the stages on the job's own route are ever offered.</span>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="submit" name="action" value="timing" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save</button>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Email notifications</h2>
                                        <div class="sub">Drop-off and ready-for-pick-up emails</div>
                                    </div>
                                    <% if (EmailReady) { %><span class="pill pill-ok">Configured</span><% } else { %><span class="pill pill-warn">Not configured</span><% } %>
                                </div>
                                <div class="card-b">
                                    <div class="notice notice-info">
                                        <%= Icons.Get("mail", "ico-sm") %>
                                        <span>SMTP settings live in an external config file (Secrets.config) that is kept out of source control, so they are not editable here.<%= EmailReady ? "" : " Until it is set up, every email is logged as Failed and can be resent from the job page later." %></span>
                                    </div>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Password recovery</h2>
                                        <div class="sub">If you forget your password, type this email on the owner login to reset it</div>
                                    </div>
                                    <span class="kpi-icon"><%= Icons.Get("mail", "ico-sm") %></span>
                                </div>
                                <div class="card-b form">
                                    <div class="field">
                                        <label for="txtRecoveryEmail">Recovery email</label>
                                        <asp:TextBox ID="txtRecoveryEmail" runat="server" TextMode="Email" MaxLength="150" autocomplete="email" />
                                        <asp:Label ID="lblRecoveryError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">Use an address only you know. "Forgot password?" on the owner login asks for it to confirm it is you.</span>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="submit" name="action" value="recovery" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save</button>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/ShowPassword.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Settings.js") %>"></script>
</body>
</html>
