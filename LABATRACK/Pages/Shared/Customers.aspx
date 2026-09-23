<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Customers.aspx.cs" Inherits="LABATRACK.Pages.Shared.Customers" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Customers · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Customers</h1>
                        <p class="sub">Look a customer up by contact number before adding them, so each person has one record and one history.</p>
                    </div>
                    <div class="page-actions">
                        <a class="btn btn-primary" href="NewJob.aspx"><%= Icons.Get("plus") %>New job order</a>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample customers from Helpers/SampleData.cs, kept for this visit only. --%>
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <div class="grid grid-main-side" style="align-items:start;">
                        <section class="card">
                            <asp:Panel ID="pnlSearch" runat="server" CssClass="card-h customer-search" DefaultButton="btnSearch">
                                <div class="search-input">
                                    <%= Icons.Get("search", "ico-sm") %>
                                    <asp:TextBox ID="txtSearch" runat="server" CssClass="input" MaxLength="100" placeholder="Name, contact number, or email" autocomplete="off" />
                                </div>
                                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn" OnClick="btnSearch_Click" />
                                <% if (Query != "") { %><a class="btn btn-ghost" href="Customers.aspx">Clear</a><% } %>
                            </asp:Panel>
                            <div class="list-caption small muted">
                                <% if (Query == "") { %><%= TotalCustomers %> customers, A to Z<% } else { %><%= ShownCustomers %> of <%= TotalCustomers %> customers match &ldquo;<%: Query %>&rdquo;<% } %>
                            </div>
                            <div class="table-wrap"<%= ShownCustomers == 0 ? " hidden" : "" %>>
                                <table class="table">
                                    <thead>
                                        <tr>
                                            <th>Customer</th>
                                            <th>Contact number</th>
                                            <th>Email</th>
                                            <th class="right">Orders</th>
                                            <th>Last drop-off</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:Repeater ID="rptCustomers" runat="server">
                                            <ItemTemplate>
                                                <tr class="<%# RowCss((int)Eval("Id")) %>">
                                                    <td>
                                                        <div class="cell-main"><%#: Eval("Name") %></div>
                                                        <div class="cell-sub">Since <%# ((DateTime)Eval("CreatedAt")).ToString("MMM d, yyyy") %></div>
                                                    </td>
                                                    <td class="mono"><%#: Eval("Contact") %></td>
                                                    <td class="small">
                                                        <%-- No email is allowed. The pick-up email is then logged as Skipped, not Failed. --%>
                                                        <%# string.IsNullOrEmpty((string)Eval("Email")) ? "<span class=\"muted\" title=\"Pick-up emails are skipped for this customer\">None</span>" : Server.HtmlEncode((string)Eval("Email")) %>
                                                    </td>
                                                    <td class="right num">
                                                        <%# (int)Eval("JobCount") == 0 ? "<span class=\"muted\">0</span>" : "<a href=\"History.aspx?q=" + HttpUtility.UrlEncode((string)Eval("Contact")) + "\" title=\"See their jobs in History\">" + Eval("JobCount") + "</a>" %>
                                                    </td>
                                                    <td class="num"><%# LastVisit((DateTime?)Eval("LastDropOff")) %></td>
                                                    <td class="right">
                                                        <%# IsOwner ? "<a class=\"btn btn-sm btn-ghost\" href=\"Customers.aspx?edit=" + Eval("Id") + "\">" + Icons.Get("pencil", "ico-sm") + "Edit</a>" : "" %>
                                                    </td>
                                                </tr>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </tbody>
                                </table>
                            </div>
                            <asp:Panel ID="pnlEmpty" runat="server" CssClass="empty-state" Visible="false">
                                <%= Icons.Get("user-plus", "ico-lg") %>
                                <div class="strong">No customer found</div>
                                <div class="muted small">If they are new, add them with the form. Their number is checked so no one is added twice.</div>
                            </asp:Panel>
                        </section>

                        <asp:Panel ID="pnlForm" runat="server" CssClass="card customer-form" DefaultButton="btnSave">
                            <div class="card-h">
                                <div>
                                    <h2><%= Editing != null ? "Edit customer" : "Add customer" %></h2>
                                    <div class="sub"><%= Editing != null ? "Owner only. Fixes apply to every past and future job." : "Name and contact number are required." %></div>
                                </div>
                                <%= Icons.Get(Editing != null ? "pencil" : "user-plus") %>
                            </div>
                            <div class="card-b form">
                                <div class="field">
                                    <label for="txtName">Name</label>
                                    <asp:TextBox ID="txtName" runat="server" MaxLength="100" autocomplete="off" />
                                    <asp:Label ID="lblNameError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                </div>
                                <div class="field">
                                    <label for="txtContact">Contact number</label>
                                    <asp:TextBox ID="txtContact" runat="server" MaxLength="20" inputmode="tel" autocomplete="off" placeholder="0917 555 0123" />
                                    <asp:Label ID="lblContactError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                </div>
                                <div class="field">
                                    <label for="txtEmail">Email <span class="muted">(optional)</span></label>
                                    <asp:TextBox ID="txtEmail" runat="server" MaxLength="150" inputmode="email" autocomplete="off" />
                                    <asp:Label ID="lblEmailError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                    <span class="hint">Used for the drop-off and ready-for-pick-up emails. Without it those emails are skipped.</span>
                                </div>
                            </div>
                            <div class="card-f">
                                <% if (Editing != null) { %><a class="btn btn-ghost" href="Customers.aspx">Cancel</a><% } %>
                                <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                            </div>
                        </asp:Panel>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
