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
                        <h1><%: Greeting %></h1>
                        <p class="sub"><%: Data.Now.ToString("dddd, d MMMM yyyy") %> &middot; here is how the shop is doing today.</p>
                    </div>
                    <div class="page-actions">
                        <a class="btn" href="DailySalesReport.aspx"><%= Icons.Get("receipt") %>Daily sales report</a>
                        <a class="btn btn-primary" href="../Pages/Shared/NewJob.aspx"><%= Icons.Get("plus") %>New job order</a>
                    </div>
                </header>

                <div class="content">
                    <%-- The same figures as the staff job board: both come from ReportService.GetDashboard. --%>

                    <!-- Today at a glance -->
                    <section class="kpis">
                        <div class="card kpi">
                            <div class="kpi-top">Today's sales <span class="kpi-icon"><%= Icons.Get("wallet", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Peso(Data.Today.NetTotal) %></div>
                            <div class="kpi-foot"><%= Data.Today.PaymentCount %> <%= Data.Today.PaymentCount == 1 ? "payment" : "payments" %>, <%= Data.Today.RefundCount %> <%= Data.Today.RefundCount == 1 ? "refund" : "refunds" %></div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Cash vs e-wallet <span class="kpi-icon"><%= Icons.Get("banknote", "ico-sm") %></span></div>
                            <div class="kpi-value num" style="font-size:18px; margin-top:12px;"><%= Peso(Data.Today.NetCash) %> <span class="muted" style="font-weight:500;">/</span> <%= Peso(Data.Today.NetEwallet) %></div>
                            <div class="split" style="margin-top:10px;">
                                <span style="width:<%= CashShare %>%; background:var(--primary);"></span>
                                <span style="width:<%= EwalletShare %>%; background:#9dbbef;"></span>
                            </div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Active loads <span class="kpi-icon"><%= Icons.Get("washing-machine", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Data.Active.Count %></div>
                            <div class="kpi-foot">Queued to Ready for pick-up</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Needs attention <span class="kpi-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("circle-alert", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Data.NeedsAttention %></div>
                            <div class="kpi-foot"><%= Data.Unclaimed.Count %> unclaimed, <%= Data.VoidedToday.Count %> voided today</div>
                        </div>
                    </section>

                    <!-- Loads per stage -->
                    <section class="card">
                        <div class="pipeline">
                            <div class="pipe"><span class="pill st-queued">Queued</span><div class="count"><%= Data.CountAt("Queued") %></div></div>
                            <div class="pipe"><span class="pill st-washing">Washing</span><div class="count"><%= Data.CountAt("Washing") %></div></div>
                            <div class="pipe"><span class="pill st-drying">Drying</span><div class="count"><%= Data.CountAt("Drying") %></div></div>
                            <div class="pipe"><span class="pill st-folding">Folding</span><div class="count"><%= Data.CountAt("Folding") %></div></div>
                            <div class="pipe"><span class="pill st-inspection">Inspection</span><div class="count"><%= Data.CountAt("Inspection") %></div></div>
                            <div class="pipe"><span class="pill st-ready">Ready for pick-up</span><div class="count"><%= Data.CountAt("Ready for pick-up") %></div></div>
                        </div>
                    </section>

                    <%-- The job list takes the full width so every column fits; the two short lists sit side by side under it. --%>
                        <!-- Everything still in the shop, newest drop-off first, with a stage filter (AdminDashboard.js) -->
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Active job orders</h2>
                                    <div class="sub" id="activeCaption">Everything not yet claimed or voided, newest drop-off first</div>
                                </div>
                                <div class="page-actions" style="align-items:center;">
                                    <label for="stageFilter" class="small muted">Stage</label>
                                    <select id="stageFilter" data-stage-filter="true" style="width:auto; height:32px;"<%= Data.Active.Count == 0 ? " disabled=\"disabled\"" : "" %>>
                                        <%= StageOptions() %>
                                    </select>
                                    <a class="btn btn-sm btn-ghost" href="../Pages/Shared/Dashboard.aspx">View board <%= Icons.Get("arrow-right", "ico-sm") %></a>
                                </div>
                            </div>
                            <div class="table-wrap" id="activeTable"<%= Data.Active.Count == 0 ? " hidden" : "" %>>
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
                                        <asp:Repeater ID="rptActive" runat="server">
                                            <ItemTemplate>
                                                <tr data-stage-row="<%#: Eval("CurrentStatus") %>">
                                                    <td class="mono"><a href="../Pages/Shared/JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></td>
                                                    <td><div class="cell-main"><%#: Eval("CustomerName") %></div><div class="cell-sub"><%#: Eval("ServiceName") %> &middot; <%# ((decimal)Eval("WeightKg")).ToString("0.0") %> kg</div></td>
                                                    <td class="num"><%# ((DateTime)Eval("CreatedAt")).ToString("MMM d, h:mm tt") %></td>
                                                    <td><span class="pill <%# StageCss((string)Eval("CurrentStatus")) %>"><%#: Eval("CurrentStatus") %></span></td>
                                                    <td class="right num"><%# Peso((decimal)Eval("TotalAmount")) %></td>
                                                    <td><span class="pill pill-ok"><%#: Eval("PaymentMethod") %></span></td>
                                                    <td class="right"><a class="btn btn-sm btn-ghost" href="EditJob.aspx?id=<%# Eval("JobId") %>"><%# Icons.Get("pencil", "ico-sm") %>Edit</a></td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                            <% if (Data.Active.Count == 0) { %>
                            <div class="empty-state">
                                <%= Icons.Get("washing-machine", "ico-lg") %>
                                <div class="strong">No loads in the shop</div>
                                <div class="muted small">New job orders appear here until they are claimed or voided.</div>
                            </div>
                            <% } %>
                            <div class="empty-state" id="stageEmpty" hidden>
                                <%= Icons.Get("washing-machine", "ico-lg") %>
                                <div class="strong">No loads at this stage</div>
                                <div class="muted small">Choose another stage, or All stages.</div>
                            </div>
                        </section>

                    <div class="grid grid-2" style="align-items:start;">

                            <!-- Needs attention: unclaimed past threshold + voided today -->
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Needs attention</h2>
                                        <div class="sub">Unclaimed after <%= Data.UnclaimedDays %> days, and today's voids</div>
                                    </div>
                                </div>
                                <ul class="list">
                                    <asp:Repeater ID="rptUnclaimed" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%# Icons.Get("clock", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("CustomerName") %> <a class="mono muted" href="../Pages/Shared/JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></div>
                                                    <div class="cell-sub"><%#: UnclaimedNote((LABATRACK.Models.JobOrder)Container.DataItem) %></div>
                                                </div>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <asp:Repeater ID="rptVoided" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:var(--danger-soft); color:var(--danger);"><%# Icons.Get("ban", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("CustomerName") %> <a class="mono muted" href="../Pages/Shared/JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></div>
                                                    <div class="cell-sub"><%#: VoidNote((LABATRACK.Models.JobOrder)Container.DataItem) %></div>
                                                </div>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <% if (Data.NeedsAttention == 0) { %><li class="muted small">Nothing needs attention.</li><% } %>
                                </ul>
                            </section>

                            <!-- Claimed today -->
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Claimed today</h2>
                                        <div class="sub"><%= Data.ClaimedToday.Count %> <%= Data.ClaimedToday.Count == 1 ? "load" : "loads" %> handed back</div>
                                    </div>
                                </div>
                                <ul class="list">
                                    <asp:Repeater ID="rptClaimed" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:var(--success-soft); color:var(--success);"><%# Icons.Get("package-check", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("CustomerName") %></div>
                                                    <div class="cell-sub mono"><%#: Eval("ClaimNumber") %> &middot; <%# ((DateTime)Eval("StatusSince")).ToString("h:mm tt") %></div>
                                                </div>
                                                <span class="num strong"><%# Peso((decimal)Eval("TotalAmount")) %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <% if (Data.ClaimedToday.Count == 0) { %><li class="muted small">Nothing claimed yet today.</li><% } %>
                                </ul>
                            </section>

                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/Assets/js/AdminDashboard.js") %>"></script>
</body>
</html>
