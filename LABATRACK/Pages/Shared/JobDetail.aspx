<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobDetail.aspx.cs" Inherits="LABATRACK.Pages.Shared.JobDetail" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Job · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <a class="small" href="<%: BackUrl %>"><%= Icons.Get("arrow-left", "ico-sm") %> <%: BackText %></a>
                        <h1 style="margin-top:8px;"><span class="mono" style="font-size:20px;"><%: Job.ClaimNumber %></span>
                            <span class="pill <%= StageCss(Job.CurrentStatus) %>" style="margin-left:8px; vertical-align:middle;"><%: Job.CurrentStatus %></span></h1>
                        <p class="sub">Dropped off <%: Job.CreatedAt.ToString("MMM d, yyyy h:mm tt") %> by <%: Job.CreatedByName %> &middot; expected pick-up <%: Job.ExpectedPickupAt.ToString("MMM d, h:mm tt") %><%: Job.LastEditedAt.HasValue ? " · corrected by " + Job.LastEditedByName + " " + Job.LastEditedAt.Value.ToString("MMM d, h:mm tt") : "" %></p>
                    </div>
                    <div class="page-actions">
                        <a class="btn" href="JobSlip.aspx?id=<%= Job.JobId %>"><%= Icons.Get("printer", "ico-sm") %>Print slip</a>
                        <%-- Only the owner may edit a job order; staff fix a mistake by voiding a Queued job instead. --%>
                        <% if (IsOwner && InShop) { %><a class="btn" href="../../Admin/EditJob.aspx?id=<%= Job.JobId %>"><%= Icons.Get("pencil", "ico-sm") %>Edit <span class="muted small">(owner)</span></a><% } %>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />
                    <%-- The status the page was showing. Every action sends it back, so a job someone else
                         moved in the meantime is refused ("This job was already updated. Refresh."). --%>
                    <asp:HiddenField ID="hfShownStatus" runat="server" />

                    <!-- Header: customer and load, shown once -->
                    <section class="card">
                        <div class="card-b grid grid-3" style="gap:24px;">
                            <div>
                                <div class="label muted">Customer</div>
                                <div class="strong" style="font-size:16px; margin-top:4px;"><%: Job.CustomerName %></div>
                                <div class="muted small"><%= Icons.Get("phone", "ico-sm") %> <%: Job.ContactNumber %> &nbsp; <%= Icons.Get("mail", "ico-sm") %> <%: string.IsNullOrEmpty(Job.CustomerEmail) ? "no email" : Job.CustomerEmail %></div>
                            </div>
                            <div>
                                <div class="label muted">Load</div>
                                <div class="strong" style="font-size:16px; margin-top:4px;"><%: Job.ServiceName %> &middot; <%: Kg(Job.WeightKg) %> kg</div>
                                <div class="muted small">Billed as <%: Kg(Job.BillableKg) %> kg at <%: Peso(Job.RatePerKgAtCreation) %>/kg<%: string.IsNullOrEmpty(Job.AddOnSummary) ? "" : " · " + Job.AddOnSummary %></div>
                            </div>
                            <div>
                                <div class="label muted">Remarks (intake check)</div>
                                <div style="margin-top:4px;"><%: string.IsNullOrEmpty(Job.Remarks) ? "None" : Job.Remarks %></div>
                            </div>
                        </div>
                    </section>

                    <!-- Stage stepper, read from JobStatusHistory: only the stages on this job's route -->
                    <section class="card <%= IsVoided ? "voided-banner" : "" %>">
                        <% if (IsVoided) { %>
                        <div class="card-b" style="display:flex; gap:12px; align-items:center; color:var(--danger);">
                            <%= Icons.Get("ban", "ico-lg") %>
                            <div>
                                <div class="strong">Voided <%: Job.StatusSince.ToString("MMM d, h:mm tt") %> by <%: Job.StatusBy %></div>
                                <div class="small">Reason: <%: Job.StatusReason %>. <%: RefundNote %></div>
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
                            <asp:Panel ID="pnlQueued" runat="server" CssClass="card" Visible="false">
                                <div class="card-h"><div><h2>Queued</h2><div class="sub">Waiting for a free machine</div></div></div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Put the load in <%: NextStage == "Washing" ? "a washer" : "for " + (NextStage ?? "").ToLowerInvariant() %>, then record it here.</span>
                                    <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Move to <%: NextStage %>"><%= Icons.Get("arrow-right", "ico-sm") %>Move to <%: NextStage %></button>
                                </div>
                                <div class="card-f" style="justify-content:space-between; flex-wrap:wrap; align-items:flex-end;" data-enter-scope="true">
                                    <div class="field" style="flex:1; min-width:220px;">
                                        <label for="txtVoidReason">Void this job (only while Queued). The full <%: Peso(Job.AmountPaid) %> is refunded.</label>
                                        <asp:TextBox ID="txtVoidReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                    </div>
                                    <button type="submit" name="action" value="void" class="btn btn-danger" data-confirm-title="Void <%: Job.ClaimNumber %>?" data-confirm-text="<%: VoidText %>" data-confirm-ok="Void and refund" data-confirm-danger="true"><%= Icons.Get("ban", "ico-sm") %>Void job</button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlAdvance" runat="server" CssClass="card" Visible="false">
                                <div class="card-h"><div><h2><%: Job.CurrentStatus %> in progress</h2><div class="sub">Advance only when this step is finished</div></div></div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Next step: <strong><%: NextStage %></strong></span>
                                    <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Move to <%: NextStage %>"><%= Icons.Get("arrow-right", "ico-sm") %>Move to <%: NextStage %></button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlInspection" runat="server" CssClass="card" Visible="false">
                                <div class="card-h"><div><h2>Inspection</h2><div class="sub">Check the finished load before shelving it</div></div></div>
                                <div class="card-b grid grid-2" style="align-items:start;">
                                    <div class="stack" style="gap:10px;">
                                        <div class="strong" style="color:var(--success);"><%= Icons.Get("circle-check", "ico-sm") %> Pass</div>
                                        <span class="muted small">Moves the load to Ready for pick-up and sends the ready email.</span>
                                        <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Pass inspection"><%= Icons.Get("check", "ico-sm") %>Pass inspection</button>
                                    </div>
                                    <div class="stack" style="gap:10px;" data-enter-scope="true">
                                        <div class="strong" style="color:var(--warning);"><%= Icons.Get("rotate-ccw", "ico-sm") %> Fail, send back</div>
                                        <%-- Filled in the code-behind with only the work stages on this job's route. --%>
                                        <asp:DropDownList ID="ddlReworkStage" runat="server" />
                                        <span class="hint">Only the stages this job's service includes are offered.</span>
                                        <asp:TextBox ID="txtReworkReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                        <%-- JobDetail.js writes the chosen stage and the reason into the question. --%>
                                        <button type="submit" name="action" value="rework" class="btn" id="btnRework" data-confirm-title="Send <%: Job.ClaimNumber %> back?" data-confirm-text="The load goes back to be redone, then through inspection again." data-confirm-ok="Send back"><%= Icons.Get("rotate-ccw", "ico-sm") %>Send back</button>
                                    </div>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlReady" runat="server" CssClass="card" Visible="false">
                                <div class="card-h">
                                    <div><h2>Release to customer</h2><div class="sub">Already paid in full at drop-off</div></div>
                                    <span class="pill pill-ok">Paid</span>
                                </div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Check the customer's slip matches claim number <strong class="mono"><%: Job.ClaimNumber %></strong>, then hand over the load.</span>
                                    <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Mark as claimed"><%= Icons.Get("package-check", "ico-sm") %>Mark as claimed</button>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlClosed" runat="server" CssClass="card" Visible="false">
                                <div class="card-b" style="display:flex; gap:12px; align-items:center;">
                                    <%= Icons.Get(IsVoided ? "circle-x" : "package-check", "ico-lg") %>
                                    <div>
                                        <div class="strong"><%: IsVoided ? "This job was voided." : "Claimed by the customer " + Job.StatusSince.ToString("MMM d, h:mm tt") + " (handed over by " + Job.StatusBy + ")." %></div>
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
                                                        <td class="num"><%# When((DateTime)Eval("ChangedAt")) %></td>
                                                        <td>
                                                            <%# string.IsNullOrEmpty((string)Eval("FromStatus")) ? "<span class=\"muted\">Created</span>" : "<span class=\"pill " + StageCss((string)Eval("FromStatus")) + "\">" + Server.HtmlEncode((string)Eval("FromStatus")) + "</span>" %>
                                                            <%# Icons.Get("arrow-right", "ico-sm") %>
                                                            <span class="pill <%# StageCss((string)Eval("ToStatus")) %>"><%#: Eval("ToStatus") %></span>
                                                        </td>
                                                        <td><%#: Eval("ChangedBy") %></td>
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
                                        <dt><%: Kg(Job.BillableKg) %> kg &times; <%: Peso(Job.RatePerKgAtCreation) %><%: MinimumApplied ? " (raised to minimum)" : "" %></dt><dd><%: Peso(Job.LaundryCharge) %></dd>
                                        <asp:Repeater ID="rptLines" runat="server">
                                            <ItemTemplate>
                                                <dt><%#: Eval("Name") %><%# (bool)Eval("IsProduct") ? " &times; " + Eval("Quantity") : "" %></dt><dd><%# Peso(LineAmount((LABATRACK.Models.JobAddOn)Container.DataItem)) %></dd>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </dl>
                                    <div class="divider" style="margin:12px 0;"></div>
                                    <div class="total-line" style="font-size:15px;"><span>Total</span><span class="num"><%: Peso(Job.TotalAmount) %></span></div>
                                </div>
                                <ul class="list" style="border-top:1px solid var(--border);">
                                    <asp:Repeater ID="rptPayments" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="background:<%# (bool)Eval("IsRefund") ? "var(--danger-soft); color:var(--danger)" : "var(--success-soft); color:var(--success)" %>;"><%# Icons.Get((string)Eval("Method") == "Cash" ? "banknote" : "smartphone", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: PaymentLabel((LABATRACK.Models.Payment)Container.DataItem) %></div>
                                                    <div class="cell-sub"><%# When((DateTime)Eval("PaidAt")) %> &middot; <%#: Eval("RecordedBy") %></div>
                                                </div>
                                                <span class="num strong"><%# Peso((decimal)Eval("Amount")) %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </ul>
                                <% if (CashReceived.HasValue) { %>
                                <div class="card-f" style="flex-direction:column; gap:4px;">
                                    <div class="row-between"><span class="muted">Cash received</span><span class="num"><%: Peso(CashReceived.Value) %></span></div>
                                    <div class="row-between"><span class="muted">Change given</span><span class="num"><%: Peso(Change) %></span></div>
                                </div>
                                <% } %>
                            </section>

                            <!-- Email log with resend -->
                            <section class="card">
                                <div class="card-h"><div><h2>Notifications</h2><div class="sub">Email only. Every attempt is logged, and a failed send never blocks the job.</div></div></div>
                                <ul class="list">
                                    <asp:Repeater ID="rptEmails" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="<%# EmailDotStyle((string)Eval("Status")) %>"><%# Icons.Get("mail", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("Kind") %> email<%# (bool)Eval("IsResend") ? ", resent" : "" %></div>
                                                    <div class="cell-sub"><%# When((DateTime)Eval("AttemptedAt")) %> &middot; <%#: EmailDetail((LABATRACK.Models.NotificationLogEntry)Container.DataItem) %></div>
                                                </div>
                                                <span class="pill <%# EmailPill((string)Eval("Status")) %>"><%#: Eval("Status") %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <% if (!ReadyReached && InShop) { %>
                                    <li>
                                        <span class="dot-icon" style="background:var(--primary-soft); color:var(--primary);"><%= Icons.Get("mail", "ico-sm") %></span>
                                        <div class="grow"><div class="cell-main">Ready email</div><div class="cell-sub">Sent when the load passes inspection</div></div>
                                        <span class="pill st-claimed">Not yet</span>
                                    </li>
                                    <% } %>
                                </ul>
                                <%-- A resend only makes sense while the load is still in the shop. --%>
                                <% if (InShop) { %>
                                <div class="card-f">
                                    <button type="submit" name="action" value="resend-dropoff" class="btn btn-sm btn-ghost"><%= Icons.Get("send", "ico-sm") %>Resend drop-off email</button>
                                    <% if (ReadyReached) { %><button type="submit" name="action" value="resend-ready" class="btn btn-sm btn-ghost"><%= Icons.Get("send", "ico-sm") %>Resend ready email</button><% } %>
                                </div>
                                <% } %>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/Assets/js/Confirm.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/Assets/js/JobDetail.js") %>"></script>
</body>
</html>
