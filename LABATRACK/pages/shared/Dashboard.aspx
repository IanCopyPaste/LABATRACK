<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="LABATRACK.pages.shared.Dashboard" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Job board · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Job board</h1>
                        <p class="sub">Every load from Queued to Ready for pick-up, oldest first. A load stays here until it is claimed or voided.</p>
                    </div>
                    <div class="page-actions">
                        <a class="btn btn-primary" href="NewJob.aspx"><%= Icons.Get("plus") %>New job order</a>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <section class="kpis">
                        <div class="card kpi">
                            <div class="kpi-top">Today's total <span class="kpi-icon"><%= Icons.Get("wallet", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Peso(Data.Today.NetTotal) %></div>
                            <div class="kpi-foot">Money received today, net of refunds</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">On the board <span class="kpi-icon"><%= Icons.Get("washing-machine", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Data.Active.Count %></div>
                            <div class="kpi-foot">Queued to Ready for pick-up</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Ready for pick-up <span class="kpi-icon" style="background:var(--success-soft); color:var(--success);"><%= Icons.Get("package-check", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Data.CountAt("Ready for pick-up") %></div>
                            <div class="kpi-foot">Waiting for the customer</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Needs attention <span class="kpi-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("circle-alert", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Data.NeedsAttention %></div>
                            <div class="kpi-foot"><%= Data.Unclaimed.Count %> unclaimed, <%= Data.VoidedToday.Count %> voided today<%= Data.RefundsWaiting.Count == 0 ? "" : ", " + Data.RefundsWaiting.Count + (Data.RefundsWaiting.Count == 1 ? " refund" : " refunds") + " waiting" %></div>
                        </div>
                    </section>

                    <div class="board-tools">
                        <div class="board-search">
                            <%= Icons.Get("search", "ico-sm") %>
                            <asp:TextBox ID="txtFilter" runat="server" TextMode="Search" CssClass="input" placeholder="Find by claim number, name, or contact number" autocomplete="off" data-board-filter="true" />
                        </div>
                        <span class="small muted" id="filterCount" aria-live="polite"></span>
                    </div>

                    <div class="board">
                        <asp:Repeater ID="rptColumns" runat="server">
                            <ItemTemplate>
                                <section class="board-col<%# ((System.Collections.ICollection)Eval("Jobs")).Count == 0 ? " is-empty" : "" %>" data-board-col="true">
                                    <div class="board-col-h">
                                        <span class="pill <%# StageCss((string)Eval("Stage")) %>"><%#: Eval("Stage") %></span>
                                        <span class="board-count" data-col-count="true"><%# ((System.Collections.ICollection)Eval("Jobs")).Count %></span>
                                    </div>
                                    <div class="board-cards">
                                        <asp:Repeater ID="rptCards" runat="server" DataSource='<%# Eval("Jobs") %>' OnItemCommand="Cards_ItemCommand">
                                            <ItemTemplate>
                                                <article class="job-card<%# ReworkNote((LABATRACK.models.JobOrder)Container.DataItem) == "" ? "" : " is-rework" %>" data-search="<%#: SearchText((LABATRACK.models.JobOrder)Container.DataItem) %>">
                                                    <div class="job-card-top">
                                                        <a class="mono" href="JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a>
                                                        <span class="age" title="Time since drop-off"><%# Icons.Get("clock", "ico-sm") %><%# Age((DateTime)Eval("CreatedAt")) %></span>
                                                    </div>
                                                    <div class="job-card-name"><%#: Eval("CustomerName") %></div>
                                                    <div class="job-card-sub"><%#: Eval("ServiceName") %> &middot; <%# ((decimal)Eval("WeightKg")).ToString("0.0") %> kg</div>
                                                    <%# RouteNote((LABATRACK.models.JobOrder)Container.DataItem) == "" ? "" : "<div class=\"job-card-route\">" + RouteNote((LABATRACK.models.JobOrder)Container.DataItem) + "</div>" %>
                                                    <%# string.IsNullOrEmpty((string)Eval("AddOnSummary")) ? "" : "<div class=\"job-card-addons\">" + Server.HtmlEncode((string)Eval("AddOnSummary")) + "</div>" %>
                                                    <%# ReworkNote((LABATRACK.models.JobOrder)Container.DataItem) == "" ? "" : "<div class=\"job-card-rework\">" + Icons.Get("rotate-ccw", "ico-sm") + "<span>" + Server.HtmlEncode(ReworkNote((LABATRACK.models.JobOrder)Container.DataItem)) + "</span></div>" %>
                                                    <%# (decimal)Eval("Balance") > 0 ? "<div class=\"job-card-balance\">" + Icons.Get("wallet", "ico-sm") + "<span>" + Peso((decimal)Eval("Balance")) + " still to pay</span></div>" : "" %>
                                                    <dl class="job-card-meta">
                                                        <dt>Drop-off</dt><dd><%# When((DateTime)Eval("CreatedAt")) %></dd>
                                                        <dt>Moved</dt><dd><%# When((DateTime)Eval("StatusSince")) %></dd>
                                                        <dt>By</dt><dd><%#: Eval("StatusBy") %></dd>
                                                    </dl>
                                                    <div class="job-card-actions">
                                                        <%# CollectFirst((LABATRACK.models.JobOrder)Container.DataItem) ? "<a class=\"btn btn-sm btn-primary\" href=\"JobDetail.aspx?id=" + Eval("JobId") + "#pay\">Collect " + Peso((decimal)Eval("Balance")) + "&hellip;</a>" : "" %>
                                                        <asp:Button runat="server" Visible='<%# !CollectFirst((LABATRACK.models.JobOrder)Container.DataItem) %>'
                                                            CssClass='<%# (string)Eval("CurrentStatus") == "Ready for pick-up" ? "btn btn-sm btn-primary" : "btn btn-sm" %>'
                                                            Text='<%# ActionLabel((LABATRACK.models.JobOrder)Container.DataItem) %>'
                                                            CommandName="Move" CommandArgument='<%# Eval("JobId") + "|" + Eval("CurrentStatus") %>'
                                                            data-confirm-title='<%# MoveConfirm.Title((LABATRACK.models.JobOrder)Container.DataItem) %>'
                                                            data-confirm-text='<%# MoveConfirm.Text((LABATRACK.models.JobOrder)Container.DataItem) %>'
                                                            data-confirm-ok='<%# ActionLabel((LABATRACK.models.JobOrder)Container.DataItem) %>' />
                                                        <%# (string)Eval("CurrentStatus") == "Inspection" ? "<a class=\"btn btn-sm btn-ghost\" href=\"JobDetail.aspx?id=" + Eval("JobId") + "\">Fail&hellip;</a>" : "" %>
                                                    </div>
                                                </article>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                        <div class="board-empty">No loads here</div>
                                    </div>
                                </section>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>

                    <div class="grid grid-2">
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Claimed today</h2>
                                    <div class="sub">Handed back to the customer, paid in full.</div>
                                </div>
                            </div>
                            <ul class="list">
                                <asp:Repeater ID="rptClaimed" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <span class="dot-icon" style="background:var(--success-soft); color:var(--success);"><%# Icons.Get("package-check", "ico-sm") %></span>
                                            <div class="grow">
                                                <div class="cell-main"><%#: Eval("CustomerName") %></div>
                                                <div class="cell-sub"><a class="mono" href="JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a> &middot; <%# When((DateTime)Eval("StatusSince")) %> &middot; <%#: Eval("StatusBy") %></div>
                                            </div>
                                            <span class="num strong"><%# Peso((decimal)Eval("TotalAmount")) %></span>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <% if (Data.ClaimedToday.Count == 0) { %><li class="muted small">Nothing claimed yet today.</li><% } %>
                            </ul>
                        </section>

                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Needs attention</h2>
                                    <div class="sub">Unclaimed after <%= Data.UnclaimedDays %> days, today's voids, and refunds waiting for the customer</div>
                                </div>
                            </div>
                            <ul class="list">
                                <asp:Repeater ID="rptUnclaimed" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%# Icons.Get("clock", "ico-sm") %></span>
                                            <div class="grow">
                                                <div class="cell-main"><%#: Eval("CustomerName") %> <a class="mono muted" href="JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></div>
                                                <div class="cell-sub"><%#: UnclaimedNote((LABATRACK.models.JobOrder)Container.DataItem) %></div>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <asp:Repeater ID="rptVoided" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <span class="dot-icon" style="background:var(--danger-soft); color:var(--danger);"><%# Icons.Get("ban", "ico-sm") %></span>
                                            <div class="grow">
                                                <div class="cell-main"><%#: Eval("CustomerName") %> <a class="mono muted" href="JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></div>
                                                <div class="cell-sub"><%#: VoidNote((LABATRACK.models.JobOrder)Container.DataItem) %></div>
                                            </div>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                                    <asp:Repeater ID="rptRefunds" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%# Icons.Get("hand-coins", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("CustomerName") %> <a class="mono muted" href="JobDetail.aspx?id=<%# Eval("JobId") %>"><%#: Eval("ClaimNumber") %></a></div>
                                                    <div class="cell-sub"><%#: RefundNote((LABATRACK.models.JobOrder)Container.DataItem) %></div>
                                                </div>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                <% if (Data.NeedsAttention == 0) { %><li class="muted small">Nothing needs attention.</li><% } %>
                            </ul>
                        </section>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Confirm.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Dashboard.js") %>"></script>
</body>
</html>
