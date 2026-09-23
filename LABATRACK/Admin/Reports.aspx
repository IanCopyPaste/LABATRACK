<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="LABATRACK.Admin.Reports" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Reports · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server" defaultbutton="btnShow">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Reports</h1>
                        <p class="sub"><strong><%: PeriodTitle %></strong> &middot; <%: PeriodNote %> &middot; compared with <%: PrevLabel %></p>
                    </div>
                    <div class="page-actions">
                        <a class="btn" href="DailySalesReport.aspx"><%= Icons.Get("printer", "ico-sm") %>Daily sales report</a>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: made-up history from Helpers/SampleSales.cs. Phase 5 builds the same figures in ReportService. --%>

                    <%-- One row of filters above everything. Every number and chart below covers this period only. --%>
                    <div class="report-filters">
                        <nav class="seg" aria-label="Report period">
                            <a href="<%: ViewLink("year") %>" class="<%= View == "year" ? "active" : "" %>">Year</a>
                            <a href="<%: ViewLink("month") %>" class="<%= View == "month" ? "active" : "" %>">Month</a>
                            <a href="<%: ViewLink("day") %>" class="<%= View == "day" ? "active" : "" %>">Day</a>
                        </nav>
                        <div class="period-pick">
                            <% if (PrevLink != null) { %><a class="btn btn-sm btn-ghost" href="<%: PrevLink %>" aria-label="Previous period"><%= Icons.Get("arrow-left", "ico-sm") %></a><% } else { %><span class="btn btn-sm btn-ghost is-off" aria-hidden="true"><%= Icons.Get("arrow-left", "ico-sm") %></span><% } %>
                            <% if (View == "year") { %>
                            <asp:DropDownList ID="ddlYear" runat="server" aria-label="Year" />
                            <% } else if (View == "month") { %>
                            <asp:TextBox ID="txtMonth" runat="server" TextMode="Month" aria-label="Month" CssClass="input" />
                            <% } else { %>
                            <asp:TextBox ID="txtDay" runat="server" TextMode="Date" aria-label="Day" CssClass="input" />
                            <% } %>
                            <asp:Button ID="btnShow" runat="server" Text="Show" CssClass="btn btn-sm" OnClick="btnShow_Click" />
                            <% if (NextLink != null) { %><a class="btn btn-sm btn-ghost" href="<%: NextLink %>" aria-label="Next period"><%= Icons.Get("arrow-right", "ico-sm") %></a><% } else { %><span class="btn btn-sm btn-ghost is-off" aria-hidden="true"><%= Icons.Get("arrow-right", "ico-sm") %></span><% } %>
                        </div>
                    </div>

                    <section class="kpis kpis-6">
                        <%= KpiTiles() %>
                    </section>

                    <%= SalesChart() %>

                    <% if (View == "day") { %>
                    <%= CompareChart() %>
                    <% } else { %>
                    <div class="grid grid-2">
                        <%= CompareChart() %>
                        <%= View == "year" ? YearsChart() : WeekdayChart() %>
                    </div>

                    <%= Heatmap() %>

                    <% if (View == "year") { %>
                    <%= WeekdayChart() %>
                    <% } %>
                    <% } %>

                    <div class="grid grid-2">
                        <%= ServicesCard() %>
                        <%= AddOnsCard() %>
                    </div>

                    <div class="grid grid-2">
                        <%= LoadSizeCard() %>
                        <%= TurnaroundCard() %>
                    </div>

                    <div class="grid grid-2">
                        <%= CustomersCard() %>
                        <%= TopCustomersCard() %>
                    </div>

                    <div class="grid grid-2">
                        <%= StaffCard() %>
                        <%= VoidsCard() %>
                    </div>

                    <%-- Not tied to the period above: these loads are waiting right now. --%>
                    <section class="card">
                        <div class="card-h">
                            <div>
                                <h2>Unclaimed loads, as of today</h2>
                                <div class="sub">Ready for pick-up longer than 7 days. Paid already, so call the customer rather than chase money.</div>
                            </div>
                        </div>
                        <div class="table-wrap">
                            <table class="table">
                                <thead><tr><th>Claim no.</th><th>Customer</th><th>Ready since</th><th class="right">Days waiting</th></tr></thead>
                                <tbody>
                                    <tr><td class="mono">LT-260912-003</td><td>Rosa Lim<div class="cell-sub">0917 555 0110</div></td><td class="num">Sep 13, 10:40 AM</td><td class="right num">9</td></tr>
                                    <tr><td class="mono">LT-260914-008</td><td>Dennis Uy<div class="cell-sub">0918 555 0111</div></td><td class="num">Sep 15, 9:30 AM</td><td class="right num">7</td></tr>
                                </tbody>
                            </table>
                        </div>
                    </section>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Charts.js") %>"></script>
</body>
</html>
