<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Staff.aspx.cs" Inherits="LABATRACK.Admin.Staff" %>

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
                        <p class="sub">Create counter accounts, reset passwords, and disable people who left. Accounts are never deleted.</p>
                    </div>
                </header>

                <div class="content">
                    <div class="grid grid-main-side">
                        <%-- DRAFT: sample rows. Phase 2 binds this to the Users table. --%>
                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Accounts</h2>
                                    <div class="sub">5 accounts &middot; 3 can sign in</div>
                                </div>
                            </div>
                            <div class="table-wrap">
                                <table class="table">
                                    <thead>
                                        <tr>
                                            <th>Username</th>
                                            <th>Role</th>
                                            <th>Status</th>
                                            <th class="right">Failed logins</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr>
                                            <td><div class="cell-main">owner</div><div class="cell-sub">Signs in at /Admin/Login.aspx</div></td>
                                            <td><span class="pill pill-plain" style="background:var(--primary-soft); color:var(--primary);">Owner</span></td>
                                            <td><span class="pill pill-ok">Active</span></td>
                                            <td class="right num">0</td>
                                            <td class="right muted small">You</td>
                                        </tr>
                                        <tr>
                                            <td><div class="cell-main">jcruz</div><div class="cell-sub">Counter staff</div></td>
                                            <td><span class="pill pill-plain">Staff</span></td>
                                            <td><span class="pill pill-ok">Active</span></td>
                                            <td class="right num">0</td>
                                            <td>
                                                <div class="row-actions">
                                                    <button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("key-round", "ico-sm") %>Reset password</button>
                                                    <button type="button" class="btn btn-sm btn-ghost btn-danger"><%= Icons.Get("user-x", "ico-sm") %>Disable</button>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><div class="cell-main">arivera</div><div class="cell-sub">Counter staff</div></td>
                                            <td><span class="pill pill-plain">Staff</span></td>
                                            <td><span class="pill pill-ok">Active</span></td>
                                            <td class="right num">1</td>
                                            <td>
                                                <div class="row-actions">
                                                    <button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("key-round", "ico-sm") %>Reset password</button>
                                                    <button type="button" class="btn btn-sm btn-ghost btn-danger"><%= Icons.Get("user-x", "ico-sm") %>Disable</button>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><div class="cell-main">bsantos</div><div class="cell-sub">Counter staff</div></td>
                                            <td><span class="pill pill-plain">Staff</span></td>
                                            <td><span class="pill pill-warn">Locked until 11:32 AM</span></td>
                                            <td class="right num">5</td>
                                            <td>
                                                <div class="row-actions">
                                                    <button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("key-round", "ico-sm") %>Reset password</button>
                                                    <button type="button" class="btn btn-sm btn-ghost btn-danger"><%= Icons.Get("user-x", "ico-sm") %>Disable</button>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><div class="cell-main muted">kmorales</div><div class="cell-sub">Counter staff</div></td>
                                            <td><span class="pill pill-plain">Staff</span></td>
                                            <td><span class="pill st-claimed">Disabled</span></td>
                                            <td class="right num">0</td>
                                            <td>
                                                <div class="row-actions">
                                                    <button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("circle-check", "ico-sm") %>Enable</button>
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </section>

                        <section class="card">
                            <div class="card-h">
                                <div>
                                    <h2>Add a staff account</h2>
                                    <div class="sub">Share the temporary password in person</div>
                                </div>
                            </div>
                            <div class="card-b form">
                                <div class="field">
                                    <label for="txtNewUsername">Username</label>
                                    <asp:TextBox ID="txtNewUsername" runat="server" MaxLength="50" placeholder="e.g. jcruz" />
                                </div>
                                <div class="field">
                                    <label for="txtNewPassword">Temporary password</label>
                                    <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" MaxLength="100" />
                                    <span class="hint">At least 8 characters. Stored only as a salted PBKDF2 hash.</span>
                                </div>
                                <div class="field">
                                    <label for="txtConfirmPassword">Confirm password</label>
                                    <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" MaxLength="100" />
                                </div>
                                <div class="notice notice-info">
                                    <%= Icons.Get("info", "ico-sm") %>
                                    <span>New accounts get the Staff role. Five wrong passwords lock the account for a few minutes.</span>
                                </div>
                            </div>
                            <div class="card-f">
                                <button type="button" class="btn btn-primary"><%= Icons.Get("user-plus", "ico-sm") %>Create account</button>
                            </div>
                        </section>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
