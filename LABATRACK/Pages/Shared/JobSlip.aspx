<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobSlip.aspx.cs" Inherits="LABATRACK.Pages.Shared.JobSlip" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Job slip · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="toolbar">
            <a class="btn btn-ghost" href="JobDetail.aspx?id=<%= Job.JobId %>"><%= Icons.Get("arrow-left", "ico-sm") %>Back to job</a>
            <div>
                <a class="btn" href="NewJob.aspx"><%= Icons.Get("plus", "ico-sm") %>New job</a>
                <button type="button" class="btn btn-primary" data-print="true"><%= Icons.Get("printer", "ico-sm") %>Print</button>
            </div>
        </div>

        <div class="paper">
            <!-- Customer copy -->
            <div class="c">
                <div class="shop"><%: Shop.ShopName %></div>
                <div class="doc-title">Job Order Slip</div>
            </div>

            <div class="claim">
                <small>Claim number</small>
                <b><%: Job.ClaimNumber %></b>
            </div>

            <div class="row"><span class="lbl">Dropped off</span><span><%: Job.CreatedAt.ToString("MMM d, yyyy h:mm tt") %></span></div>
            <div class="row"><span class="lbl">Ready by</span><span><strong><%: Job.ExpectedPickupAt.ToString("MMM d, yyyy h:mm tt") %></strong></span></div>
            <div class="row"><span class="lbl">Received by</span><span><%: Job.CreatedByName %></span></div>

            <hr class="rule" />
            <div class="row"><span class="lbl">Customer</span><span><%: Job.CustomerName %></span></div>
            <div class="row"><span class="lbl">Contact</span><span><%: Job.ContactNumber %></span></div>

            <hr class="rule" />
            <%-- Every amount below is the one frozen on the job when it was created, not today's price. --%>
            <div class="row"><span><%: Job.ServiceName %></span><span><%: Money(Job.LaundryCharge) %></span></div>
            <div class="item-sub"><%: Kg(Job.WeightKg) %> kg, billed <%: Kg(Job.BillableKg) %> kg &times; <%: Money(Job.RatePerKgAtCreation) %><%: MinimumApplied ? " (minimum " + Money(Job.MinimumChargeAtCreation) + ")" : "" %></div>
            <asp:Repeater ID="rptAddOns" runat="server">
                <ItemTemplate>
                    <div class="row" style="margin-top:4px;"><span><%#: Eval("Name") %><%# (bool)Eval("IsProduct") ? " &times; " + Eval("Quantity") : "" %></span><span><%# Money(LineAmount((LABATRACK.Models.JobAddOn)Container.DataItem)) %></span></div>
                    <%# (bool)Eval("IsProduct") ? "<div class=\"item-sub\">" + Eval("Quantity") + " &times; " + Money((decimal)Eval("PriceAtCreation")) + "</div>" : "" %>
                </ItemTemplate>
            </asp:Repeater>

            <hr class="rule" />
            <div class="row grand"><span>TOTAL</span><span>₱<%: Money(Job.TotalAmount) %></span></div>
            <% if (!HasLaterPayments && DropOff != null && DropOff.Method == "Cash") { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">Cash received</span><span><%: Money(DropOff.CashReceived ?? DropOff.Amount) %></span></div>
            <div class="row bal" style="margin-top:4px;"><span>CHANGE</span><span>₱<%: Money(Change) %></span></div>
            <% } else if (!HasLaterPayments && DropOff != null) { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">E-wallet</span><span><%: Money(DropOff.Amount) %></span></div>
            <% } else if (DropOff != null) { %>
            <%-- After a correction or a void, each payment is listed on its own line with its own change,
                 so the slip still adds up: drop-off + corrections = total (or 0 once voided). --%>
            <div class="row" style="margin-top:4px;"><span class="lbl">Paid at drop-off (<%: DropOff.Method %>)</span><span><%: Money(DropOff.Amount) %></span></div>
            <% if (DropOff.CashReceived.HasValue) { %><div class="item-sub">Cash received <%: Money(DropOff.CashReceived.Value) %> &middot; change <%: Money(Change) %></div><% } %>
            <% } %>
            <%-- An owner's correction after drop-off (the difference collected or refunded), or a void refund. --%>
            <asp:Repeater ID="rptLaterPayments" runat="server">
                <ItemTemplate>
                    <div class="row" style="margin-top:4px;"><span class="lbl"><%#: LaterLabel((LABATRACK.Models.Payment)Container.DataItem) %></span><span><%# SignedMoney((decimal)Eval("Amount")) %></span></div>
                    <%# Eval("CashReceived") == null ? "" : "<div class=\"item-sub\">Cash received " + Money((decimal)Eval("CashReceived")) + " &middot; change " + Money((decimal)Eval("CashReceived") - (decimal)Eval("Amount")) + "</div>" %>
                </ItemTemplate>
            </asp:Repeater>
            <div style="margin-top:6px;"><span class="status"><%: IsVoided ? "VOIDED · REFUNDED" : "PAID IN FULL" %></span> <span class="lbl" style="font-size:11px;">&nbsp;<%: DropOff != null ? DropOff.Method + " · " + DropOff.PaidAt.ToString("MMM d h:mm tt") : "" %></span></div>

            <% if (!string.IsNullOrEmpty(Job.Remarks)) { %>
            <hr class="rule" />
            <div class="fine"><strong>Remarks:</strong> <%: Job.Remarks %></div>
            <% } %>

            <hr class="rule" />
            <div class="c fine">
                <% if (!string.IsNullOrEmpty(Shop.SlipHeader)) { %><%: Shop.SlipHeader %><br /><% } %>
                <span style="color:#777;">This is not an official receipt.</span>
            </div>

            <!-- Shop copy: torn off and tied to the laundry bag -->
            <div class="cut"><%= Icons.Get("scissors", "ico-sm") %> cut here</div>

            <div class="c">
                <div class="doc-title" style="margin-top:0;">Bag tag &middot; shop copy</div>
                <div class="tag-claim" style="margin-top:6px;"><%: Job.ClaimNumber %></div>
                <div style="font-size:14px; font-weight:600;"><%: Job.CustomerName %></div>
                <div><%: Job.ServiceName %> &middot; <%: Kg(Job.WeightKg) %> kg</div>
                <% if (!string.IsNullOrEmpty(Job.AddOnSummary)) { %><div style="margin-top:6px;"><%: Job.AddOnSummary.Replace(", ", " · ") %></div><% } %>
                <div class="fine" style="margin-top:6px;">Ready by <%: Job.ExpectedPickupAt.ToString("MMM d, h:mm tt") %> &middot; <%: IsVoided ? "Voided" : "Paid" %></div>
            </div>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Print.js") %>"></script>
</body>
</html>
