<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="LABATRACK.Admin.Reports" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Reports · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Reports</h1>
                        <p class="sub">September 2026 (month to date), compared with August 2026.</p>
                    </div>
                    <div class="page-actions">
                        <asp:DropDownList ID="ddlPeriod" runat="server" CssClass="input" style="width:auto;">
                            <asp:ListItem Value="daily">Daily</asp:ListItem>
                            <asp:ListItem Value="monthly" Selected="True">Monthly</asp:ListItem>
                        </asp:DropDownList>
                        <asp:TextBox ID="txtMonth" runat="server" TextMode="Month" Text="2026-09" CssClass="input" style="width:auto;" />
                        <a class="btn" href="DailySalesReport.aspx"><%= Icons.Get("printer", "ico-sm") %>Daily sales report</a>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample figures. Phase 5 builds these in ReportService. --%>
                    <section class="kpis">
                        <div class="card kpi">
                            <div class="kpi-top">Sales <span class="kpi-icon"><%= Icons.Get("wallet", "ico-sm") %></span></div>
                            <div class="kpi-value">₱48,320.00</div>
                            <div class="kpi-foot"><span class="up"><%= Icons.Get("trending-up", "ico-sm") %> 9.4%</span> vs ₱44,150.00</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Job orders <span class="kpi-icon"><%= Icons.Get("clipboard-list", "ico-sm") %></span></div>
                            <div class="kpi-value">212</div>
                            <div class="kpi-foot"><span class="up"><%= Icons.Get("trending-up", "ico-sm") %> 7.1%</span> vs 198</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Average turnaround <span class="kpi-icon"><%= Icons.Get("timer", "ico-sm") %></span></div>
                            <div class="kpi-value">26.4 h</div>
                            <div class="kpi-foot"><span class="up"><%= Icons.Get("trending-down", "ico-sm") %> 2.5 h faster</span> vs 28.9 h</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Unclaimed loads <span class="kpi-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("clock", "ico-sm") %></span></div>
                            <div class="kpi-value">2</div>
                            <div class="kpi-foot">Ready for more than 7 days</div>
                        </div>
                    </section>

                    <div class="grid grid-2">
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Cash vs e-wallet</h2>
                                    <div class="sub">From payment dates (what the drawer check needs)</div>
                                </div>
                            </div>
                            <div class="card-b">
                                <div class="split" style="height:12px;">
                                    <span style="width:61.8%; background:var(--primary);"></span>
                                    <span style="width:38.2%; background:#9dbbef;"></span>
                                </div>
                                <dl class="dl" style="margin-top:16px;">
                                    <dt><span style="display:inline-block; width:10px; height:10px; border-radius:3px; background:var(--primary);"></span> Cash</dt><dd>₱29,870.00 <span class="muted">(61.8%)</span></dd>
                                    <dt><span style="display:inline-block; width:10px; height:10px; border-radius:3px; background:#9dbbef;"></span> E-wallet</dt><dd>₱18,450.00 <span class="muted">(38.2%)</span></dd>
                                </dl>
                                <div class="divider" style="margin:14px 0;"></div>
                                <div class="total-line" style="font-size:14px;"><span>Total</span><span class="num">₱48,320.00</span></div>
                            </div>
                        </section>

                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Most availed services</h2>
                                    <div class="sub">From job creation dates</div>
                                </div>
                            </div>
                            <table class="table">
                                <thead><tr><th>Service</th><th>Share</th><th class="right">Jobs</th></tr></thead>
                                <tbody>
                                    <tr><td class="cell-main">Wash-Dry-Fold</td><td><div class="bar"><span style="width:69.8%;"></span></div></td><td class="right num">148</td></tr>
                                    <tr><td class="cell-main">Wash-Dry</td><td><div class="bar"><span style="width:19.3%;"></span></div></td><td class="right num">41</td></tr>
                                    <tr><td class="cell-main">Comforter / Bulky</td><td><div class="bar"><span style="width:10.8%;"></span></div></td><td class="right num">23</td></tr>
                                </tbody>
                            </table>
                        </section>
                    </div>

                    <div class="grid grid-2">
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Last 7 days</h2>
                                    <div class="sub">Net payments per day</div>
                                </div>
                            </div>
                            <table class="table">
                                <thead><tr><th>Day</th><th></th><th class="right">Sales</th></tr></thead>
                                <tbody>
                                    <tr><td>Wed, Sep 16</td><td><div class="bar"><span style="width:62.5%;"></span></div></td><td class="right num">₱1,820.00</td></tr>
                                    <tr><td>Thu, Sep 17</td><td><div class="bar"><span style="width:73.5%;"></span></div></td><td class="right num">₱2,140.00</td></tr>
                                    <tr><td>Fri, Sep 18</td><td><div class="bar"><span style="width:67.4%;"></span></div></td><td class="right num">₱1,960.00</td></tr>
                                    <tr><td>Sat, Sep 19</td><td><div class="bar"><span style="width:85.2%;"></span></div></td><td class="right num">₱2,480.00</td></tr>
                                    <tr><td>Sun, Sep 20</td><td><div class="bar"><span style="width:100%;"></span></div></td><td class="right num">₱2,910.00</td></tr>
                                    <tr><td>Mon, Sep 21</td><td><div class="bar"><span style="width:79.2%;"></span></div></td><td class="right num">₱2,305.00</td></tr>
                                    <tr><td>Tue, Sep 22 <span class="muted small">today</span></td><td><div class="bar"><span style="width:29.2%;"></span></div></td><td class="right num">₱850.00</td></tr>
                                </tbody>
                            </table>
                        </section>

                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Unclaimed loads</h2>
                                    <div class="sub">Ready for pick-up longer than 7 days</div>
                                </div>
                            </div>
                            <table class="table">
                                <thead><tr><th>Claim no.</th><th>Customer</th><th class="right">Days</th></tr></thead>
                                <tbody>
                                    <tr><td class="mono">LT-260912-003</td><td>Rosa Lim<div class="cell-sub">0918 555 0177</div></td><td class="right num">10</td></tr>
                                    <tr><td class="mono">LT-260914-008</td><td>Dennis Uy<div class="cell-sub">0927 555 0310</div></td><td class="right num">8</td></tr>
                                </tbody>
                            </table>
                        </section>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
