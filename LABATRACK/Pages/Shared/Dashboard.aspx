<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="LABATRACK.Pages.Shared.Dashboard" %>

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
                    <%-- DRAFT: sample jobs. Moving a card works for this page visit only; Phase 4 saves it
                         through StatusService and reads the board from the shared repository query. --%>
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <%-- Staff see today's total only: no cash split, no other days. --%>
                    <section class="kpis">
                        <div class="card kpi">
                            <div class="kpi-top">Today's total <span class="kpi-icon"><%= Icons.Get("wallet", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= Peso(TodaysTotal) %></div>
                            <div class="kpi-foot">Paid at drop-off, net of refunds</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">On the board <span class="kpi-icon"><%= Icons.Get("washing-machine", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= ActiveCount %></div>
                            <div class="kpi-foot">Queued to Ready for pick-up</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Ready for pick-up <span class="kpi-icon" style="background:var(--success-soft); color:var(--success);"><%= Icons.Get("package-check", "ico-sm") %></span></div>
                            <div class="kpi-value"><%= ReadyCount %></div>
                            <div class="kpi-foot">Waiting for the customer</div>
                        </div>
                        <div class="card kpi">
                            <div class="kpi-top">Needs attention <span class="kpi-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("circle-alert", "ico-sm") %></span></div>
                            <div class="kpi-value">3</div>
                            <div class="kpi-foot">2 unclaimed, 1 voided today</div>
                        </div>
                    </section>

                    <div class="board-tools">
                        <div class="board-search">
                            <%= Icons.Get("search", "ico-sm") %>
                            <asp:TextBox ID="txtFilter" runat="server" TextMode="Search" CssClass="input" placeholder="Find by claim number, name, or contact number" autocomplete="off" data-board-filter="true" />
                        </div>
                        <span class="small muted" id="filterCount" aria-live="polite"></span>
                    </div>

                    <%-- One column per stage. The cards come from the same Repeater template in every column. --%>
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
                                                <article class="job-card<%# string.IsNullOrEmpty((string)Eval("Rework")) ? "" : " is-rework" %>" data-search="<%#: Eval("SearchText") %>">
                                                    <div class="job-card-top">
                                                        <a class="mono" href="JobDetail.aspx"><%#: Eval("Claim") %></a>
                                                        <span class="age" title="Time since drop-off"><%# Icons.Get("clock", "ico-sm") %><%# Age((DateTime)Eval("DroppedOff")) %></span>
                                                    </div>
                                                    <div class="job-card-name"><%#: Eval("Customer") %></div>
                                                    <div class="job-card-sub"><%#: Eval("Service") %> &middot; <%# ((decimal)Eval("WeightKg")).ToString("0.0") %> kg</div>
                                                    <%# string.IsNullOrEmpty((string)Eval("AddOns")) ? "" : "<div class=\"job-card-addons\">" + Server.HtmlEncode((string)Eval("AddOns")) + "</div>" %>
                                                    <%# string.IsNullOrEmpty((string)Eval("Rework")) ? "" : "<div class=\"job-card-rework\">" + Icons.Get("rotate-ccw", "ico-sm") + "<span>" + Server.HtmlEncode((string)Eval("Rework")) + "</span></div>" %>
                                                    <%-- The column already names the stage, so the second line only says when the
                                                         load got here and who moved it. --%>
                                                    <dl class="job-card-meta">
                                                        <dt>Drop-off</dt><dd><%# When((DateTime)Eval("DroppedOff")) %></dd>
                                                        <dt>Moved</dt><dd><%# When((DateTime)Eval("StageSince")) %></dd>
                                                        <dt>By</dt><dd><%#: Eval("StageBy") %></dd>
                                                    </dl>
                                                    <div class="job-card-actions">
                                                        <asp:Button runat="server" CssClass='<%# (string)Eval("Stage") == "Ready for pick-up" ? "btn btn-sm btn-primary" : "btn btn-sm" %>'
                                                            Text='<%# ActionLabel((string)Eval("Stage")) %>'
                                                            CommandName="Move" CommandArgument='<%# Eval("Claim") + "|" + Eval("Stage") %>' />
                                                        <%-- A failed inspection needs a reason, so it is recorded on the job page. --%>
                                                        <%# (string)Eval("Stage") == "Inspection" ? "<a class=\"btn btn-sm btn-ghost\" href=\"JobDetail.aspx?stage=Inspection\">Fail&hellip;</a>" : "" %>
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
                                    <div class="sub">Handed back to the customer. Already paid at drop-off.</div>
                                </div>
                            </div>
                            <ul class="list">
                                <asp:Repeater ID="rptClaimed" runat="server">
                                    <ItemTemplate>
                                        <li>
                                            <span class="dot-icon" style="background:var(--success-soft); color:var(--success);"><%# Icons.Get("package-check", "ico-sm") %></span>
                                            <div class="grow">
                                                <div class="cell-main"><%#: Eval("Customer") %></div>
                                                <div class="cell-sub"><span class="mono"><%#: Eval("Claim") %></span> &middot; <%# When((DateTime)Eval("StageSince")) %> &middot; <%#: Eval("StageBy") %></div>
                                            </div>
                                            <span class="num strong"><%# Peso((decimal)Eval("Total")) %></span>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </section>

                        <%-- DRAFT: sample rows. Phase 4 reads the unclaimed threshold from Settings. --%>
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
                                        <div class="cell-sub">Ready since Sep 13 &middot; 9 days unclaimed &middot; paid</div>
                                    </div>
                                </li>
                                <li>
                                    <span class="dot-icon" style="background:var(--warning-soft); color:var(--warning);"><%= Icons.Get("clock", "ico-sm") %></span>
                                    <div class="grow">
                                        <div class="cell-main">Dennis Uy <span class="mono muted">LT-260914-008</span></div>
                                        <div class="cell-sub">Ready since Sep 15 &middot; 7 days unclaimed &middot; paid</div>
                                    </div>
                                </li>
                                <li>
                                    <span class="dot-icon" style="background:var(--danger-soft); color:var(--danger);"><%= Icons.Get("ban", "ico-sm") %></span>
                                    <div class="grow">
                                        <div class="cell-main">Mark Tan <span class="mono muted">LT-260922-006</span></div>
                                        <div class="cell-sub">Voided 11:20 AM by jcruz &middot; &ldquo;Customer cancelled, wrong bag&rdquo; &middot; ₱120.00 refunded</div>
                                    </div>
                                </li>
                            </ul>
                        </section>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Dashboard.js") %>"></script>
</body>
</html>
