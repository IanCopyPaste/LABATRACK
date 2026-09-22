<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobDetail.aspx.cs" Inherits="LABATRACK.Pages.Shared.JobDetail" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Job LT-260921-004 · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <a class="small" href="../../Admin/Dashboard.aspx"><%= Icons.Get("arrow-left", "ico-sm") %> Back to dashboard</a>
                        <h1 style="margin-top:8px;"><span class="mono" style="font-size:20px;">LT-260921-004</span>
                            <span class="pill <%= StageCss(CurrentStage) %>" style="margin-left:8px; vertical-align:middle;"><%: CurrentStage %></span></h1>
                        <p class="sub">Dropped off Sep 21, 2026 9:12 AM by jcruz &middot; expected pick-up Sep 21, 5:00 PM (rush)</p>
                    </div>
                    <div class="page-actions">
                        <a class="btn" href="JobSlip.aspx"><%= Icons.Get("printer", "ico-sm") %>Print slip</a>
                        <a class="btn" href="../../Admin/EditJob.aspx"><%= Icons.Get("pencil", "ico-sm") %>Edit <span class="muted small">(owner)</span></a>
                    </div>
                </header>

                <div class="content">
                    <div class="notice notice-warn preview-links">
                        <%= Icons.Get("eye", "ico-sm") %>
                        <span><strong>Design draft.</strong> Preview this job at another stage:
                            <a href="?stage=Queued">Queued</a><a href="?stage=Washing">Washing</a><a href="?stage=Inspection">Inspection</a><a href="?stage=Ready">Ready</a><a href="?stage=Claimed">Claimed</a><a href="?stage=Voided">Voided</a></span>
                    </div>

                    <!-- Header: customer and load, shown once -->
                    <section class="card">
                        <div class="card-b grid grid-3" style="gap:24px;">
                            <div>
                                <div class="label muted">Customer</div>
                                <div class="strong" style="font-size:16px; margin-top:4px;">Maria Santos</div>
                                <div class="muted small"><%= Icons.Get("phone", "ico-sm") %> 0917 555 0123 &nbsp; <%= Icons.Get("mail", "ico-sm") %> maria.santos@example.com</div>
                            </div>
                            <div>
                                <div class="label muted">Load</div>
                                <div class="strong" style="font-size:16px; margin-top:4px;">Wash-Dry-Fold &middot; 5.1 kg</div>
                                <div class="muted small">Billed as 6 kg at ₱40.00/kg &middot; Fabric conditioner, Rush</div>
                            </div>
                            <div>
                                <div class="label muted">Remarks (intake check)</div>
                                <div style="margin-top:4px;">1 blouse with a loose button. No stains noted.</div>
                            </div>
                        </div>
                    </section>

                    <!-- Stage stepper, read from JobStatusHistory -->
                    <section class="card <%= IsVoided ? "voided-banner" : "" %>">
                        <% if (IsVoided) { %>
                        <div class="card-b" style="display:flex; gap:12px; align-items:center; color:var(--danger);">
                            <%= Icons.Get("ban", "ico-lg") %>
                            <div>
                                <div class="strong">Voided Sep 21, 9:20 AM by jcruz</div>
                                <div class="small">Reason: Customer changed mind, took the load home. The ₱305.00 payment was refunded in cash.</div>
                            </div>
                        </div>
                        <% } %>
                        <div class="stepper">
                            <asp:Repeater ID="rptStepper" runat="server">
                                <ItemTemplate>
                                    <div class="step <%# Eval("State") %>">
                                        <div class="step-dot"><%# (string)Eval("State") == "done" ? Icons.Get("check", "ico-sm") : Eval("Number").ToString() %></div>
                                        <div>
                                            <div class="step-name"><%#: Eval("Name") %></div>
                                            <div class="step-meta"><%#: Eval("When") %><br /><%#: Eval("By") %></div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </section>

                    <div class="grid grid-main-side">
                        <div class="stack">
                            <!-- Action for the CURRENT stage only -->
                            <asp:Panel ID="pnlQueued" runat="server" CssClass="card">
                                <div class="card-h"><div><h2>Queued</h2><div class="sub">Waiting for a free machine</div></div></div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Put the load in a washer, then record it here.</span>
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("arrow-right", "ico-sm") %>Move to Washing</button>
                                </div>
                                <div class="card-f" style="justify-content:space-between; flex-wrap:wrap; align-items:flex-end;">
                                    <div class="field" style="flex:1; min-width:220px;">
                                        <label for="txtVoidReason">Void this job (only while Queued). The full ₱305.00 is refunded.</label>
                                        <asp:TextBox ID="txtVoidReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                    </div>
                                    <button type="button" class="btn btn-danger"><%= Icons.Get("ban", "ico-sm") %>Void job</button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlAdvance" runat="server" CssClass="card">
                                <div class="card-h"><div><h2><%: CurrentStage %> in progress</h2><div class="sub">Advance only when this step is finished</div></div></div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Next step: <strong><%: NextStage %></strong></span>
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("arrow-right", "ico-sm") %>Move to <%: NextStage %></button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlInspection" runat="server" CssClass="card">
                                <div class="card-h"><div><h2>Inspection</h2><div class="sub">Check the finished load before shelving it</div></div></div>
                                <div class="card-b grid grid-2" style="align-items:start;">
                                    <div class="stack" style="gap:10px;">
                                        <div class="strong" style="color:var(--success);"><%= Icons.Get("circle-check", "ico-sm") %> Pass</div>
                                        <span class="muted small">Moves the load to Ready for pick-up and sends the ready email.</span>
                                        <button type="button" class="btn btn-primary"><%= Icons.Get("check", "ico-sm") %>Pass inspection</button>
                                    </div>
                                    <div class="stack" style="gap:10px;">
                                        <div class="strong" style="color:var(--warning);"><%= Icons.Get("rotate-ccw", "ico-sm") %> Fail, send back</div>
                                        <asp:DropDownList ID="ddlReworkStage" runat="server">
                                            <asp:ListItem>Washing</asp:ListItem>
                                            <asp:ListItem>Drying</asp:ListItem>
                                            <asp:ListItem Selected="True">Folding</asp:ListItem>
                                        </asp:DropDownList>
                                        <asp:TextBox ID="txtReworkReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                        <button type="button" class="btn"><%= Icons.Get("rotate-ccw", "ico-sm") %>Send back</button>
                                    </div>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlReady" runat="server" CssClass="card">
                                <div class="card-h">
                                    <div><h2>Release to customer</h2><div class="sub">Already paid in full at drop-off</div></div>
                                    <span class="pill pill-ok">Paid</span>
                                </div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Check the customer's slip matches claim number <strong class="mono">LT-260921-004</strong>, then hand over the load.</span>
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("package-check", "ico-sm") %>Mark as claimed</button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlClosed" runat="server" CssClass="card">
                                <div class="card-b" style="display:flex; gap:12px; align-items:center;">
                                    <%= Icons.Get(IsVoided ? "circle-x" : "package-check", "ico-lg") %>
                                    <div>
                                        <div class="strong"><%= IsVoided ? "This job was voided." : "Claimed by the customer Sep 22, 4:10 PM." %></div>
                                        <div class="muted small">Claimed and Voided are final. The job stays in history and cannot change.</div>
                                    </div>
                                </div>
                            </asp:Panel>

                            <!-- Status history, newest first. Rows are never edited or deleted. -->
                            <section class="card">
                                <div class="card-h"><div><h2>Status history</h2><div class="sub">Every change, who made it, and when</div></div></div>
                                <div class="table-wrap">
                                    <table class="table">
                                        <thead><tr><th>When</th><th>Change</th><th>By</th><th>Reason</th></tr></thead>
                                        <tbody>
                                            <asp:Repeater ID="rptHistory" runat="server">
                                                <ItemTemplate>
                                                    <tr class="<%# string.IsNullOrEmpty((string)Eval("Reason")) ? "" : "rework" %>">
                                                        <td class="num"><%#: Eval("At") %></td>
                                                        <td>
                                                            <%# string.IsNullOrEmpty((string)Eval("From")) ? "<span class=\"muted\">Created</span>" : "<span class=\"pill " + StageCss((string)Eval("From")) + "\">" + Server.HtmlEncode((string)Eval("From")) + "</span>" %>
                                                            <%= Icons.Get("arrow-right", "ico-sm") %>
                                                            <span class="pill <%# StageCss((string)Eval("To")) %>"><%#: Eval("To") %></span>
                                                        </td>
                                                        <td><%#: Eval("By") %></td>
                                                        <td class="small"><%#: Eval("Reason") %></td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </tbody>
                                    </table>
                                </div>
                            </section>
                        </div>

                        <div class="stack">
                            <!-- Payment: one full payment at drop-off, plus a refund row if voided -->
                            <section class="card">
                                <div class="card-h">
                                    <div><h2>Payment</h2><div class="sub">Price frozen when the job was created</div></div>
                                    <span class="pill <%= IsVoided ? "pill-danger" : "pill-ok" %>"><%= IsVoided ? "Refunded" : "Paid in full" %></span>
                                </div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt>6 kg &times; ₱40.00</dt><dd>₱240.00</dd>
                                        <dt>Fabric conditioner</dt><dd>₱15.00</dd>
                                        <dt>Rush (same day)</dt><dd>₱50.00</dd>
                                    </dl>
                                    <div class="divider" style="margin:12px 0;"></div>
                                    <div class="total-line" style="font-size:15px;"><span>Total</span><span class="num"><%= Peso(Total) %></span></div>
                                </div>
                                <ul class="list" style="border-top:1px solid var(--border);">
                                    <asp:Repeater ID="rptPayments" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:<%# (bool)Eval("IsRefund") ? "var(--danger-soft); color:var(--danger)" : "var(--success-soft); color:var(--success)" %>;"><%# Icons.Get((string)Eval("Method") == "Cash" ? "banknote" : "smartphone", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%# (bool)Eval("IsRefund") ? "Refund on void" : Server.HtmlEncode((string)Eval("Method")) %></div>
                                                    <div class="cell-sub"><%#: Eval("At") %> &middot; <%#: Eval("By") %></div>
                                                </div>
                                                <span class="num strong"><%# Peso((decimal)Eval("Amount")) %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                                <div class="card-f" style="flex-direction:column; gap:4px;">
                                    <div class="row-between"><span class="muted">Cash received</span><span class="num"><%= Peso(CashReceived) %></span></div>
                                    <div class="row-between"><span class="muted">Change given</span><span class="num"><%= Peso(Change) %></span></div>
                                </div>
                            </section>

                            <!-- Email log with resend -->
                            <section class="card">
                                <div class="card-h"><div><h2>Notifications</h2><div class="sub">Email only. A failed send never blocks the job.</div></div></div>
                                <ul class="list">
                                    <li>
                                        <span class="dot-icon" style="background:var(--primary-soft); color:var(--primary);"><%= Icons.Get("mail", "ico-sm") %></span>
                                        <div class="grow"><div class="cell-main">Drop-off email</div><div class="cell-sub">Sep 21, 9:13 AM</div></div>
                                        <span class="pill pill-ok">Sent</span>
                                        <button type="button" class="btn btn-sm btn-ghost" title="Resend"><%= Icons.Get("send", "ico-sm") %></button>
                                    </li>
                                    <li>
                                        <span class="dot-icon" style="background:var(--primary-soft); color:var(--primary);"><%= Icons.Get("mail", "ico-sm") %></span>
                                        <div class="grow"><div class="cell-main">Ready for pick-up email</div><div class="cell-sub"><%= ReadyEmailSent ? "Sep 21, 1:35 PM" : "Sent when the load passes inspection" %></div></div>
                                        <% if (ReadyEmailSent) { %>
                                        <span class="pill pill-ok">Sent</span>
                                        <button type="button" class="btn btn-sm btn-ghost" title="Resend"><%= Icons.Get("send", "ico-sm") %></button>
                                        <% } else { %>
                                        <span class="pill st-claimed">Not yet</span>
                                        <% } %>
                                    </li>
                                </ul>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
