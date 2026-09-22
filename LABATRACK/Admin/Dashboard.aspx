<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="LABATRACK.Admin.Dashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Dashboard · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Good morning, owner</h1>
                        <p class="sub">Tuesday, 22 September 2026 &middot; here is how the shop is doing today.</p>
                    </div>
                    <div class="page-actions">
                        <a class="btn" href="DailySalesReport.aspx"><%= Icons.Get("receipt") %>Daily sales report</a>
                        <a class="btn btn-primary" href="../Pages/Shared/NewJob.aspx"><%= Icons.Get("plus") %>New job order</a>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: every number below is sample data. Phase 4 and 5 bind these to repositories. --%>

                    <!-- Today at a glance -->
                    <section class="kpis">
                        <div class="card kpi">
                            <div class="kpi-top">Today's sales <span class="kpi-icon"><%= Icons.Get("wallet", "ico-sm") %></span></div>
                            <div class="kpi-value">₱850.00</div>
                            <div class="kpi-foot">6 payments, 1 refund</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Cash vs e-wallet <span class="kpi-icon"><%= Icons.Get("banknote", "ico-sm") %></span></div>
                            <div class="kpi-value num" style="font-size:18px; margin-top:12px;">₱535.00 <span class="muted" style="font-weight:500;">/</span> ₱315.00</div>
                            <div class="split" style="margin-top:10px;">
                                <span style="width:62.9%; background:var(--primary);"></span>
                                <span style="width:37.1%; background:#9dbbef;"></span>
                            </div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Active loads <span class="kpi-icon"><%= Icons.Get("washing-machine", "ico-sm") %></span></div>
                            <div class="kpi-value">7</div>
                            <div class="kpi-foot">Queued to Ready for pick-up</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Needs attention <span class="kpi-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("circle-alert", "ico-sm") %></span></div>
                            <div class="kpi-value">3</div>
                            <div class="kpi-foot">2 unclaimed, 1 voided today</div>
                        </div>
                    </section>

                    <!-- Loads per stage -->
                    <section class="card">
                        <div class="pipeline">
                            <div class="pipe"><span class="pill st-queued">Queued</span><div class="count">2</div></div>
                            <div class="pipe"><span class="pill st-washing">Washing</span><div class="count">1</div></div>
                            <div class="pipe"><span class="pill st-drying">Drying</span><div class="count">1</div></div>
                            <div class="pipe"><span class="pill st-folding">Folding</span><div class="count">1</div></div>
                            <div class="pipe"><span class="pill st-inspection">Inspection</span><div class="count">1</div></div>
                            <div class="pipe"><span class="pill st-ready">Ready for pick-up</span><div class="count">1</div></div>
                        </div>
                    </section>

                    <div class="grid grid-main-side">
                        <!-- Active board: filtered by status, oldest first -->
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Active job orders</h2>
                                    <div class="sub">Everything not yet claimed or voided, oldest drop-off first</div>
                                </div>
                                <a class="btn btn-sm btn-ghost" href="#">View board <%= Icons.Get("arrow-right", "ico-sm") %></a>
                            </div>
                            <div class="table-wrap">
                                <table class="table">
                                    <thead>
                                        <tr>
                                            <th>Claim no.</th>
                                            <th>Customer</th>
                                            <th>Dropped off</th>
                                            <th>Status</th>
                                            <th class="right">Total</th>
                                            <th>Paid via</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260921-004</a></td>
                                            <td><div class="cell-main">Maria Santos</div><div class="cell-sub">Wash-Dry-Fold &middot; 5.1 kg</div></td>
                                            <td class="num">Sep 21, 9:12 AM</td>
                                            <td><span class="pill st-ready">Ready for pick-up</span></td>
                                            <td class="right num">₱305.00</td>
                                            <td><span class="pill pill-ok">Cash</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260921-007</a></td>
                                            <td><div class="cell-main">Jose Dela Cruz</div><div class="cell-sub">Wash-Dry-Fold &middot; 3.2 kg</div></td>
                                            <td class="num">Sep 21, 3:40 PM</td>
                                            <td><span class="pill st-inspection">Inspection</span></td>
                                            <td class="right num">₱160.00</td>
                                            <td><span class="pill pill-ok">Cash</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260922-001</a></td>
                                            <td><div class="cell-main">Ana Reyes</div><div class="cell-sub">Comforter / Bulky &middot; 2.0 kg</div></td>
                                            <td class="num">Sep 22, 8:05 AM</td>
                                            <td><span class="pill st-folding">Folding</span></td>
                                            <td class="right num">₱120.00</td>
                                            <td><span class="pill pill-ok">Cash</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260922-002</a></td>
                                            <td><div class="cell-main">Carlo Mendoza</div><div class="cell-sub">Wash-Dry-Fold &middot; 2.3 kg</div></td>
                                            <td class="num">Sep 22, 8:31 AM</td>
                                            <td><span class="pill st-drying">Drying</span></td>
                                            <td class="right num">₱135.00</td>
                                            <td><span class="pill pill-ok">Cash</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260922-003</a></td>
                                            <td><div class="cell-main">Liza Peñaflor</div><div class="cell-sub">Wash-Dry &middot; 4.6 kg</div></td>
                                            <td class="num">Sep 22, 9:02 AM</td>
                                            <td><span class="pill st-washing">Washing</span></td>
                                            <td class="right num">₱195.00</td>
                                            <td><span class="pill pill-ok">E-wallet</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260922-004</a></td>
                                            <td><div class="cell-main">Ramon Bautista</div><div class="cell-sub">Wash-Dry-Fold &middot; 7.0 kg</div></td>
                                            <td class="num">Sep 22, 10:15 AM</td>
                                            <td><span class="pill st-queued">Queued</span></td>
                                            <td class="right num">₱280.00</td>
                                            <td><span class="pill pill-ok">Cash</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                        <tr>
                                            <td class="mono"><a href="../Pages/Shared/JobDetail.aspx">LT-260922-005</a></td>
                                            <td><div class="cell-main">Grace Villanueva</div><div class="cell-sub">Wash-Dry-Fold &middot; 1.5 kg</div></td>
                                            <td class="num">Sep 22, 10:48 AM</td>
                                            <td><span class="pill st-queued">Queued</span></td>
                                            <td class="right num">₱120.00</td>
                                            <td><span class="pill pill-ok">E-wallet</span></td>
                                            <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </section>

                        <div class="stack">
                            <!-- Needs attention: unclaimed past threshold + voided today -->
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Needs attention</h2>
                                        <div class="sub">Unclaimed after 7 days, and today's voids</div>
                                    </div>
                                </div>
                                <ul class="list">
                                    <li>
                                        <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("clock", "ico-sm") %></span>
                                        <div class="grow">
                                            <div class="cell-main">Rosa Lim <span class="mono muted">LT-260912-003</span></div>
                                            <div class="cell-sub">Ready since Sep 13 &middot; 10 days unclaimed &middot; paid</div>
                                        </div>
                                    </li>
                                    <li>
                                        <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("clock", "ico-sm") %></span>
                                        <div class="grow">
                                            <div class="cell-main">Dennis Uy <span class="mono muted">LT-260914-008</span></div>
                                            <div class="cell-sub">Ready since Sep 15 &middot; 8 days unclaimed &middot; paid</div>
                                        </div>
                                    </li>
                                    <li>
                                        <span class="dot-icon" style="background:var(--danger-soft); color:var(--danger);"><%= Icons.Get("ban", "ico-sm") %></span>
                                        <div class="grow">
                                            <div class="cell-main">Mark Tan <span class="mono muted">LT-260922-006</span></div>
                                            <div class="cell-sub">Voided 11:20 AM by jcruz &middot; “Customer cancelled, wrong bag” &middot; ₱120.00 refunded</div>
                                        </div>
                                    </li>
                                </ul>
                            </section>

                            <!-- Claimed today -->
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Claimed today</h2>
                                        <div class="sub">2 loads handed back</div>
                                    </div>
                                </div>
                                <ul class="list">
                                    <li>
                                        <span class="dot-icon" style="background:var(--success-soft); color:var(--success);"><%= Icons.Get("package-check", "ico-sm") %></span>
                                        <div class="grow">
                                            <div class="cell-main">Paolo Garcia</div>
                                            <div class="cell-sub mono">LT-260920-011 &middot; 10:05 AM</div>
                                        </div>
                                        <span class="num strong">₱240.00</span>
                                    </li>
                                    <li>
                                        <span class="dot-icon" style="background:var(--success-soft); color:var(--success);"><%= Icons.Get("package-check", "ico-sm") %></span>
                                        <div class="grow">
                                            <div class="cell-main">Nena Aquino</div>
                                            <div class="cell-sub mono">LT-260921-002 &middot; 11:42 AM</div>
                                        </div>
                                        <span class="num strong">₱150.00</span>
                                    </li>
                                </ul>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
