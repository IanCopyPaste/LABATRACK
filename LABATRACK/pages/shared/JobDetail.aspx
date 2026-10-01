<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobDetail.aspx.cs" Inherits="LABATRACK.pages.shared.JobDetail" %>

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
                        <% if (IsOwner && InShop) { %><a class="btn" href="../../admin/EditJob.aspx?id=<%= Job.JobId %>"><%= Icons.Get("pencil", "ico-sm") %>Edit <span class="muted small">(owner)</span></a><% } %>
                    </div>
                </header>

                <div class="content">
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />
                    <asp:HiddenField ID="hfShownStatus" runat="server" />
                    <asp:HiddenField ID="hfShownBalance" runat="server" />

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
                            <asp:Panel ID="pnlQueued" runat="server" CssClass="card" Visible="false">
                                <div class="card-h"><div><h2>Queued</h2><div class="sub">Waiting for a free machine</div></div></div>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Put the load in <%: NextStage == "Washing" ? "a washer" : "for " + (NextStage ?? "").ToLowerInvariant() %>, then record it here.</span>
                                    <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Move to <%: NextStage %>"><%= Icons.Get("arrow-right", "ico-sm") %>Move to <%: NextStage %></button>
                                </div>
                                <div class="card-f" style="justify-content:space-between; flex-wrap:wrap; align-items:flex-end;" data-enter-scope="true">
                                    <div class="field" style="flex:1; min-width:220px;">
                                        <label for="txtVoidReason">Void this job (only while Queued). <%: Job.AmountPaid > 0 ? "The " + Peso(Job.AmountPaid) + " paid is refunded." : "Nothing was paid, so nothing is refunded." %></label>
                                        <asp:TextBox ID="txtVoidReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                        <% if (Job.AmountPaid > 0) { %>
                                        <label class="check" style="margin-top:8px;"><input type="checkbox" id="chkRefundNow" name="voidNow" value="1"<%= VoidNowChecked ? " checked=\"checked\"" : "" %> />The customer is here: give the <%: Peso(Job.AmountPaid) %> refund back now</label>
                                        <span class="hint">Untick it if they are not at the counter. The refund then waits for them, and they are emailed a refund code to collect it with.</span>
                                        <% } %>
                                    </div>
                                    <button type="submit" name="action" value="void" class="btn btn-danger" id="btnVoid" data-confirm-title="Void <%: Job.ClaimNumber %>?" data-confirm-text="<%: VoidText %>"
                                            data-text-now="<%: VoidTextNow %>" data-text-later="<%: VoidTextLater %>" data-confirm-ok="Void job" data-confirm-danger="true"><%= Icons.Get("ban", "ico-sm") %>Void job</button>
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
                                        <asp:DropDownList ID="ddlReworkStage" runat="server" />
                                        <span class="hint">Only the stages this job's service includes are offered.</span>
                                        <asp:TextBox ID="txtReworkReason" runat="server" placeholder="Reason (required)" MaxLength="200" />
                                        <button type="submit" name="action" value="rework" class="btn" id="btnRework" data-confirm-title="Send <%: Job.ClaimNumber %> back?" data-confirm-text="The load goes back to be redone, then through inspection again." data-confirm-ok="Send back"><%= Icons.Get("rotate-ccw", "ico-sm") %>Send back</button>
                                    </div>
                                </div>
                            </asp:Panel>

                            <asp:Panel ID="pnlReady" runat="server" CssClass="card" Visible="false">
                                <div class="card-h">
                                    <div><h2>Release to customer</h2><div class="sub"><%: HasBalance ? "Collect the balance, then hand over the load" : "Paid in full" %></div></div>
                                    <span class="pill <%= HasBalance ? "pill-warn" : "pill-ok" %>"><%: HasBalance ? Peso(Job.Balance) + " due" : "Paid" %></span>
                                </div>
                                <% if (!HasBalance) { %>
                                <div class="card-b" style="display:flex; justify-content:space-between; align-items:center; gap:12px; flex-wrap:wrap;">
                                    <span>Check the customer's slip matches claim number <strong class="mono"><%: Job.ClaimNumber %></strong>, then hand over the load.</span>
                                    <button type="submit" name="action" value="advance" class="btn btn-primary" data-confirm-title="<%: MoveTitle %>" data-confirm-text="<%: MoveText %>" data-confirm-ok="Mark as claimed"><%= Icons.Get("package-check", "ico-sm") %>Mark as claimed</button>
                                </div>
                                <% } else { %>
                                <div class="card-b form pay-form" data-pay-form="true" data-balance="<%: Money(Job.Balance) %>" data-enter-scope="true"
                                     data-what="claim" data-customer="<%: Job.CustomerName %> · <%: Job.ClaimNumber %>">
                                    <span>Check the customer's slip matches claim number <strong class="mono"><%: Job.ClaimNumber %></strong>. They still owe <strong><%: Peso(Job.Balance) %></strong>: take it, then hand over the load.</span>
                                    <div class="form-row">
                                        <div class="field">
                                            <span class="label">Method</span>
                                            <div class="method">
                                                <label class="check"><input type="radio" name="claimMethod" value="Cash" data-pay-method="true"<%= IsPosted("claimMethod", "E-wallet") ? "" : " checked=\"checked\"" %> /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                                <label class="check"><input type="radio" name="claimMethod" value="E-wallet" data-pay-method="true"<%= IsPosted("claimMethod", "E-wallet") ? " checked=\"checked\"" : "" %> /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                            </div>
                                        </div>
                                        <div class="field" data-cash-field="true">
                                            <label for="claimCashReceived">Cash received</label>
                                            <div class="input-prefix"><span>₱</span><input type="text" id="claimCashReceived" name="claimCashReceived" inputmode="decimal" autocomplete="off" data-pay-cash="true" value="<%: Posted("claimCashReceived", "") %>" /></div>
                                            <div class="quick-cash">
                                                <button type="button" class="btn btn-sm" data-cash-chip="exact">Exact</button>
                                                <button type="button" class="btn btn-sm" data-cash-chip="100">₱100</button>
                                                <button type="button" class="btn btn-sm" data-cash-chip="200">₱200</button>
                                                <button type="button" class="btn btn-sm" data-cash-chip="500">₱500</button>
                                                <button type="button" class="btn btn-sm" data-cash-chip="1000">₱1,000</button>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="change-box idle" data-change-box="true"><span class="small" data-change-label="true">Change</span><span class="change-amount num" data-change-amount="true">Enter cash received</span></div>
                                    <button type="submit" name="action" value="claim" class="btn btn-primary" data-pay-submit="true"
                                            data-confirm-title="Hand <%: Job.ClaimNumber %> over to the customer?" data-confirm-ok="Collect and mark claimed"><%= Icons.Get("package-check", "ico-sm") %>Collect <%: Peso(Job.Balance) %> and mark claimed</button>
                                </div>
                                <% } %>
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

                            <% if (HasRefund && Job.RefundPending) { %>
                            <section class="card" id="refund" style="border-color:#e9d3a3;">
                                <div class="card-h">
                                    <div><h2>Refund waiting</h2><div class="sub"><%: Peso(Job.RefundAmount) %> is owed to the customer and has not been handed over</div></div>
                                    <span class="pill pill-warn"><%: Peso(Job.RefundAmount) %></span>
                                </div>
                                <div class="card-b form" data-enter-scope="true">
                                    <span>Hand it over only to the customer who gives the <strong>refund code</strong> that was emailed to them. It can be given <strong>once</strong>: after that this job's claim number and code are worthless.</span>
                                    <% if (RefundLocked) { %>
                                    <div class="notice notice-danger"><%= Icons.Get("circle-alert", "ico-sm") %><span>Locked after <%: MaxWrongCodes %> wrong codes. Only the owner can release this refund now.</span></div>
                                    <% } else { %>
                                    <div class="field">
                                        <label for="refundCode">Refund code</label>
                                        <input type="text" id="refundCode" name="refundCode" maxlength="12" autocomplete="off" autocapitalize="characters" spellcheck="false" placeholder="K7M-2QX" class="refund-code" />
                                        <span class="hint"><%: WrongCodesLeft %> <%: WrongCodesLeft == 1 ? "try" : "tries" %> left. Spaces and dashes don't matter.</span>
                                    </div>
                                    <button type="submit" name="action" value="release-refund" class="btn btn-primary"
                                            data-confirm-title="Give the <%: Peso(Job.RefundAmount) %> refund?" data-confirm-ok="Give the refund"
                                            data-confirm-text="<%: Job.CustomerName %> · <%: Job.ClaimNumber %>&#10;&#10;Hand <%: Peso(Job.RefundAmount) %> to the customer, the same way it was paid.&#10;It can be given only once, and it is recorded with your name."><%= Icons.Get("hand-coins", "ico-sm") %>Give the refund</button>
                                    <% } %>
                                    <div class="row-between" style="align-items:center; gap:12px;">
                                        <span class="hint">Lost the email? A new code can be sent<%: string.IsNullOrEmpty(Job.CustomerEmail) ? ", but this customer has no email address." : " to " + Job.CustomerEmail + ". The old code stops working once the new one is sent." %></span>
                                        <button type="submit" name="action" value="reissue-code" class="btn btn-sm btn-ghost"<%= string.IsNullOrEmpty(Job.CustomerEmail) ? " disabled=\"disabled\"" : "" %>><%= Icons.Get("send", "ico-sm") %>Send a new code</button>
                                    </div>
                                </div>
                                <% if (IsOwner) { %>
                                <div class="card-f" style="flex-direction:column; align-items:stretch; gap:8px;" data-enter-scope="true">
                                    <div class="strong small">Owner: release without the code</div>
                                    <span class="hint">For a customer with no email, a lost code, or a locked refund. Check who they are first. The reason is recorded with your name.</span>
                                    <div style="display:flex; gap:8px; align-items:center; flex-wrap:wrap;">
                                        <input type="text" id="overrideReason" name="overrideReason" maxlength="200" autocomplete="off" placeholder="Reason (required), e.g. ID checked, no email" style="flex:1; min-width:220px;" />
                                        <button type="submit" name="action" value="release-override" class="btn btn-danger"
                                                data-confirm-title="Give the refund without the code?" data-confirm-ok="Give the refund" data-confirm-danger="true"
                                                data-confirm-text="<%: Job.CustomerName %> · <%: Job.ClaimNumber %>&#10;&#10;Hand <%: Peso(Job.RefundAmount) %> to the customer without their refund code.&#10;It is recorded as an owner override with your name and reason."><%= Icons.Get("hand-coins", "ico-sm") %>Release</button>
                                    </div>
                                </div>
                                <% } %>
                            </section>
                            <% } else if (HasRefund) { %>
                            <section class="card" id="refund">
                                <div class="card-h">
                                    <div><h2>Refund</h2><div class="sub">Handed back <%: Job.RefundReleasedAt.HasValue ? Job.RefundReleasedAt.Value.ToString("MMM d, h:mm tt") : "" %><%: string.IsNullOrEmpty(Job.RefundReleasedBy) ? "" : " by " + Job.RefundReleasedBy %></div></div>
                                    <span class="pill pill-ok"><%: Peso(Job.RefundAmount) %> given back</span>
                                </div>
                                <div class="card-b">
                                    <span class="small muted"><%: RefundHow %> A refund is given only once, so this job can never be refunded again.</span>
                                </div>
                            </section>
                            <% } %>

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
                            <section class="card" id="pay">
                                <div class="card-h">
                                    <div><h2>Payment</h2><div class="sub">Price frozen when the job was created</div></div>
                                    <span class="pill <%= PaymentPill %>"><%: PaymentPillText %></span>
                                </div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt><%: Kg(Job.BillableKg) %> kg &times; <%: Peso(Job.RatePerKgAtCreation) %><%: MinimumApplied ? " (raised to minimum)" : "" %></dt><dd><%: Peso(Job.LaundryCharge) %></dd>
                                        <asp:Repeater ID="rptLines" runat="server">
                                            <ItemTemplate>
                                                <dt><%#: Eval("Name") %><%# (bool)Eval("IsProduct") ? " &times; " + Eval("Quantity") : "" %></dt><dd><%# Peso(LineAmount((LABATRACK.models.JobAddOn)Container.DataItem)) %></dd>
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
                                                    <div class="cell-main"><%#: PaymentLabel((LABATRACK.models.Payment)Container.DataItem) %></div>
                                                    <div class="cell-sub"><%# When((DateTime)Eval("PaidAt")) %> &middot; <%#: Eval("RecordedBy") %><%#: CashNote((LABATRACK.models.Payment)Container.DataItem) %></div>
                                                </div>
                                                <span class="num strong"><%# Peso((decimal)Eval("Amount")) %></span>
                                            </li>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                    <% if (NothingPaid) { %><li class="muted small">Nothing paid yet.</li><% } %>
                                </ul>
                                <% if (HasBalance) { %>
                                <div class="card-b pay-totals">
                                    <div class="row-between"><span class="muted">Paid so far</span><span class="num"><%: Peso(Job.AmountPaid) %></span></div>
                                    <div class="row-between strong balance-due"><span>Balance due</span><span class="num"><%: Peso(Job.Balance) %></span></div>
                                </div>
                                <% } %>
                                <% if (CanCollect) { %>
                                <div class="card-b form pay-form" data-pay-form="true" data-balance="<%: Money(Job.Balance) %>" data-enter-scope="true"
                                     data-what="pay" data-customer="<%: Job.CustomerName %> · <%: Job.ClaimNumber %>">
                                    <div>
                                        <div class="strong">Take a payment</div>
                                        <span class="hint">Part of the balance or all of it. Whatever is left is collected when the load is claimed.</span>
                                    </div>
                                    <div class="field">
                                        <label for="payAmount">Amount</label>
                                        <div class="input-prefix"><span>₱</span><input type="text" id="payAmount" name="payAmount" inputmode="decimal" autocomplete="off" data-pay-amount="true" value="<%: Posted("payAmount", Money(Job.Balance)) %>" /></div>
                                        <div class="quick-cash">
                                            <button type="button" class="btn btn-sm" data-amount-chip="full">Full balance <%: Peso(Job.Balance) %></button>
                                        </div>
                                    </div>
                                    <div class="field">
                                        <span class="label">Method</span>
                                        <div class="method">
                                            <label class="check"><input type="radio" name="payMethod" value="Cash" data-pay-method="true"<%= IsPosted("payMethod", "E-wallet") ? "" : " checked=\"checked\"" %> /><%= Icons.Get("banknote", "ico-sm") %>Cash</label>
                                            <label class="check"><input type="radio" name="payMethod" value="E-wallet" data-pay-method="true"<%= IsPosted("payMethod", "E-wallet") ? " checked=\"checked\"" : "" %> /><%= Icons.Get("smartphone", "ico-sm") %>E-wallet</label>
                                        </div>
                                    </div>
                                    <div class="field" data-cash-field="true">
                                        <label for="payCash">Cash received</label>
                                        <div class="input-prefix"><span>₱</span><input type="text" id="payCash" name="payCash" inputmode="decimal" autocomplete="off" data-pay-cash="true" value="<%: Posted("payCash", "") %>" /></div>
                                        <div class="quick-cash">
                                            <button type="button" class="btn btn-sm" data-cash-chip="exact">Exact</button>
                                            <button type="button" class="btn btn-sm" data-cash-chip="100">₱100</button>
                                            <button type="button" class="btn btn-sm" data-cash-chip="200">₱200</button>
                                            <button type="button" class="btn btn-sm" data-cash-chip="500">₱500</button>
                                            <button type="button" class="btn btn-sm" data-cash-chip="1000">₱1,000</button>
                                        </div>
                                    </div>
                                    <div class="change-box idle" data-change-box="true"><span class="small" data-change-label="true">Change</span><span class="change-amount num" data-change-amount="true">Enter cash received</span></div>
                                    <button type="submit" name="action" value="pay" class="btn btn-primary" data-pay-submit="true"
                                            data-confirm-title="Record this payment?" data-confirm-ok="Record payment"><%= Icons.Get("hand-coins", "ico-sm") %>Record payment</button>
                                </div>
                                <% } %>
                            </section>

                            <section class="card">
                                <div class="card-h"><div><h2>Notifications</h2><div class="sub">Email only: at drop-off, when the order is updated or cancelled, and when it is ready. Every attempt is logged, and a failed send never blocks the job.</div></div></div>
                                <ul class="list">
                                    <asp:Repeater ID="rptEmails" runat="server">
                                        <ItemTemplate>
                                            <li>
                                                <span class="dot-icon" style="<%# EmailDotStyle((string)Eval("Status")) %>"><%# Icons.Get("mail", "ico-sm") %></span>
                                                <div class="grow">
                                                    <div class="cell-main"><%#: Eval("Kind") %> email<%# (bool)Eval("IsResend") ? ", resent" : "" %></div>
                                                    <div class="cell-sub"><%# When((DateTime)Eval("AttemptedAt")) %> &middot; <%#: EmailDetail((LABATRACK.models.NotificationLogEntry)Container.DataItem) %></div>
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
                                <% if (InShop) { %>
                                <div class="card-f">
                                    <button type="submit" name="action" value="resend-dropoff" class="btn btn-sm btn-ghost"><%= Icons.Get("send", "ico-sm") %>Resend drop-off email</button>
                                    <% if (ReadyReached) { %><button type="submit" name="action" value="resend-ready" class="btn btn-sm btn-ghost"><%= Icons.Get("send", "ico-sm") %>Resend ready email</button><% } %>
                                </div>
                                <% } else if (IsVoided) { %>
                                <div class="card-f">
                                    <button type="submit" name="action" value="resend-voided" class="btn btn-sm btn-ghost"><%= Icons.Get("send", "ico-sm") %><%: Job.RefundPending ? "Resend cancellation email with a new refund code" : "Resend cancellation email" %></button>
                                </div>
                                <% } %>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/NumericInput.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Confirm.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/JobDetail.js") %>"></script>
</body>
</html>
