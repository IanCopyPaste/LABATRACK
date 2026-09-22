<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="LABATRACK.Admin.Settings" %>

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
                        <p class="sub">Shop details and the rules the counter screens follow. Pricing rules are on the Pricing page.</p>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample values. Phase 1 stores these in the Settings table. --%>
                    <div class="grid grid-2" style="align-items:start;">
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
                                    <asp:TextBox ID="txtShopName" runat="server" Text="LabaTrack Laundry Shop" MaxLength="100" />
                                </div>
                                <div class="field">
                                    <label for="txtSlipHeader">Slip header</label>
                                    <asp:TextBox ID="txtSlipHeader" runat="server" TextMode="MultiLine" Text="Salamat po! Please present this slip when claiming your laundry." />
                                </div>
                                <div class="field">
                                    <label for="txtClaimFormat">Claim number format</label>
                                    <asp:TextBox ID="txtClaimFormat" runat="server" Text="LT-yyMMdd-###" ReadOnly="true" CssClass="input mono" />
                                    <span class="hint">Example: LT-260922-003. Fixed in code by ClaimNumberGenerator (open decision 2).</span>
                                </div>
                            </div>
                            <div class="card-f">
                                <button type="button" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save</button>
                            </div>
                        </section>

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
                                            <asp:TextBox ID="txtTurnaroundHours" runat="server" TextMode="Number" Text="24" />
                                        </div>
                                        <div class="field">
                                            <label for="txtUnclaimedDays">Flag unclaimed after (days)</label>
                                            <asp:TextBox ID="txtUnclaimedDays" runat="server" TextMode="Number" Text="7" />
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">A failed inspection may return the load to</span>
                                        <div class="grid grid-3" style="gap:10px;">
                                            <label class="check"><input type="checkbox" checked="checked" />Washing</label>
                                            <label class="check"><input type="checkbox" checked="checked" />Drying</label>
                                            <label class="check"><input type="checkbox" checked="checked" />Folding</label>
                                        </div>
                                        <span class="hint">Open decision 5. Default: any of the three.</span>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save</button>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Email notifications</h2>
                                        <div class="sub">Drop-off and ready-for-pick-up emails</div>
                                    </div>
                                    <span class="pill pill-warn">Not configured</span>
                                </div>
                                <div class="card-b">
                                    <div class="notice notice-info">
                                        <%= Icons.Get("mail", "ico-sm") %>
                                        <span>SMTP settings live in an external config file that is kept out of source control, so they are not editable here.</span>
                                    </div>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
