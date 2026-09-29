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
                    <%-- Customers from the database (CustomerService). Staff add and search; only the owner edits. --%>
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <%-- The form sits on top in one row, and the list below it gets the full width so no
                         column is squeezed. It only adds a customer: the owner edits one in a window of
                         its own (the Edit button on a row, Customers.js). --%>
                    <asp:Panel ID="pnlForm" runat="server" CssClass="card" DefaultButton="btnSave">
                        <div class="card-h">
                            <div>
                                <h2>Add customer</h2>
                                <div class="sub">Search the list below first. Name and contact number are required; email is optional and is used for the drop-off, update, cancellation and ready-for-pick-up emails.</div>
                            </div>
                            <%= Icons.Get("user-plus") %>
                        </div>
                        <div class="card-b form-strip">
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
                            </div>
                            <div class="form-strip-actions">
                                <asp:Button ID="btnSave" runat="server" Text="Add customer" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                            </div>
                        </div>
                    </asp:Panel>

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
                                            <%-- Each row carries what the Edit window needs (Customers.js reads it). --%>
                                            <tr class="<%# RowCss((int)Eval("CustomerId")) %>" data-customer-id="<%# Eval("CustomerId") %>" data-name="<%#: Eval("Name") %>" data-contact="<%#: Eval("ContactNumber") %>" data-email="<%#: Eval("Email") %>" data-since="<%# ((DateTime)Eval("CreatedAt")).ToString("MMM d, yyyy") %>">
                                                <td>
                                                    <div class="cell-main"><%#: Eval("Name") %></div>
                                                    <div class="cell-sub">Since <%# ((DateTime)Eval("CreatedAt")).ToString("MMM d, yyyy") %></div>
                                                </td>
                                                <td class="mono"><%#: Eval("ContactNumber") %></td>
                                                <td class="small">
                                                    <%-- No email is allowed. The pick-up email is then logged as Skipped, not Failed. --%>
                                                    <%# string.IsNullOrEmpty((string)Eval("Email")) ? "<span class=\"muted\" title=\"Pick-up emails are skipped for this customer\">None</span>" : Server.HtmlEncode((string)Eval("Email")) %>
                                                </td>
                                                <td class="right num">
                                                    <%# (int)Eval("JobCount") == 0 ? "<span class=\"muted\">0</span>" : "<a href=\"History.aspx?q=" + HttpUtility.UrlEncode((string)Eval("ContactNumber")) + "\" title=\"See their jobs in History\">" + Eval("JobCount") + "</a>" %>
                                                </td>
                                                <td class="num"><%# LastVisit((DateTime?)Eval("LastDropOff")) %></td>
                                                <td class="right">
                                                    <%# IsOwner ? "<button type=\"button\" class=\"btn btn-sm btn-ghost\" data-customer-edit=\"true\">" + Icons.Get("pencil", "ico-sm") + "Edit</button>" : "" %>
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
                            <div class="muted small">If they are new, add them with the form above. Their number is checked so no one is added twice.</div>
                        </asp:Panel>
                    </section>
                </div>
            </main>
        </div>

        <%-- The owner's window to edit one customer (staff only add). The Edit button on a row fills it from
             that row's data- attributes. It posts to the server, which checks everything again (CustomerService);
             after a refused save the server draws it open again (data-open) with what was typed and what is wrong. --%>
        <% if (IsOwner) { %>
        <dialog id="customerDialog" class="modal" aria-labelledby="customerDialogTitle" data-open="<%= DialogOpen ? "true" : "false" %>">
            <input type="hidden" name="cdId" id="cdId" value="<%: DialogId %>" />
            <div class="modal-h">
                <div>
                    <h2 id="customerDialogTitle">Edit <%: DialogName %></h2>
                    <div class="sub" id="customerDialogSub">Fixes apply to every past and future job of this customer.</div>
                </div>
                <button type="button" class="icon-btn" data-modal-close="true" aria-label="Close"><%= Icons.Get("x") %></button>
            </div>
            <div class="modal-b form">
                <div class="notice notice-danger" id="cdError"<%= DialogError == null ? " hidden" : "" %>>
                    <%= Icons.Get("circle-alert", "ico-sm") %><span id="cdErrorText"><%: DialogError %></span>
                </div>
                <div class="field">
                    <label for="cdName">Name</label>
                    <input type="text" id="cdName" name="cdName" maxlength="100" autocomplete="off" value="<%: DialogName %>" />
                    <span class="error-text" id="cdNameError"<%= NameError == null ? " hidden" : "" %>><%: NameError %></span>
                </div>
                <div class="field">
                    <label for="cdContact">Contact number</label>
                    <input type="text" id="cdContact" name="cdContact" maxlength="20" inputmode="tel" autocomplete="off" placeholder="0917 555 0123" value="<%: DialogContact %>" />
                    <span class="error-text" id="cdContactError"<%= ContactError == null ? " hidden" : "" %>><%: ContactError %></span>
                </div>
                <div class="field">
                    <label for="cdEmail">Email <span class="muted">(optional)</span></label>
                    <input type="text" id="cdEmail" name="cdEmail" maxlength="150" inputmode="email" autocomplete="off" value="<%: DialogEmail %>" />
                    <span class="error-text" id="cdEmailError"<%= EmailError == null ? " hidden" : "" %>><%: EmailError %></span>
                    <span class="hint">The drop-off, update, cancellation and ready-for-pick-up emails go to this address. Leave it empty if they have none.</span>
                </div>
            </div>
            <div class="modal-f">
                <button type="button" class="btn btn-ghost" data-modal-close="true">Cancel</button>
                <button type="submit" name="action" value="save-customer" class="btn btn-primary" id="cdSave"><%= Icons.Get("save", "ico-sm") %>Save changes</button>
            </div>
        </dialog>
        <% } %>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/Assets/js/NumericInput.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/Assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/Assets/js/Customers.js") %>"></script>
</body>
</html>
