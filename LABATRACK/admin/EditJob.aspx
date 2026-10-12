<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="EditJob.aspx.cs" Inherits="LABATRACK.admin.EditJob" %>

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
                        <a class="small" href="../pages/shared/JobDetail.aspx?id=<%= Job.JobId %>"><%= Icons.Get("arrow-left", "ico-sm") %> Back to the job</a>
                        <h1 style="margin-top:8px;">Edit job order <span class="mono muted" style="font-size:16px;"><%: Job.ClaimNumber %></span></h1>
                        <p class="sub">Owner only. Fix a wrong weight, service, add-on, or customer detail.</p>
                    </div>
                    <div class="page-actions">
                        <span class="pill <%= StageCss(Job.CurrentStatus) %>"><%: Job.CurrentStatus %></span>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />
                    <asp:HiddenField ID="hfShownStatus" runat="server" />
                    <asp:HiddenField ID="hfShownDifference" runat="server" />

                    <div class="grid grid-main-side">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h"><h2>Customer</h2></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtCustomerName">Name</label>
                                            <asp:TextBox ID="txtCustomerName" runat="server" MaxLength="100" />
                                            <asp:Label ID="lblNameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                        <div class="field">
                                            <label for="txtContactNumber">Contact number</label>
                                            <asp:TextBox ID="txtContactNumber" runat="server" MaxLength="20" inputmode="tel" />
                                            <asp:Label ID="lblContactError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                    </div>
                                    <div class="field">
                                        <label for="txtEmail">Email <span class="muted">(optional)</span></label>
                                        <asp:TextBox ID="txtEmail" runat="server" MaxLength="150" inputmode="email" />
                                        <asp:Label ID="lblEmailError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">These are the customer's own details, so a fix here shows on all of their jobs.</span>
                                    </div>
                                </div>
                            </section>

                            <section class="card" id="loadCard" data-rates="<%: RatesData %>" data-minimum="<%: Money(Job.MinimumChargeAtCreation) %>"
                                     data-rounding="<%: Job.RoundingIncrementKgAtCreation.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) %>"
                                     data-max-weight="<%: MaxWeightData %>" data-paid="<%: Money(AmountPaid) %>">
                                <div class="card-h"><h2>Load</h2></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="ddlService">Service</label>
                                            <asp:DropDownList ID="ddlService" runat="server" />
                                            <% if (!ServiceChangeAllowed) { %><span class="hint">Locked: the load has left Queued, so the service (and its stages) can no longer change.</span><% } %>
                                        </div>
                                        <div class="field">
                                            <label for="txtWeight">Weight (kg)</label>
                                            <asp:TextBox ID="txtWeight" runat="server" inputmode="decimal" MaxLength="6" autocomplete="off" />
                                            <span class="hint">Was <%: Kg(Job.WeightKg) %> kg when the job was <%: Job.LastEditedAt.HasValue ? "last saved" : "created" %>.</span>
                                            <span class="error-text" id="weightError" hidden>Enter the weight in kilos, for example 5.1.</span>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Service add-ons</span>
                                        <div class="grid grid-2" style="gap:10px; margin-top:8px;">
                                            <asp:Repeater ID="rptServiceAddOns" runat="server">
                                                <ItemTemplate>
                                                    <label class="check"><input type="checkbox" name="addon" value="<%# Eval("AddOnId") %>" data-service="true" data-name="<%#: Eval("Name") %>" data-price="<%# Money(PriceFor((LABATRACK.models.AddOn)Container.DataItem)) %>"<%# IsTicked((int)Eval("AddOnId")) ? " checked=\"checked\"" : "" %> /><%#: Eval("Name") %><span class="price num">₱<%# PriceFor((LABATRACK.models.AddOn)Container.DataItem).ToString("N2") %></span></label>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Product add-ons</span>
                                        <div class="stack" style="gap:10px; margin-top:8px;">
                                            <asp:Repeater ID="rptProducts" runat="server">
                                                <ItemTemplate>
                                                    <div class="product-row is-zero" data-product="true" data-name="<%#: Eval("Name") %>" data-price="<%# Money(PriceFor((LABATRACK.models.AddOn)Container.DataItem)) %>">
                                                        <span class="product-name"><%#: Eval("Name") %></span>
                                                        <span class="price num">₱<%# PriceFor((LABATRACK.models.AddOn)Container.DataItem).ToString("N2") %> each</span>
                                                        <div class="qty">
                                                            <button type="button" data-qty-step="-1" aria-label="One less <%#: Eval("Name") %>"><%# Icons.Get("minus", "ico-sm") %></button>
                                                            <input type="text" inputmode="numeric" name="qty_<%# Eval("AddOnId") %>" value="<%# QuantityOf((int)Eval("AddOnId")) %>" data-qty="true" maxlength="2" aria-label="<%#: Eval("Name") %> quantity" />
                                                            <button type="button" data-qty-step="1" aria-label="One more <%#: Eval("Name") %>"><%# Icons.Get("plus", "ico-sm") %></button>
                                                        </div>
                                                        <span class="line-total num">₱0.00</span>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <label for="txtRemarks">Remarks</label>
                                        <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" />
                                    </div>
                                </div>
                                <div class="card-f">
                                    <a class="btn btn-ghost" href="../pages/shared/JobDetail.aspx?id=<%= Job.JobId %>">Cancel</a>
                                    <button type="submit" name="action" value="save" class="btn btn-primary" id="btnSave"><%= Icons.Get("save", "ico-sm") %><span id="saveLabel"><%: SaveLabel %></span></button>
                                </div>
                            </section>
                        </div>

                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Price summary</h2>
                                        <div class="sub">Recomputed with the prices frozen on this job</div>
                                    </div>
                                </div>
                                <div class="card-b">
                                    <dl class="dl" id="sumLines">
                                        <dt>Rate</dt><dd id="sumRate"><%: Quote != null ? Peso(Quote.RatePerKg) + " / kg" : "—" %></dd>
                                        <dt id="sumWeight"><%: Quote != null ? Kg(Quote.WeightKg) + " kg → billable" : "Weight → billable" %></dt><dd id="sumBillable"><%: Quote != null ? Kg(Quote.BillableKg) + " kg" : "—" %></dd>
                                        <dt id="sumLaundryLabel"><%: Quote != null && Quote.MinimumApplied ? "Raised to minimum" : "Laundry charge" %></dt><dd id="sumLaundry"><%: Quote != null ? Peso(Quote.LaundryCharge) : "—" %></dd>
                                        <% if (Quote != null) { foreach (var line in Quote.Lines) { %>
                                        <dt class="addon-line"><%: line.Label %></dt><dd class="addon-line"><%: Peso(line.Amount) %></dd>
                                        <% } } %>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>New total</span><span class="num" id="sumTotal"><%: Quote != null ? Peso(Quote.Total) : "—" %></span></div>
                                    <dl class="dl" style="margin-top:12px;">
                                        <dt>Already paid</dt><dd><%: Peso(AmountPaid) %></dd>
                                    </dl>
                                </div>
                            </section>

                            <section class="card" id="settleCard" data-state="<%: SettleState %>">
                                <div class="card-h">
                                    <div><h2>After the correction</h2><div class="sub">Saved together with the edit</div></div>
                                    <span class="pill <%: SettlePill %>" id="settlePill"><%: SettlePillText %></span>
                                </div>
                                <div class="card-b form">
                                    <div class="total-line" id="settleLine" style="color:<%: SettleState == "refund" ? "var(--danger)" : SettleState == "balance" ? "var(--warning)" : "var(--text)" %>;">
                                        <span id="settleLabel"><%: SettleState == "refund" ? "Refund to customer" : SettleState == "balance" ? "Balance due" : "Nothing owed" %></span>
                                        <span class="num" id="settleAmount"><%: SettleState == "none" ? "" : Peso(Math.Abs(Difference)) %></span>
                                    </div>
                                    <div class="notice notice-info" id="balanceNotice"<%= SettleState == "balance" ? "" : " hidden" %>>
                                        <%= Icons.Get("info", "ico-sm") %>
                                        <span id="balanceText"><%: BalanceText %></span>
                                    </div>
                                    <div class="notice notice-danger" id="refundNotice"<%= SettleState == "refund" ? "" : " hidden" %>>
                                        <%= Icons.Get("hand-coins", "ico-sm") %>
                                        <span id="refundText"><%: RefundText %></span>
                                    </div>
                                    <div class="field" id="methodField"<%= SettleState == "refund" ? "" : " hidden" %>>
                                        <span class="label">Refund by</span>
                                        <div class="grid grid-2" style="gap:10px;">
                                            <label class="check"><asp:RadioButton ID="rbAdjCash" runat="server" GroupName="adjMethod" /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                            <label class="check"><asp:RadioButton ID="rbAdjEwallet" runat="server" GroupName="adjMethod" /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                        </div>
                                        <span class="hint">Defaults to how the job was first paid.</span>
                                    </div>
                                </div>
                            </section>

                            <div class="notice notice-info">
                                <%= Icons.Get("info", "ico-sm") %>
                                <span>The weight is re-billed at the <%: Peso(Job.RatePerKgAtCreation) %> rate frozen on this job, not today's rate. Only a newly chosen service or add-on takes today's price.</span>
                            </div>

                            <section class="card">
                                <div class="card-h"><h2>Job info</h2></div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt>Dropped off</dt><dd><%: Job.CreatedAt.ToString("MMM d, h:mm tt") %></dd>
                                        <dt>Created by</dt><dd><%: Job.CreatedByName %></dd>
                                        <dt>Expected pick-up</dt><dd><%: Job.ExpectedPickupAt.ToString("MMM d, h:mm tt") %></dd>
                                        <% if (Job.LastEditedAt.HasValue) { %><dt>Last corrected</dt><dd><%: Job.LastEditedAt.Value.ToString("MMM d, h:mm tt") %> by <%: Job.LastEditedByName %></dd><% } %>
                                    </dl>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/NumericInput.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/EditJob.js") %>"></script>
</body>
</html>
