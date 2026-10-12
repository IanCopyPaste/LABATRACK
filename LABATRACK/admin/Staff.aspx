<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Staff.aspx.cs" Inherits="LABATRACK.admin.Staff" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Staff accounts · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Staff accounts</h1>
                        <p class="sub">Add counter accounts, and manage each one: its username, its password, and whether it can sign in.</p>
                    </div>
                    <div class="page-actions">
                        <button type="button" class="btn btn-primary" data-user-add="true"><%= Icons.Get("user-plus") %>Add staff account</button>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <section class="card">
                        <div class="card-h">
                            <div>
                                <h2>Accounts</h2>
                                <div class="sub"><%= AccountCount %> <%= AccountCount == 1 ? "account" : "accounts" %> &middot; <%= CanSignInCount %> can sign in</div>
                            </div>
                        </div>
                        <div class="table-wrap">
                            <table class="table">
                                <thead>
                                    <tr>
                                        <th>Username</th>
                                        <th>Role</th>
                                        <th>Status</th>
                                        <th><span class="sr-only">Actions</span></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptUsers" runat="server">
                                        <ItemTemplate>
                                            <tr class="<%# RowCss((LABATRACK.models.UserAccount)Container.DataItem) %>"
                                                data-user-id="<%# Eval("UserId") %>" data-username="<%#: Eval("Username") %>"
                                                data-active="<%# (bool)Eval("IsActive") ? "1" : "0" %>" data-history="<%# (bool)Eval("HasHistory") ? "1" : "0" %>"
                                                data-status="<%#: StatusNote((LABATRACK.models.UserAccount)Container.DataItem) %>">
                                                <td>
                                                    <div class="cell-main<%# (bool)Eval("IsActive") ? "" : " muted" %>"><%#: Eval("Username") %></div>
                                                    <div class="cell-sub"><%# (bool)Eval("IsOwner") ? "Signs in at /admin/Login.aspx" : "Counter staff" %></div>
                                                </td>
                                                <td>
                                                    <%# (bool)Eval("IsOwner")
                                                        ? "<span class=\"pill pill-plain\" style=\"background:var(--primary-soft); color:var(--primary);\">Owner</span>"
                                                        : "<span class=\"pill pill-plain\">Staff</span>" %>
                                                </td>
                                                <td><%# StatusPill((LABATRACK.models.UserAccount)Container.DataItem) %></td>
                                                <td class="right">
                                                    <%# (bool)Eval("IsOwner")
                                                        ? "<span class=\"muted small\">You &middot; change it on Settings</span>"
                                                        : "<button type=\"button\" class=\"btn btn-sm btn-ghost\" data-user-manage=\"true\">" + Icons.Get("user-cog", "ico-sm") + "Manage</button>" %>
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </section>
                </div>
            </main>
        </div>

        <dialog id="userDialog" class="modal" aria-labelledby="userDialogTitle" data-open="<%= DialogOpen ? "true" : "false" %>">
            <input type="hidden" name="udId" id="udId" value="<%: DialogUserId %>" />
            <div class="modal-h">
                <div>
                    <h2 id="userDialogTitle"><%: DialogTitle %></h2>
                    <div class="sub" id="userDialogSub"><%: DialogSub %></div>
                </div>
                <button type="button" class="icon-btn" data-modal-close="true" aria-label="Close"><%= Icons.Get("x") %></button>
            </div>
            <div class="modal-b form">
                <div class="notice notice-danger" id="udError"<%= DialogError == null ? " hidden" : "" %>>
                    <%= Icons.Get("circle-alert", "ico-sm") %><span id="udErrorText"><%: DialogError %></span>
                </div>
                <div class="field">
                    <label for="udUsername">Username</label>
                    <input type="text" id="udUsername" name="udUsername" maxlength="50" autocomplete="off" placeholder="e.g. jcruz" value="<%: DialogUsername %>" />
                    <span class="error-text" id="udUsernameError"<%= UsernameError == null ? " hidden" : "" %>><%: UsernameError %></span>
                    <span class="hint" id="udRenameHint"<%= DialogIsManage ? "" : " hidden" %>>A new username takes effect at once: they sign in with it next time.</span>
                </div>
                <div class="field">
                    <label for="udPassword" id="udPasswordLabel"><%: DialogIsManage ? "New password" : "Password" %></label>
                    <div class="input-reveal">
                        <input type="password" id="udPassword" name="udPassword" maxlength="100" autocomplete="new-password" />
                        <button type="button" class="reveal-btn" data-reveal="udPassword" aria-pressed="false" aria-label="Show password" title="Show password"><%= Icons.Get("eye", "ico-sm when-hidden") %><%= Icons.Get("eye-off", "ico-sm when-shown") %></button>
                    </div>
                    <span class="error-text" id="udPasswordError"<%= PasswordError == null ? " hidden" : "" %>><%: PasswordError %></span>
                    <span class="hint" id="udPasswordHint"><%: DialogIsManage ? "Leave it blank to keep their current password." : "At least 8 characters. Share it with them in person." %></span>
                </div>
                <div class="field">
                    <span class="label">Status</span>
                    <label class="switch">
                        <input type="checkbox" id="udActive" name="udActive" value="1"<%= DialogActive ? " checked=\"checked\"" : "" %> />
                        <span class="track" aria-hidden="true"></span>
                        <span id="udActiveText"><%: DialogActive ? "Active · can sign in" : "Inactive · can't sign in" %></span>
                    </label>
                    <span class="hint">An inactive account keeps its name on every job and payment it touched; it just can't sign in.</span>
                </div>
                <div class="notice notice-warn" id="udDeleteNote"<%= DialogIsManage && DialogHasHistory ? "" : " hidden" %>>
                    <%= Icons.Get("info", "ico-sm") %>
                    <span>This account has jobs, payments or history on record, so it can't be deleted. Switch it to Inactive instead.</span>
                </div>
            </div>
            <div class="modal-f">
                <button type="submit" name="action" value="delete-user" class="btn btn-ghost btn-danger delete-user" id="udDelete"
                        data-confirm-title="Delete this account?" data-confirm-text="<%: DeleteConfirmText %>" data-confirm-ok="Delete account" data-confirm-danger="true"
                        <%= DialogIsManage ? "" : " hidden" %><%= DialogHasHistory ? " disabled=\"disabled\" title=\"It has jobs or payments on record. Switch it to Inactive instead.\"" : "" %>><%= Icons.Get("trash-2", "ico-sm") %>Delete</button>
                <button type="button" class="btn btn-ghost" data-modal-close="true">Cancel</button>
                <button type="submit" name="action" value="save-user" class="btn btn-primary" id="udSave"><%= Icons.Get("save", "ico-sm") %><span id="udSaveText"><%: DialogIsManage ? "Save changes" : "Create account" %></span></button>
            </div>
        </dialog>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/ShowPassword.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Confirm.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Staff.js") %>"></script>
</body>
</html>
