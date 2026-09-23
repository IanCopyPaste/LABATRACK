<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DailySalesReport.aspx.cs" Inherits="LABATRACK.Admin.DailySalesReport" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Daily sales report · LabaTrack</title>
</head>
<body class="print-page">
    <form id="form1" runat="server" defaultbutton="btnShow">
        <%-- Screen only: never printed. --%>
        <div class="toolbar">
            <a class="btn btn-ghost" href="Reports.aspx?view=day&amp;d=<%: Day.ToString("yyyy-MM-dd") %>"><%= Icons.Get("arrow-left", "ico-sm") %>Back to reports</a>
            <div class="toolbar-tools">
                <% if (PrevDayLink != null) { %><a class="btn btn-ghost" href="<%: PrevDayLink %>" aria-label="Previous day"><%= Icons.Get("arrow-left", "ico-sm") %></a><% } %>
                <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="input" aria-label="Report date" />
                <asp:Button ID="btnShow" runat="server" Text="Show" CssClass="btn" OnClick="btnShow_Click" />
                <% if (NextDayLink != null) { %><a class="btn btn-ghost" href="<%: NextDayLink %>" aria-label="Next day"><%= Icons.Get("arrow-right", "ico-sm") %></a><% } %>
                <button type="button" class="btn btn-primary" data-print="true"><%= Icons.Get("printer", "ico-sm") %>Print (A4)</button>
            </div>
        </div>

        <%-- DRAFT: figures from Helpers/SampleSales.cs. Phase 5 reads Payments for the chosen day. --%>
        <article class="paper">
            <header class="letterhead">
                <div class="shop">
                    <span class="shop-mark"><%= Icons.Get("washing-machine") %></span>
                    <div>
                        <div class="shop-name"><%: ShopTitle %></div>
                        <div class="shop-sub">Job order and sales record</div>
                    </div>
                </div>
                <div class="doc-id">
                    <div class="doc-title">Daily sales report</div>
                    <div class="doc-date"><%: DayTitle %></div>
                    <div class="doc-meta">No. <%: ReportNo %> &middot; prepared <%: Prepared %></div>
                </div>
            </header>

            <% if (IsToday) { %>
            <div class="open-day"><%= Icons.Get("clock", "ico-sm") %>The day is still open. Figures run up to <%: PreparedAt %>; print again after closing for the final report.</div>
            <% } %>

            <%-- The four numbers the drawer check needs, then the smaller facts about the day. --%>
            <section class="summary">
                <div class="sum-box lead">
                    <div class="sum-label">Net sales</div>
                    <div class="sum-value"><%: Peso(NetTotal) %></div>
                    <div class="sum-note"><%: VersusLastWeek() %></div>
                </div>
                <div class="sum-box">
                    <div class="sum-label"><i class="key-box" style="background:#2f62c9"></i>Cash, net</div>
                    <div class="sum-value"><%: Peso(NetCash) %></div>
                    <div class="sum-note"><%: Peso(CashIn) %> in, <%: Peso(CashRefunds) %> refunded</div>
                </div>
                <div class="sum-box">
                    <div class="sum-label"><i class="key-box" style="background:#eb6834"></i>E-wallet, net</div>
                    <div class="sum-value"><%: Peso(NetEwallet) %></div>
                    <div class="sum-note"><%: Peso(EwalletIn) %> in, <%: Peso(EwalletRefunds) %> refunded</div>
                </div>
                <div class="sum-box">
                    <div class="sum-label">Job orders</div>
                    <div class="sum-value"><%= Orders %></div>
                    <div class="sum-note"><%= Voids %> voided &middot; <%: Kilos.ToString("0.0") %> kg &middot; avg <%: AveragePerOrder() %></div>
                </div>
            </section>

            <section class="block">
                <h2>Payments by hour</h2>
                <%= HourChart() %>
            </section>

            <div class="two-col">
                <section class="block">
                    <h2>By service</h2>
                    <table class="sheet-table">
                        <thead><tr><th>Service</th><th class="right">Orders</th><th class="right">Laundry</th><th class="right">Amount</th></tr></thead>
                        <tbody><%= ServiceRows() %></tbody>
                    </table>
                </section>
                <section class="block">
                    <h2>By staff</h2>
                    <table class="sheet-table">
                        <thead><tr><th>Staff</th><th class="right">Orders</th><th class="right">Voided</th><th class="right">Amount</th></tr></thead>
                        <tbody><%= StaffRows() %></tbody>
                    </table>
                </section>
            </div>

            <section class="block ledger-block">
                <h2>Payments and refunds</h2>
                <table class="sheet-table ledger">
                    <thead>
                        <tr><th>Time</th><th>Claim no.</th><th>Customer</th><th>Details</th><th>Method</th><th>Staff</th><th class="right">Amount</th></tr>
                    </thead>
                    <tbody><%= LedgerRows() %></tbody>
                    <tfoot>
                        <tr><td colspan="6">Net total (<%= Lines.Count %> <%= Lines.Count == 1 ? "line" : "lines" %>)</td><td class="right num"><%: Peso(NetTotal) %></td></tr>
                    </tfoot>
                </table>
                <p class="footnote">E-wallet amounts are as declared at the counter; the system does not verify them. Refunds go back by the same method they were paid.</p>
            </section>

            <%-- Filled in by hand at closing: the count of the drawer against what the system expects.
                 The drawer sum and the two signatures share one row, so a normal day fits on one sheet. --%>
            <div class="closing">
                <section class="block drawer">
                    <h2>Cash drawer check</h2>
                    <div class="drawer-grid">
                        <div class="drawer-row"><span>Opening cash (float)</span><span class="blank"></span></div>
                        <div class="drawer-row"><span>Cash sales, net of refunds</span><span class="val"><%: Peso(NetCash) %></span></div>
                        <div class="drawer-row strong"><span>Expected in drawer</span><span class="blank"></span></div>
                        <div class="drawer-row"><span>Counted cash</span><span class="blank"></span></div>
                        <div class="drawer-row strong"><span>Over / short</span><span class="blank"></span></div>
                    </div>
                </section>
                <section class="signatures">
                    <div><span class="line"></span>Counted by (staff) &middot; date</div>
                    <div><span class="line"></span>Checked by (owner) &middot; date</div>
                </section>
            </div>
        </article>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Print.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/Assets/js/Charts.js") %>"></script>
</body>
</html>
