<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="LABATRACK.pages.shared.History" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>History · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server" defaultbutton="btnSearch">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>History</h1>
                        <p class="sub">Find any job order, past or current, by claim number, customer name, or contact number.</p>
                    </div>
                </header>

                <div class="content">
                    <section class="card">
                        <div class="card-b history-filters">
                            <div class="field grow">
                                <label for="txtSearch">Search</label>
                                <div class="search-input">
                                    <%= Icons.Get("search", "ico-sm") %>
                                    <asp:TextBox ID="txtSearch" runat="server" CssClass="input" MaxLength="100" placeholder="Claim number, name, or contact number" autocomplete="off" />
                                </div>
                            </div>
                            <div class="field">
                                <label for="ddlStatus">Status</label>
                                <asp:DropDownList ID="ddlStatus" runat="server">
                                    <asp:ListItem Value="">All jobs</asp:ListItem>
                                    <asp:ListItem Value="Active">Still in the shop</asp:ListItem>
                                    <asp:ListItem Value="Claimed">Claimed</asp:ListItem>
                                    <asp:ListItem Value="Voided">Voided</asp:ListItem>
                                </asp:DropDownList>
                            </div>
                            <div class="field">
                                <label for="txtFrom">Dropped off from</label>
                                <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
                            </div>
                            <div class="field">
                                <label for="txtTo">To</label>
                                <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
                            </div>
                            <div class="history-filter-actions">
                                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
                                <% if (HasFilter) { %><a class="btn btn-ghost" href="History.aspx">Clear</a><% } %>
                            </div>
                        </div>
                    </section>

                    <section class="card">
                        <div class="card-h">
                            <div>
                                <h2><%= ResultCount %> <%= ResultCount == 1 ? "job" : "jobs" %><%= HasFilter ? " found" : "" %></h2>
                                <div class="sub">Newest drop-off first</div>
                            </div>
                            <% if (IsOwner) { %>
                            <div class="result-sales">
                                <span class="muted small">Sales in these results</span>
                                <span class="num strong"><%= Peso(ResultSales) %></span>
                                <span class="muted small">voided jobs not counted</span>
                            </div>
                            <% } %>
                        </div>
                        <div class="table-wrap"<%= ResultCount == 0 ? " hidden" : "" %>>
                            <table class="table">
                                <thead>
                                    <tr>
                                        <th>Claim no.</th>
                                        <th>Customer</th>
                                        <th>Load</th>
                                        <th>Dropped off</th>
                                        <th>Status</th>
                                        <th class="right">Total</th>
                                        <th>Payment</th>
                                        <% if (IsOwner) { %><th>Staff</th><% } %>
                                        <th><span class="sr-only">Actions</span></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptJobs" runat="server">
                                        <ItemTemplate>
                                            <tr class="<%# (string)Eval("CurrentStatus") == "Voided" ? "is-voided" : "" %>">
                                                <td class="mono"><%#: Eval("ClaimNumber") %></td>
                                                <td><div class="cell-main"><%#: Eval("CustomerName") %></div><div class="cell-sub"><%#: Eval("ContactNumber") %></div></td>
                                                <td><div class="cell-main"><%#: Eval("ServiceName") %></div><div class="cell-sub"><%# ((decimal)Eval("WeightKg")).ToString("0.0") %> kg</div></td>
                                                <td class="num"><%# When((DateTime)Eval("CreatedAt")) %></td>
                                                <td>
                                                    <span class="pill <%# StageCss((string)Eval("CurrentStatus")) %>"><%#: Eval("CurrentStatus") %></span>
                                                    <div class="cell-sub" style="margin-top:3px;"><%#: StatusNote((LABATRACK.models.JobOrder)Container.DataItem) %></div>
                                                </td>
                                                <td class="right num">
                                                    <span class="<%# (string)Eval("CurrentStatus") == "Voided" ? "struck" : "" %>"><%# Peso((decimal)Eval("TotalAmount")) %></span>
                                                    <%# (string)Eval("CurrentStatus") == "Voided" ? "<div class=\"cell-sub\">voided</div>" : "" %>
                                                </td>
                                                <td><%# LABATRACK.tools.PaymentBadge.Html((LABATRACK.models.JobOrder)Container.DataItem) %></td>
                                                <% if (IsOwner) { %>
                                                <td class="small">
                                                    <div>Taken by <%#: Eval("CreatedByName") %></div>
                                                    <%# ClosedByNote((LABATRACK.models.JobOrder)Container.DataItem) %>
                                                </td>
                                                <% } %>
                                                <td class="right">
                                                    <a class="btn btn-sm" href="JobDetail.aspx?id=<%# Eval("JobId") %>" aria-label="View job <%#: Eval("ClaimNumber") %>"><%# Icons.Get("eye", "ico-sm") %>View</a>
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>

                        <asp:Panel ID="pnlEmpty" runat="server" CssClass="empty-state" Visible="false">
                            <%= Icons.Get("search", "ico-lg") %>
                            <div class="strong">No jobs match</div>
                            <div class="muted small">Check the claim number, or try part of the name or the last digits of the contact number.</div>
                        </asp:Panel>

                        <% if (PageCount > 1) { %>
                        <div class="card-f pager">
                            <span class="muted small">Page <%= PageNumber %> of <%= PageCount %></span>
                            <div class="pager-links">
                                <% if (PageNumber > 1) { %><a class="btn btn-sm" href="<%: PageLink(PageNumber - 1) %>"><%= Icons.Get("arrow-left", "ico-sm") %>Newer</a><% } %>
                                <% if (PageNumber < PageCount) { %><a class="btn btn-sm" href="<%: PageLink(PageNumber + 1) %>">Older<%= Icons.Get("arrow-right", "ico-sm") %></a><% } %>
                            </div>
                        </div>
                        <% } %>
                    </section>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
</body>
</html>
