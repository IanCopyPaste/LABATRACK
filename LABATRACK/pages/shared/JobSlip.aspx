<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobSlip.aspx.cs" Inherits="LABATRACK.pages.shared.JobSlip" %>

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
            <div class="row"><span><%: Job.ServiceName %></span><span><%: Money(Job.LaundryCharge) %></span></div>
            <div class="item-sub"><%: Kg(Job.WeightKg) %> kg, billed <%: Kg(Job.BillableKg) %> kg &times; <%: Money(Job.RatePerKgAtCreation) %><%: MinimumApplied ? " (minimum " + Money(Job.MinimumChargeAtCreation) + ")" : "" %></div>
            <asp:Repeater ID="rptAddOns" runat="server">
                <ItemTemplate>
                    <div class="row" style="margin-top:4px;"><span><%#: Eval("Name") %><%# (bool)Eval("IsProduct") ? " &times; " + Eval("Quantity") : "" %></span><span><%# Money(LineAmount((LABATRACK.models.JobAddOn)Container.DataItem)) %></span></div>
                    <%# (bool)Eval("IsProduct") ? "<div class=\"item-sub\">" + Eval("Quantity") + " &times; " + Money((decimal)Eval("PriceAtCreation")) + "</div>" : "" %>
                </ItemTemplate>
            </asp:Repeater>

            <hr class="rule" />
            <div class="row grand"><span>TOTAL</span><span>₱<%: Money(Job.TotalAmount) %></span></div>
            <% if (PaidInOneGo && DropOff.Method == "Cash") { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">Cash received</span><span><%: Money(DropOff.CashReceived ?? DropOff.Amount) %></span></div>
            <div class="row bal" style="margin-top:4px;"><span>CHANGE</span><span>₱<%: Money(DropOff.Change) %></span></div>
            <% } else if (PaidInOneGo) { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">E-wallet</span><span><%: Money(DropOff.Amount) %></span></div>
            <% } else { %>
            <asp:Repeater ID="rptPayments" runat="server">
                <ItemTemplate>
                    <div class="row" style="margin-top:4px;"><span class="lbl"><%#: PaymentLabel((LABATRACK.models.Payment)Container.DataItem) %></span><span><%# SignedMoney((decimal)Eval("Amount")) %></span></div>
                    <%# Eval("CashReceived") == null ? "" : "<div class=\"item-sub\">Cash received " + Money((decimal)Eval("CashReceived")) + " &middot; change " + Money(((LABATRACK.models.Payment)Container.DataItem).Change) + "</div>" %>
                </ItemTemplate>
            </asp:Repeater>
            <% if (NothingPaid) { %><div class="row" style="margin-top:4px;"><span class="lbl">Paid at drop-off</span><span>0.00</span></div><% } %>
            <% if (Job.Balance > 0) { %><div class="row bal" style="margin-top:4px;"><span>BALANCE DUE</span><span>₱<%: Money(Job.Balance) %></span></div>
            <div class="item-sub">To be paid before the laundry is released.</div><% } %>
            <% if (Job.RefundPending) { %><div class="row bal" style="margin-top:4px;"><span>REFUND WAITING</span><span>₱<%: Money(Job.RefundAmount) %></span></div>
            <div class="item-sub">Given back once, to the customer who gives the refund code.</div><% } %>
            <% } %>
            <div style="margin-top:6px;"><span class="status"><%: Stamp %></span> <span class="lbl" style="font-size:11px;">&nbsp;<%: DropOff != null ? DropOff.Method + " · " + DropOff.PaidAt.ToString("MMM d h:mm tt") : "" %></span></div>

            <% if (!string.IsNullOrEmpty(Job.Remarks)) { %>
            <hr class="rule" />
            <div class="fine"><strong>Remarks:</strong> <%: Job.Remarks %></div>
            <% } %>

            <hr class="rule" />
            <div class="c fine">
                <% if (!string.IsNullOrEmpty(Shop.SlipHeader)) { %><%: Shop.SlipHeader %><br /><% } %>
                <span style="color:#777;">This is not an official receipt.</span>
            </div>

            <div class="cut"><%= Icons.Get("scissors", "ico-sm") %> cut here</div>

            <div class="c">
                <div class="doc-title" style="margin-top:0;">Bag tag &middot; shop copy</div>
                <div class="tag-claim" style="margin-top:6px;"><%: Job.ClaimNumber %></div>
                <div style="font-size:14px; font-weight:600;"><%: Job.CustomerName %></div>
                <div><%: Job.ServiceName %> &middot; <%: Kg(Job.WeightKg) %> kg</div>
                <% if (!string.IsNullOrEmpty(Job.AddOnSummary)) { %><div style="margin-top:6px;"><%: Job.AddOnSummary.Replace(", ", " · ") %></div><% } %>
                <div class="fine" style="margin-top:6px;">Ready by <%: Job.ExpectedPickupAt.ToString("MMM d, h:mm tt") %> &middot; <%: IsVoided ? "Voided" : Job.Balance > 0 ? "₱" + Money(Job.Balance) + " due" : "Paid" %></div>
            </div>
        </div>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Print.js") %>"></script>
</body>
</html>
