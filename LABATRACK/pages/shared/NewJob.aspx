<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="NewJob.aspx.cs" Inherits="LABATRACK.pages.shared.NewJob" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>New job order · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>New job order</h1>
                        <p class="sub">Find the customer, weigh the load, take the payment, then print the slip.</p>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <div class="grid grid-main-side" style="align-items:start;">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h"><div class="title"><span class="step-no">1</span><div><h2>Customer</h2><div class="sub">Search by contact number. A new customer is created only if none is found.</div></div></div></div>
                                <div class="card-b form">
                                    <asp:HiddenField ID="hfCustomerId" runat="server" />
                                    <div class="search-row" style="align-items:flex-end;">
                                        <div class="field">
                                            <label for="txtSearchContact">Contact number</label>
                                            <div class="suggest-wrap">
                                                <asp:TextBox ID="txtSearchContact" runat="server" MaxLength="20" inputmode="tel" autocomplete="off" placeholder="0917 555 0123" data-find-box="true" data-suggest="numberList" />
                                                <ul id="numberList" class="suggest" role="listbox" aria-label="Saved contact numbers" hidden>
                                                    <asp:Repeater ID="rptSuggest" runat="server">
                                                        <ItemTemplate>
                                                            <li role="option" data-number="<%#: Eval("ContactNumber") %>" data-digits="<%#: DigitsOf((string)Eval("ContactNumber")) %>" data-name="<%#: Eval("Name") %>">
                                                                <span class="suggest-number"><%#: Eval("ContactNumber") %></span>
                                                                <span class="suggest-name"><%#: Eval("Name") %> &middot; <%# SuggestNote((LABATRACK.models.Customer)Container.DataItem) %></span>
                                                            </li>
                                                        </ItemTemplate>
                                                    </asp:Repeater>
                                                    <li class="suggest-empty" hidden>No saved number matches. Press Find to add a new customer.</li>
                                                </ul>
                                            </div>
                                        </div>
                                        <button type="submit" name="action" value="find" class="btn"><%= Icons.Get("search", "ico-sm") %>Find</button>
                                    </div>
                                    <asp:Label ID="lblContactError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                    <asp:Panel ID="pnlFound" runat="server" CssClass="found" Visible="false">
                                        <%= Icons.Get("circle-check") %>
                                        <div><strong>Existing customer found.</strong> <span class="small"><%: FoundNote %></span></div>
                                    </asp:Panel>
                                    <asp:Panel ID="pnlNewCustomer" runat="server" CssClass="notice notice-info" Visible="false">
                                        <%= Icons.Get("user-plus", "ico-sm") %>
                                        <span>No customer has this number yet. Enter their name (and email, if they have one) to add them with this job.</span>
                                    </asp:Panel>
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtCustomerName">Name</label>
                                            <asp:TextBox ID="txtCustomerName" runat="server" MaxLength="100" autocomplete="off" />
                                            <asp:Label ID="lblNameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                        <div class="field">
                                            <label for="txtEmail">Email <span class="muted">(optional, for the pick-up email)</span></label>
                                            <asp:TextBox ID="txtEmail" runat="server" MaxLength="150" inputmode="email" autocomplete="off" />
                                            <asp:Label ID="lblEmailError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        </div>
                                    </div>
                                    <% if (CustomerFound) { %><span class="hint">A saved customer's details are changed by the owner on the Customers page.</span><% } %>
                                </div>
                            </section>

                            <section class="card" id="loadCard" data-rates="<%: RatesData %>" data-routes="<%: RoutesData %>" data-minimum="<%: MinimumData %>" data-rounding="<%: RoundingData %>" data-max-weight="<%: MaxWeightData %>">
                                <div class="card-h"><div class="title"><span class="step-no">2</span><div><h2>Load</h2><div class="sub">Weigh the laundry and choose the service</div></div></div></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="ddlService">Service</label>
                                            <asp:DropDownList ID="ddlService" runat="server" />
                                            <span class="hint" id="serviceRoute"></span>
                                        </div>
                                        <div class="field">
                                            <label for="txtWeight">Weight (kg)</label>
                                            <asp:TextBox ID="txtWeight" runat="server" inputmode="decimal" autocomplete="off" MaxLength="6" />
                                            <span class="hint" id="weightHint">Weigh the load, then type the kilos shown on the scale.</span>
                                            <span class="error-text" id="weightError" hidden>Enter the weight in kilos, for example 5.1.</span>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Service add-ons</span>
                                        <span class="hint">Extra work on the load. Charged once, whatever the weight.</span>
                                        <div class="grid grid-2" style="gap:10px; margin-top:8px;">
                                            <asp:Repeater ID="rptServiceAddOns" runat="server">
                                                <ItemTemplate>
                                                    <label class="check"><input type="checkbox" name="addon" value="<%# Eval("AddOnId") %>" data-service="true" data-name="<%#: Eval("Name") %>" data-price="<%# PriceData((decimal)Eval("Price")) %>"<%# IsTicked((int)Eval("AddOnId")) ? " checked=\"checked\"" : "" %> /><%#: Eval("Name") %><span class="price num">₱<%# ((decimal)Eval("Price")).ToString("N2") %></span></label>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Product add-ons</span>
                                        <span class="hint">Items sold with the load. Set how many.</span>
                                        <div class="stack" style="gap:10px; margin-top:8px;">
                                            <asp:Repeater ID="rptProducts" runat="server">
                                                <ItemTemplate>
                                                    <div class="product-row is-zero" data-product="true" data-name="<%#: Eval("Name") %>" data-price="<%# PriceData((decimal)Eval("Price")) %>">
                                                        <span class="product-name"><%#: Eval("Name") %></span>
                                                        <span class="price num">₱<%# ((decimal)Eval("Price")).ToString("N2") %> each</span>
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
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtRemarks">Remarks (intake check)</label>
                                            <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" placeholder="For example: 1 blouse with a loose button. No stains noted." />
                                        </div>
                                        <div class="field">
                                            <label for="txtExpectedPickup">Expected pick-up</label>
                                            <asp:TextBox ID="txtExpectedPickup" runat="server" TextMode="DateTimeLocal" />
                                            <span class="hint">Defaults to now + <%: TurnaroundHours %> hours (Settings). Can be changed if the load will be ready sooner.</span>
                                        </div>
                                    </div>
                                </div>
                            </section>

                            <section class="card" id="paymentCard" data-min-percent="<%: MinDownPercent %>">
                                <div class="card-h"><div class="title"><span class="step-no">3</span><div><h2>Payment</h2><div class="sub"><%: PaymentSub %></div></div></div></div>
                                <div class="card-b form">
                                    <div class="field">
                                        <span class="label">Paid now</span>
                                        <div class="method">
                                            <label class="check"><asp:RadioButton ID="rbPayFull" runat="server" GroupName="plan" Checked="true" />Pay in full</label>
                                            <% if (MinDownPercent < 100) { %><label class="check"><asp:RadioButton ID="rbPayPart" runat="server" GroupName="plan" />Down payment</label><% } %>
                                            <% if (MinDownPercent == 0) { %><label class="check"><asp:RadioButton ID="rbPayLater" runat="server" GroupName="plan" />Pay at pick-up</label><% } %>
                                        </div>
                                    </div>
                                    <div class="field" id="downFields" hidden>
                                        <label for="txtDownPayment">Down payment</label>
                                        <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtDownPayment" runat="server" inputmode="decimal" autocomplete="off" /></div>
                                        <span class="hint" id="downHint">At least <%: MinDownPercent %>% of the total. The rest is paid before the load is claimed.</span>
                                    </div>
                                    <div id="laterNote" class="notice notice-info" hidden>
                                        <%= Icons.Get("info", "ico-sm") %>
                                        <span>Nothing is paid now. The whole total is collected before the load is handed over.</span>
                                    </div>
                                    <div class="field" id="methodField">
                                        <span class="label">Method</span>
                                        <div class="method">
                                            <label class="check"><asp:RadioButton ID="rbCash" runat="server" GroupName="method" Checked="true" /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                            <label class="check"><asp:RadioButton ID="rbEwallet" runat="server" GroupName="method" /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                        </div>
                                    </div>
                                    <div id="cashFields" class="form-row">
                                        <div class="field">
                                            <label for="txtCashReceived">Cash received</label>
                                            <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtCashReceived" runat="server" inputmode="decimal" autocomplete="off" /></div>
                                            <div class="quick-cash">
                                                <button type="button" class="btn btn-sm" id="btnExact" data-cash="" disabled="disabled">Exact</button>
                                                <button type="button" class="btn btn-sm" data-cash="400">₱400</button>
                                                <button type="button" class="btn btn-sm" data-cash="500">₱500</button>
                                                <button type="button" class="btn btn-sm" data-cash="1000">₱1,000</button>
                                            </div>
                                        </div>
                                        <div class="field">
                                            <span class="label">Change to give</span>
                                            <div id="changeBox" class="change-box idle">
                                                <span class="small" id="changeLabel">Change</span>
                                                <span id="changeAmount" class="change-amount num">Enter the weight first</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div id="ewalletNote" class="notice notice-info" style="display:none;">
                                        <%= Icons.Get("smartphone", "ico-sm") %>
                                        <span>Confirm the customer sent exactly <strong id="ewalletTotal">—</strong> before saving. E-wallet is recorded only; it is not verified by the system.</span>
                                    </div>
                                    <span class="error-text" id="downError" hidden></span>
                                </div>
                            </section>
                        </div>

                        <aside class="summary stack">
                            <section class="card">
                                <div class="card-h">
                                    <div><h2>Order summary</h2><div class="sub">Claim number is assigned on save</div></div>
                                    <%= Icons.Get("receipt") %>
                                </div>
                                <div class="card-b">
                                    <dl class="dl" id="sumLines">
                                        <dt id="sumService">—</dt><dd id="sumRate">—</dd>
                                        <dt id="sumWeight">Weight &rarr; billable</dt><dd id="sumBillable">—</dd>
                                        <dt>Laundry charge</dt><dd id="sumLaundry">—</dd>
                                        <dt class="small" id="sumMinLabel">Minimum</dt><dd class="small muted" id="sumMin">—</dd>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>Total</span><span class="num" id="sumTotal">—</span></div>
                                    <dl class="dl" id="balanceSummary" style="margin-top:12px;" hidden>
                                        <dt>Paid now</dt><dd id="sumPaidNow">—</dd>
                                        <dt class="strong" style="color:var(--text);">Balance due</dt><dd class="strong" id="sumBalance" style="color:var(--warning);">—</dd>
                                    </dl>
                                    <dl class="dl" id="cashSummary" style="margin-top:12px;">
                                        <dt>Cash received</dt><dd id="sumReceived">—</dd>
                                        <dt class="strong" style="color:var(--text);">Change</dt><dd class="strong" id="sumChange" style="color:var(--success);">—</dd>
                                    </dl>
                                    <div class="status-row" style="margin-top:14px;">
                                        <span class="muted small">Payment</span>
                                        <span class="pill pill-ok" id="sumMethod">Paid in full &middot; Cash</span>
                                    </div>
                                    <asp:Label ID="lblPaymentError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" style="display:block; margin-top:12px;" />
                                </div>
                                <div class="card-f" style="flex-direction:column;">
                                    <asp:Button ID="btnCreate" runat="server" Text="Create job and print slip" CssClass="btn btn-primary btn-block" OnClick="btnCreate_Click" style="height:42px;"
                                        data-confirm-title="Create this job order?" data-confirm-ok="Create and print slip" />
                                    <a class="btn btn-ghost btn-block" href="<%: CancelUrl %>">Cancel</a>
                                </div>
                            </section>
                            <div class="notice notice-info">
                                <%= Icons.Get("info", "ico-sm") %>
                                <span>The rate and add-on prices are copied into the job when it is saved, so later price changes never alter this total.</span>
                            </div>
                        </aside>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/NumericInput.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Confirm.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/CustomerSuggest.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/NewJob.js") %>"></script>
</body>
</html>
