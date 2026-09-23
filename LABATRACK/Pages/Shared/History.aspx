<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="LABATRACK.Pages.Shared.History" %>

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
                    <%-- DRAFT: sample jobs from Helpers/SampleData.cs. Phase 4 searches JobOrders instead. --%>
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
                            <%-- Sales totals are the owner's, so staff do not see this figure. --%>
                            <% if (IsOwner) { %>
                            <div class="result-sales">
                                <span class="muted small">Sales in these results</span>
                                <span class="num strong"><%= Peso(ResultSales) %></span>
                                <span class="muted small">voided jobs not counted</span>
                            </div>
                            <% } %>
                        </div>
                        <%-- With no rows the table is left out, so the empty message is not stuck under bare headings. --%>
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
                                        <th>Paid via</th>
                                        <% if (IsOwner) { %><th>Staff</th><% } %>
                                        <th><span class="sr-only">Actions</span></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptJobs" runat="server">
                                        <ItemTemplate>
                                            <tr class="<%# (string)Eval("Status") == "Voided" ? "is-voided" : "" %>">
                                                <td class="mono"><%#: Eval("Claim") %></td>
                                                <td><div class="cell-main"><%#: Eval("Customer") %></div><div class="cell-sub"><%#: Eval("Contact") %></div></td>
                                                <td><div class="cell-main"><%#: Eval("Service") %></div><div class="cell-sub"><%# ((decimal)Eval("WeightKg")).ToString("0.0") %> kg</div></td>
                                                <td class="num"><%# When((DateTime)Eval("DroppedOff")) %></td>
                                                <td>
                                                    <span class="pill <%# StageCss((string)Eval("Status")) %>"><%#: Eval("Status") %></span>
                                                    <div class="cell-sub" style="margin-top:3px;"><%#: Eval("StatusNote") %></div>
                                                </td>
                                                <td class="right num">
                                                    <span class="<%# (string)Eval("Status") == "Voided" ? "struck" : "" %>"><%# Peso((decimal)Eval("Total")) %></span>
                                                    <%# (string)Eval("Status") == "Voided" ? "<div class=\"cell-sub\">refunded</div>" : "" %>
                                                </td>
                                                <td><span class="pill pill-ok"><%#: Eval("Method") %></span></td>
                                                <% if (IsOwner) { %>
                                                <td class="small">
                                                    <div>Taken by <%#: Eval("CreatedBy") %></div>
                                                    <%# string.IsNullOrEmpty((string)Eval("ClosedBy")) ? "" : "<div class=\"muted\">" + ((string)Eval("Status") == "Voided" ? "Voided" : "Handed over") + " by " + Server.HtmlEncode((string)Eval("ClosedBy")) + "</div>" %>
                                                    <%# string.IsNullOrEmpty((string)Eval("VoidReason")) ? "" : "<div class=\"void-reason\">&ldquo;" + Server.HtmlEncode((string)Eval("VoidReason")) + "&rdquo;</div>" %>
                                                </td>
                                                <% } %>
                                                <td class="right">
                                                    <a class="btn btn-sm" href="<%# DetailLink((string)Eval("Status")) %>" aria-label="View job <%#: Eval("Claim") %>"><%# Icons.Get("eye", "ico-sm") %>View</a>
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
</body>
</html>
