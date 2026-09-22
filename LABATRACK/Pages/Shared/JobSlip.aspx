<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobSlip.aspx.cs" Inherits="LABATRACK.Pages.Shared.JobSlip" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Slip LT-260921-004 · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <%-- DRAFT: sample slip. Phase 3 fills it from the saved job, its add-ons and its payments. --%>
        <div class="toolbar">
            <a class="btn btn-ghost" href="JobDetail.aspx"><%= Icons.Get("arrow-left", "ico-sm") %>Back to job</a>
            <div>
                <a class="btn" href="NewJob.aspx"><%= Icons.Get("plus", "ico-sm") %>New job</a>
                <button type="button" class="btn btn-primary" data-print="true"><%= Icons.Get("printer", "ico-sm") %>Print</button>
            </div>
        </div>

        <div class="paper">
            <!-- Customer copy -->
            <div class="c">
                <div class="shop">LabaTrack Laundry Shop</div>
                <div class="doc-title">Job Order Slip</div>
            </div>

            <div class="claim">
                <small>Claim number</small>
                <b>LT-260921-004</b>
            </div>

            <div class="row"><span class="lbl">Dropped off</span><span>Sep 21, 2026 9:12 AM</span></div>
            <div class="row"><span class="lbl">Ready by</span><span><strong>Sep 21, 2026 5:00 PM</strong></span></div>
            <div class="row"><span class="lbl">Received by</span><span>jcruz</span></div>

            <hr class="rule" />
            <div class="row"><span class="lbl">Customer</span><span>Maria Santos</span></div>
            <div class="row"><span class="lbl">Contact</span><span>0917 555 0123</span></div>

            <hr class="rule" />
            <div class="row"><span>Wash-Dry-Fold</span><span>240.00</span></div>
            <div class="item-sub">5.1 kg, billed 6 kg &times; 40.00</div>
            <div class="row" style="margin-top:4px;"><span>Fabric conditioner</span><span>15.00</span></div>
            <div class="row"><span>Rush (same day)</span><span>50.00</span></div>

            <hr class="rule" />
            <div class="row grand"><span>TOTAL</span><span>₱305.00</span></div>
            <% if (Method == "Cash") { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">Cash received</span><span><%= Money(CashReceived) %></span></div>
            <div class="row bal" style="margin-top:4px;"><span>CHANGE</span><span>₱<%= Money(Change) %></span></div>
            <% } else { %>
            <div class="row" style="margin-top:4px;"><span class="lbl">E-wallet</span><span><%= Money(Total) %></span></div>
            <% } %>
            <div style="margin-top:6px;"><span class="status">PAID IN FULL</span> <span class="lbl" style="font-size:11px;">&nbsp;<%: Method %> &middot; Sep 21 9:12 AM</span></div>

            <hr class="rule" />
            <div class="fine"><strong>Remarks:</strong> 1 blouse with a loose button. No stains noted.</div>

            <hr class="rule" />
            <div class="c fine">
                Salamat po! Please present this slip when claiming your laundry.<br />
                <span style="color:#777;">This is not an official receipt.</span>
            </div>

            <!-- Shop copy: torn off and tied to the laundry bag -->
            <div class="cut"><%= Icons.Get("scissors", "ico-sm") %> cut here</div>

            <div class="c">
                <div class="doc-title" style="margin-top:0;">Bag tag &middot; shop copy</div>
                <div class="tag-claim" style="margin-top:6px;">LT-260921-004</div>
                <div style="font-size:14px; font-weight:600;">Maria Santos</div>
                <div>Wash-Dry-Fold &middot; 5.1 kg</div>
                <div style="margin-top:6px;">Fabric conditioner &nbsp;<span class="rush">RUSH</span></div>
                <div class="fine" style="margin-top:6px;">Ready by Sep 21, 5:00 PM &middot; Paid</div>
            </div>
        </div>
    </form>

    <script src="../../Assets/js/Print.js"></script>
</body>
</html>
