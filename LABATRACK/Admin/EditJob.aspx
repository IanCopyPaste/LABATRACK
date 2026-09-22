<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EditJob.aspx.cs" Inherits="LABATRACK.Admin.EditJob" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Edit job order · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <a class="small" href="Dashboard.aspx"><%= Icons.Get("arrow-left", "ico-sm") %> Back to dashboard</a>
                        <h1 style="margin-top:8px;">Edit job order <span class="mono muted" style="font-size:16px;">LT-260922-003</span></h1>
                        <p class="sub">Owner only. Fix a wrong weight, service, add-on, or customer detail.</p>
                    </div>
                    <div class="page-actions">
                        <span class="pill st-washing">Washing</span>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample job. Phase 3/4 loads it by JobId and saves through the services. --%>
                    <div class="grid grid-main-side">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h"><h2>Customer</h2></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtCustomerName">Name</label>
                                            <asp:TextBox ID="txtCustomerName" runat="server" Text="Liza Peñaflor" MaxLength="100" />
                                        </div>
                                        <div class="field">
                                            <label for="txtContactNumber">Contact number</label>
                                            <asp:TextBox ID="txtContactNumber" runat="server" Text="0917 555 0142" MaxLength="20" />
                                        </div>
                                    </div>
                                    <div class="field">
                                        <label for="txtEmail">Email <span class="muted">(optional)</span></label>
                                        <asp:TextBox ID="txtEmail" runat="server" Text="liza.penaflor@example.com" MaxLength="150" />
                                    </div>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h"><h2>Load</h2></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="ddlService">Service</label>
                                            <asp:DropDownList ID="ddlService" runat="server">
                                                <asp:ListItem Value="1">Wash-Dry-Fold</asp:ListItem>
                                                <asp:ListItem Value="2" Selected="True">Wash-Dry</asp:ListItem>
                                                <asp:ListItem Value="3">Comforter / Bulky</asp:ListItem>
                                            </asp:DropDownList>
                                        </div>
                                        <div class="field">
                                            <label for="txtWeight">Weight (kg)</label>
                                            <asp:TextBox ID="txtWeight" runat="server" />
                                            <span class="hint">Was 4.6 kg when the job was created.</span>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Add-ons</span>
                                        <div class="grid grid-2" style="gap:10px;">
                                            <label class="check"><input type="checkbox" />Fabric conditioner<span class="price num">₱15.00</span></label>
                                            <label class="check"><input type="checkbox" checked="checked" />Extra rinse<span class="price num">₱20.00</span></label>
                                            <label class="check"><input type="checkbox" />Stain treatment<span class="price num">₱30.00</span></label>
                                            <label class="check"><input type="checkbox" />Rush (same day)<span class="price num">₱50.00</span></label>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <label for="txtRemarks">Remarks</label>
                                        <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" Text="1 white shirt with a small stain on the collar." />
                                    </div>
                                </div>
                                <div class="card-f">
                                    <a class="btn btn-ghost" href="Dashboard.aspx">Cancel</a>
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %><%= IsRefund ? "Save and refund ₱55.00" : "Save and collect ₱35.00" %></button>
                                </div>
                            </section>
                        </div>

                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Price summary</h2>
                                        <div class="sub">Recomputed with the rate frozen at creation</div>
                                    </div>
                                </div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt>Rate at creation</dt><dd>₱35.00 / kg</dd>
                                        <% if (IsRefund) { %>
                                        <dt>2.9 kg &rarr; billable</dt><dd>3 kg</dd>
                                        <dt>3 kg &times; ₱35.00</dt><dd class="muted">₱105.00</dd>
                                        <dt>Raised to minimum</dt><dd>₱120.00</dd>
                                        <% } else { %>
                                        <dt>5.3 kg &rarr; billable</dt><dd>6 kg</dd>
                                        <dt>Laundry charge</dt><dd>₱210.00</dd>
                                        <% } %>
                                        <dt>Extra rinse</dt><dd>₱20.00</dd>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>New total</span><span class="num"><%= IsRefund ? "₱140.00" : "₱230.00" %></span></div>
                                    <dl class="dl" style="margin-top:12px;">
                                        <dt>Already paid (e-wallet)</dt><dd>₱195.00</dd>
                                    </dl>
                                </div>
                            </section>

                            <%-- The difference is always settled on save, so payments = total and there is never a balance.
                                 Higher total: collect the difference. Lower (customer overpaid): refund it. Same: nothing to do. --%>
                            <section class="card">
                                <div class="card-h">
                                    <div><h2>Settle the difference</h2><div class="sub">Saved together with the edit</div></div>
                                    <span class="pill <%= IsRefund ? "pill-danger" : "pill-warn" %>"><%= IsRefund ? "Overpaid" : "Collect" %></span>
                                </div>
                                <div class="card-b form">
                                    <% if (IsRefund) { %>
                                    <div class="total-line" style="color:var(--danger);"><span>Refund to customer</span><span class="num">₱55.00</span></div>
                                    <div class="notice notice-danger">
                                        <%= Icons.Get("hand-coins", "ico-sm") %>
                                        <span>The customer paid ₱195.00 but the corrected total is ₱140.00. Give back <strong>₱55.00</strong> before saving. It is recorded as a −₱55.00 refund row.</span>
                                    </div>
                                    <% } else { %>
                                    <div class="total-line" style="color:var(--warning);"><span>Collect from customer</span><span class="num">₱35.00</span></div>
                                    <% } %>
                                    <div class="field">
                                        <span class="label"><%= IsRefund ? "Refund by" : "Method" %></span>
                                        <div class="grid grid-2" style="gap:10px;">
                                            <label class="check"><input type="radio" name="adjMethod" id="rbAdjCash" /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                            <label class="check"><input type="radio" name="adjMethod" id="rbAdjEwallet" checked="checked" /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                        </div>
                                        <span class="hint">Defaults to how the job was first paid.</span>
                                    </div>
                                    <% if (!IsRefund) { %>
                                    <%-- Cash only: same cash received and change as a new job --%>
                                    <div id="adjCashFields" class="form-row" style="display:none;" data-due="35.00">
                                        <div class="field">
                                            <label for="txtAdjCashReceived">Cash received</label>
                                            <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtAdjCashReceived" runat="server" Text="50.00" autocomplete="off" /></div>
                                        </div>
                                        <div class="field">
                                            <span class="label">Change to give</span>
                                            <div id="adjChangeBox" class="change-box"><span class="muted small">Change</span><span id="adjChange" class="change-amount num">₱15.00</span></div>
                                        </div>
                                    </div>
                                    <% } %>
                                </div>
                            </section>

                            <div class="notice notice-warn preview-links">
                                <%= Icons.Get("eye", "ico-sm") %>
                                <span><strong>Design draft.</strong> Preview: <a href="EditJob.aspx">total goes up (collect)</a> <a href="EditJob.aspx?case=refund">customer overpaid (refund)</a></span>
                            </div>

                            <div class="notice notice-info">
                                <%= Icons.Get("info", "ico-sm") %>
                                <span>The weight is re-billed at the ₱35.00 rate frozen on this job, not today's rate.</span>
                            </div>

                            <section class="card">
                                <div class="card-h"><h2>Job info</h2></div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt>Dropped off</dt><dd>Sep 22, 9:02 AM</dd>
                                        <dt>Created by</dt><dd>jcruz</dd>
                                        <dt>Expected pick-up</dt><dd>Sep 23, 9:00 AM</dd>
                                    </dl>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="../Assets/js/EditJob.js"></script>
</body>
</html>
