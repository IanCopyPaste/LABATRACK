<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="NewJob.aspx.cs" Inherits="LABATRACK.Pages.Shared.NewJob" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>New job order · LabaTrack</title>
    <style>
        .step-no {
            width: 26px; height: 26px; border-radius: 50%; flex-shrink: 0;
            display: grid; place-items: center; font-size: 12.5px; font-weight: 650;
            background: var(--primary); color: #fff;
        }
        .card-h .title { display: flex; align-items: center; gap: 12px; }
        .search-row { display: flex; gap: 8px; }
        .search-row .field { flex: 1; }
        .found { display: flex; align-items: center; gap: 12px; padding: 12px 14px; border-radius: var(--radius-sm); background: var(--success-soft); color: #17613f; }
        .summary { position: sticky; top: 20px; }
        .method { display: flex; gap: 10px; }
        .method label { flex: 1; }
        .status-row { display: flex; justify-content: space-between; align-items: center; }
        .quick-cash { display: flex; gap: 6px; flex-wrap: wrap; }
        .change-box {
            height: 38px; display: flex; align-items: center; justify-content: space-between; padding: 0 12px;
            border-radius: var(--radius-sm); background: var(--success-soft); color: var(--success);
        }
        .change-box.short { background: var(--danger-soft); color: var(--danger); }
        .change-amount { font-size: 18px; font-weight: 700; }
    </style>
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
                            <section class="card">
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
                                        </div>
                                        <div class="field">
                                            <label for="txtWeight">Weight (kg)</label>
                                            <asp:TextBox ID="txtWeight" runat="server" Text="5.1" />
                                            <span class="hint">Rounded up to the next 1 kg: bills as 6 kg.</span>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Add-ons</span>
                                        <div class="grid grid-2" style="gap:10px;">
                                            <label class="check"><input type="checkbox" checked="checked" />Fabric conditioner<span class="price num">₱15.00</span></label>
                                            <label class="check"><input type="checkbox" />Extra rinse<span class="price num">₱20.00</span></label>
                                            <label class="check"><input type="checkbox" />Stain treatment<span class="price num">₱30.00</span></label>
                                            <label class="check"><input type="checkbox" checked="checked" />Rush (same day)<span class="price num">₱50.00</span></label>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="field">
                                            <label for="txtRemarks">Remarks (intake check)</label>
                                            <asp:TextBox ID="txtRemarks" runat="server" TextMode="MultiLine" Text="1 blouse with a loose button. No stains noted." />
                                        </div>
                                        <div class="field">
                                            <label for="txtExpectedPickup">Expected pick-up</label>
                                            <asp:TextBox ID="txtExpectedPickup" runat="server" TextMode="DateTimeLocal" Text="2026-09-22T17:00" />
                                            <span class="hint">Rush: same day. Without rush, now + 24 hours (default turnaround). Can be changed.</span>
                                        </div>
                                    </div>
                                </div>
                            </section>

                            <!-- 3. Payment: full amount only, collected now -->
                            <section class="card">
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
                                            <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtCashReceived" runat="server" Text="500.00" autocomplete="off" /></div>
                                            <div class="quick-cash">
                                                <button type="button" class="btn btn-sm" data-cash="305">Exact</button>
                                                <button type="button" class="btn btn-sm" data-cash="400">₱400</button>
                                                <button type="button" class="btn btn-sm" data-cash="500">₱500</button>
                                                <button type="button" class="btn btn-sm" data-cash="1000">₱1,000</button>
                                            </div>
                                        </div>
                                        <div class="field">
                                            <span class="label">Change to give</span>
                                            <div id="changeBox" class="change-box">
                                                <span class="muted small">Change</span>
                                                <span id="changeAmount" class="change-amount num">₱195.00</span>
                                            </div>
                                        </div>
                                    </div>
                                    <div id="ewalletNote" class="notice notice-info" style="display:none;">
                                        <%= Icons.Get("smartphone", "ico-sm") %>
                                        <span>Confirm the customer sent exactly <strong>₱305.00</strong> before saving. E-wallet is recorded only; it is not verified by the system.</span>
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
                                    <dl class="dl">
                                        <dt>Wash-Dry-Fold</dt><dd>₱40.00 / kg</dd>
                                        <dt>5.1 kg &rarr; billable</dt><dd>6 kg</dd>
                                        <dt>Laundry charge</dt><dd>₱240.00</dd>
                                        <dt class="small">Minimum ₱120.00</dt><dd class="small muted">not needed</dd>
                                        <dt>Fabric conditioner</dt><dd>₱15.00</dd>
                                        <dt>Rush (same day)</dt><dd>₱50.00</dd>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>Total</span><span class="num">₱305.00</span></div>
                                    <dl class="dl" id="cashSummary" style="margin-top:12px;">
                                        <dt>Cash received</dt><dd id="sumReceived">₱500.00</dd>
                                        <dt class="strong" style="color:var(--text);">Change</dt><dd class="strong" id="sumChange" style="color:var(--success);">₱195.00</dd>
                                    </dl>
                                    <div class="status-row" style="margin-top:14px;">
                                        <span class="muted small">Payment</span>
                                        <span class="pill pill-ok" id="sumMethod">Paid in full &middot; Cash</span>
                                    </div>
                                    <asp:Label ID="lblPaymentError" runat="server" CssClass="error-text" Visible="false" />
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

    <script>
        // Shows the change while the cashier types. Display only: the server checks the
        // cash and computes the change again on save, and that is the value that is stored.
        (function () {
            var total = 305.00;   // DRAFT: Phase 3 writes the computed total here
            var cash = document.getElementById('txtCashReceived');
            var rbCash = document.getElementById('rbCash');
            var rbEwallet = document.getElementById('rbEwallet');
            var createBtn = document.getElementById('btnCreate');

            function peso(n) {
                return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            }

            function update() {
                var isCash = rbCash.checked;
                document.getElementById('cashFields').style.display = isCash ? '' : 'none';
                document.getElementById('cashSummary').style.display = isCash ? '' : 'none';
                document.getElementById('ewalletNote').style.display = isCash ? 'none' : '';

                var received = parseFloat(cash.value.replace(/,/g, '')) || 0;
                var change = received - total;
                var enough = !isCash || change >= 0;

                document.getElementById('changeBox').className = 'change-box' + (enough ? '' : ' short');
                document.getElementById('changeAmount').textContent = enough ? peso(change) : 'Short ' + peso(-change);
                document.getElementById('sumReceived').textContent = peso(received);
                document.getElementById('sumChange').textContent = enough ? peso(change) : '—';
                document.getElementById('sumMethod').textContent = 'Paid in full · ' + (isCash ? 'Cash' : 'E-wallet');
                createBtn.disabled = !enough;
            }

            cash.addEventListener('input', update);
            rbCash.addEventListener('change', update);
            rbEwallet.addEventListener('change', update);
            document.querySelectorAll('[data-cash]').forEach(function (b) {
                b.addEventListener('click', function () {
                    cash.value = parseFloat(b.getAttribute('data-cash')).toFixed(2);
                    update();
                });
            });
            update();
        })();
    </script>
</body>
</html>
