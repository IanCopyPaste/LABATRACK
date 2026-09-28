<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="NewJob.aspx.cs" Inherits="LABATRACK.Pages.Shared.NewJob" %>

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
                        <p class="sub">Find the customer, weigh the load, take any payment, then print the slip.</p>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample values. Phase 3 wires the customer lookup, PricingService,
                         ClaimNumberGenerator, and the single-transaction save. --%>
                    <div class="grid grid-main-side" style="align-items:start;">
                        <div class="stack">
                            <!-- 1. Customer: search by contact number first -->
                            <section class="card">
                                <div class="card-h"><div class="title"><span class="step-no">1</span><div><h2>Customer</h2><div class="sub">Search by contact number. A new customer is created only if none is found.</div></div></div></div>
                                <div class="card-b form">
                                    <div class="search-row" style="align-items:flex-end;">
                                        <div class="field">
                                            <label for="txtSearchContact">Contact number</label>
                                            <asp:TextBox ID="txtSearchContact" runat="server" Text="0917 555 0123" MaxLength="20" />
                                        </div>
                                        <button type="button" class="btn"><%= Icons.Get("search", "ico-sm") %>Find</button>
                                    </div>
                                    <div class="found">
                                        <%= Icons.Get("circle-check") %>
                                        <div><strong>Existing customer found.</strong> <span class="small">Details filled in below. 12 previous job orders.</span></div>
                                    </div>
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtCustomerName">Name</label>
                                            <asp:TextBox ID="txtCustomerName" runat="server" Text="Maria Santos" MaxLength="100" />
                                        </div>
                                        <div class="field">
                                            <label for="txtEmail">Email <span class="muted">(optional, for the pick-up email)</span></label>
                                            <asp:TextBox ID="txtEmail" runat="server" Text="maria.santos@example.com" MaxLength="150" />
                                        </div>
                                    </div>
                                </div>
                            </section>

                            <!-- 2. Load: service, weight, add-ons -->
                            <%-- The price is worked out on screen as the cashier types: rate per service (by the
                                 dropdown's value), minimum charge, rounding step and the maximum weight per job.
                                 data-routes is each service's work stages, shown under the dropdown. DRAFT: these come
                                 from Pricing and Settings in Phase 3, and the server works the total out again on save.
                                 The form opens empty: no weight, no add-ons and no cash, so there is no total until
                                 the load is weighed. --%>
                            <section class="card" id="loadCard" data-rates="1:40,2:35,3:60" data-routes="1:WDF,2:WD,3:WDF" data-minimum="120" data-rounding="1" data-max-weight="<%= SampleMaxWeightKg %>">
                                <div class="card-h"><div class="title"><span class="step-no">2</span><div><h2>Load</h2><div class="sub">Weigh the laundry and choose the service</div></div></div></div>
                                <div class="card-b form">
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="ddlService">Service</label>
                                            <asp:DropDownList ID="ddlService" runat="server">
                                                <asp:ListItem Value="1" Selected="True">Wash-Dry-Fold · ₱40.00/kg</asp:ListItem>
                                                <asp:ListItem Value="2">Wash-Dry · ₱35.00/kg</asp:ListItem>
                                                <asp:ListItem Value="3">Comforter / Bulky · ₱60.00/kg</asp:ListItem>
                                            </asp:DropDownList>
                                            <span class="hint" id="serviceRoute">Stages: Washing → Drying → Folding → Inspection</span>
                                        </div>
                                        <div class="field">
                                            <label for="txtWeight">Weight (kg)</label>
                                            <asp:TextBox ID="txtWeight" runat="server" inputmode="decimal" autocomplete="off" MaxLength="6" />
                                            <span class="hint" id="weightHint">Weigh the load, then type the kilos shown on the scale.</span>
                                            <span class="error-text" id="weightError" hidden>Enter the weight in kilos, for example 5.1.</span>
                                        </div>
                                    </div>
                                    <%-- Two kinds of add-on. A service is work done to the load, so it is
                                         charged once and is a plain checkbox. A product is a thing sold, so it
                                         carries a quantity and bills as quantity x unit price. --%>
                                    <div class="field">
                                        <span class="label">Service add-ons</span>
                                        <span class="hint">Extra work on the load. Charged once, whatever the weight.</span>
                                        <div class="grid grid-2" style="gap:10px; margin-top:8px;">
                                            <label class="check"><input type="checkbox" data-service data-name="Extra rinse" data-price="20.00" />Extra rinse<span class="price num">₱20.00</span></label>
                                            <label class="check"><input type="checkbox" data-service data-name="Stain treatment" data-price="30.00" />Stain treatment<span class="price num">₱30.00</span></label>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Product add-ons</span>
                                        <span class="hint">Items sold with the load. Set how many.</span>
                                        <div class="stack" style="gap:10px; margin-top:8px;">
                                            <div class="product-row is-zero" data-product data-name="Fabric conditioner" data-price="15.00">
                                                <span class="product-name">Fabric conditioner</span>
                                                <span class="price num">₱15.00 each</span>
                                                <div class="qty">
                                                    <button type="button" data-qty-step="-1" aria-label="One less fabric conditioner" disabled="disabled"><%= Icons.Get("minus", "ico-sm") %></button>
                                                    <input type="text" inputmode="numeric" value="0" data-qty aria-label="Fabric conditioner quantity" />
                                                    <button type="button" data-qty-step="1" aria-label="One more fabric conditioner"><%= Icons.Get("plus", "ico-sm") %></button>
                                                </div>
                                                <span class="line-total num">₱0.00</span>
                                            </div>
                                            <div class="product-row is-zero" data-product data-name="Detergent sachet" data-price="12.00">
                                                <span class="product-name">Detergent sachet</span>
                                                <span class="price num">₱12.00 each</span>
                                                <div class="qty">
                                                    <button type="button" data-qty-step="-1" aria-label="One less detergent sachet" disabled="disabled"><%= Icons.Get("minus", "ico-sm") %></button>
                                                    <input type="text" inputmode="numeric" value="0" data-qty aria-label="Detergent sachet quantity" />
                                                    <button type="button" data-qty-step="1" aria-label="One more detergent sachet"><%= Icons.Get("plus", "ico-sm") %></button>
                                                </div>
                                                <span class="line-total num">₱0.00</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtRemarks">Remarks (intake check)</label>
                                            <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" Text="1 blouse with a loose button. No stains noted." />
                                        </div>
                                        <div class="field">
                                            <label for="txtExpectedPickup">Expected pick-up</label>
                                            <asp:TextBox ID="txtExpectedPickup" runat="server" TextMode="DateTimeLocal" Text="2026-09-22T09:12" />
                                            <span class="hint">Defaults to now + the turnaround hours in Settings. Can be changed if the load will be ready sooner.</span>
                                        </div>
                                    </div>
                                </div>
                            </section>

                            <!-- 3. Payment: full amount only, collected now -->
                            <section class="card" id="paymentCard">
                                <div class="card-h"><div class="title"><span class="step-no">3</span><div><h2>Payment</h2><div class="sub">Full payment is collected now. No partial payments or balances.</div></div></div></div>
                                <div class="card-b form">
                                    <div class="field">
                                        <span class="label">Method</span>
                                        <div class="method">
                                            <label class="check"><asp:RadioButton ID="rbCash" runat="server" GroupName="method" Checked="true" /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                            <label class="check"><asp:RadioButton ID="rbEwallet" runat="server" GroupName="method" /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                        </div>
                                    </div>
                                    <div id="cashFields" class="form-row">
                                        <div class="field">
                                            <label for="txtCashReceived">Cash received</label>
                                            <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtCashReceived" runat="server" autocomplete="off" /></div>
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
                                </div>
                            </section>
                        </div>

                        <!-- Live summary: the numbers PricingService will compute -->
                        <aside class="summary stack">
                            <section class="card">
                                <div class="card-h">
                                    <div><h2>Order summary</h2><div class="sub">Claim number is assigned on save</div></div>
                                    <%= Icons.Get("receipt") %>
                                </div>
                                <div class="card-b">
                                    <%-- The dt/dd pairs marked addon-line are rebuilt by NewJob.js whenever a
                                         service is ticked or a product quantity changes. The server renders the
                                         same rows first, so the summary is right before the script runs. --%>
                                    <dl class="dl" id="sumLines">
                                        <dt id="sumService">Wash-Dry-Fold</dt><dd id="sumRate">₱40.00 / kg</dd>
                                        <dt id="sumWeight">Weight &rarr; billable</dt><dd id="sumBillable">—</dd>
                                        <dt>Laundry charge</dt><dd id="sumLaundry">—</dd>
                                        <dt class="small" id="sumMinLabel">Minimum ₱120.00</dt><dd class="small muted" id="sumMin">—</dd>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>Total</span><span class="num" id="sumTotal">—</span></div>
                                    <dl class="dl" id="cashSummary" style="margin-top:12px;">
                                        <dt>Cash received</dt><dd id="sumReceived">—</dd>
                                        <dt class="strong" style="color:var(--text);">Change</dt><dd class="strong" id="sumChange" style="color:var(--success);">—</dd>
                                    </dl>
                                    <div class="status-row" style="margin-top:14px;">
                                        <span class="muted small">Payment</span>
                                        <span class="pill pill-ok" id="sumMethod">Paid in full &middot; Cash</span>
                                    </div>
                                    <asp:Label ID="lblPaymentError" runat="server" CssClass="error-text" Visible="false" />
                                    <%-- DRAFT ONLY: the total the page showed, so the draft save can check the cash against it.
                                         Phase 3 ignores this and works the total out again with PricingService. --%>
                                    <asp:HiddenField ID="hfShownTotal" runat="server" />
                                </div>
                                <div class="card-f" style="flex-direction:column;">
                                    <%-- DRAFT: passes the payment to the slip in the query string so the design can be
                                         tried. Phase 3 saves the job, add-ons, first history row and payment in one
                                         SqlTransaction, then opens the slip by JobId. --%>
                                    <asp:Button ID="btnCreate" runat="server" Text="Create job and print slip" CssClass="btn btn-primary btn-block" OnClick="btnCreate_Click" style="height:42px;" />
                                    <a class="btn btn-ghost btn-block" href="../../Admin/Dashboard.aspx">Cancel</a>
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

    <script src="<%= AssetUrl.Get("~/Assets/js/NewJob.js") %>"></script>
</body>
</html>
