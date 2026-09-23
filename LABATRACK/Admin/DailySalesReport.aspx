<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="DailySalesReport.aspx.cs" Inherits="LABATRACK.Admin.DailySalesReport" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Daily sales report · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="sheet">
            <div class="toolbar">
                <a class="btn btn-ghost" href="Reports.aspx"><%= Icons.Get("arrow-left", "ico-sm") %>Back to reports</a>
                <div style="display:flex; gap:8px;">
                    <asp:TextBox ID="txtDate" runat="server" TextMode="Date" Text="2026-09-22" CssClass="input" style="width:auto;" />
                    <button type="button" class="btn btn-primary" data-print="true"><%= Icons.Get("printer", "ico-sm") %>Print</button>
                </div>
            </div>

            <%-- DRAFT: sample payments. Phase 5 reads Payments where PaidAt is inside the chosen day. --%>
            <div class="sheet-head">
                <div>
                    <div class="muted small">LabaTrack Laundry Shop</div>
                    <h1 style="margin-top:4px;">Daily sales report</h1>
                    <div class="muted">Tuesday, 22 September 2026</div>
                </div>
                <div class="right small muted">Printed Sep 22, 2026 6:05 PM<br />by owner</div>
            </div>

            <section class="card" style="margin-bottom:20px;">
                <div class="totals">
                    <div><div class="muted small">Cash (after refunds)</div><div class="v">₱535.00</div></div>
                    <div><div class="muted small">E-wallet</div><div class="v">₱315.00</div></div>
                    <div><div class="muted small">Refunds (in cash)</div><div class="v neg">−₱120.00</div></div>
                    <div><div class="muted small">Net total</div><div class="v">₱850.00</div></div>
                </div>
            </section>

            <section class="card">
                <div class="table-wrap">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Time</th>
                                <th>Claim no.</th>
                                <th>Customer</th>
                                <th>Method</th>
                                <th>Staff</th>
                                <th class="right">Amount</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr><td class="num">8:05 AM</td><td class="mono">LT-260922-001</td><td>Ana Reyes</td><td>Cash</td><td>jcruz</td><td class="right num">₱120.00</td></tr>
                            <tr><td class="num">8:31 AM</td><td class="mono">LT-260922-002</td><td>Carlo Mendoza</td><td>Cash</td><td>jcruz</td><td class="right num">₱135.00</td></tr>
                            <tr><td class="num">9:02 AM</td><td class="mono">LT-260922-003</td><td>Liza Peñaflor</td><td>E-wallet</td><td>jcruz</td><td class="right num">₱195.00</td></tr>
                            <tr><td class="num">10:15 AM</td><td class="mono">LT-260922-004</td><td>Ramon Bautista</td><td>Cash</td><td>arivera</td><td class="right num">₱280.00</td></tr>
                            <tr><td class="num">10:48 AM</td><td class="mono">LT-260922-005</td><td>Grace Villanueva</td><td>E-wallet</td><td>arivera</td><td class="right num">₱120.00</td></tr>
                            <tr><td class="num">11:05 AM</td><td class="mono">LT-260922-006</td><td>Mark Tan</td><td>Cash</td><td>jcruz</td><td class="right num">₱120.00</td></tr>
                            <tr><td class="num">11:20 AM</td><td class="mono">LT-260922-006</td><td>Mark Tan <span class="pill st-voided" style="margin-left:4px;">Refund on void</span></td><td>Cash</td><td>jcruz</td><td class="right num neg">−₱120.00</td></tr>
                        </tbody>
                        <tfoot>
                            <tr><td colspan="5">Net total (7 payment rows)</td><td class="right num">₱850.00</td></tr>
                        </tfoot>
                    </table>
                </div>
            </section>

            <div class="sign">
                <div>Counted by</div>
                <div>Checked by (owner)</div>
            </div>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Print.js") %>"></script>
</body>
</html>