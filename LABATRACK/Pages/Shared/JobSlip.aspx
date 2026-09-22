<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="JobSlip.aspx.cs" Inherits="LABATRACK.Pages.Shared.JobSlip" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Slip LT-260921-004 · LabaTrack</title>
    <style>
        .toolbar {
            max-width: 560px; margin: 24px auto 0; padding: 0 16px;
            display: flex; justify-content: space-between; gap: 8px; flex-wrap: wrap;
        }
        .toolbar div { display: flex; gap: 8px; }

        /* The slip is sized like 80 mm thermal paper */
        .paper {
            width: 340px; max-width: calc(100% - 32px); margin: 20px auto 40px;
            background: #fff; color: #111; padding: 22px 20px;
            border-radius: 4px; box-shadow: 0 2px 10px rgba(20, 35, 60, .10);
            font-size: 12.5px; line-height: 1.45;
        }
        .c { text-align: center; }
        .shop { font-size: 16px; font-weight: 700; letter-spacing: -.01em; }
        .doc-title { margin-top: 6px; font-size: 11px; font-weight: 700; letter-spacing: .14em; text-transform: uppercase; }
        .claim {
            margin: 14px 0; padding: 10px; border: 1.5px solid #111; border-radius: 6px; text-align: center;
        }
        .claim small { display: block; font-size: 10px; letter-spacing: .12em; text-transform: uppercase; }
        .claim b { font-family: Consolas, 'Cascadia Mono', monospace; font-size: 22px; letter-spacing: .04em; }
        .rule { border: 0; border-top: 1px dashed #999; margin: 12px 0; }
        .row { display: flex; justify-content: space-between; gap: 10px; }
        .row span:last-child { text-align: right; font-variant-numeric: tabular-nums; }
        .lbl { color: #555; }
        .item-sub { color: #555; font-size: 11.5px; }
        .grand { font-size: 15px; font-weight: 700; }
        .bal { font-size: 14px; font-weight: 700; }
        .status { display: inline-block; border: 1px solid #111; border-radius: 3px; padding: 0 6px; font-size: 10.5px; font-weight: 700; letter-spacing: .08em; }
        .fine { font-size: 11px; color: #444; }
        .cut { display: flex; align-items: center; gap: 6px; color: #888; margin: 18px -8px; font-size: 10.5px; }
        .cut::before, .cut::after { content: ''; flex: 1; border-top: 1px dashed #aaa; }
        .tag-claim { font-family: Consolas, 'Cascadia Mono', monospace; font-size: 26px; font-weight: 700; letter-spacing: .03em; }
        .rush { display: inline-block; background: #111; color: #fff; font-weight: 700; padding: 1px 8px; border-radius: 3px; letter-spacing: .08em; }

        @media print {
            @page { margin: 0; }
            body { background: #fff; }
            .toolbar { display: none; }
            .paper { width: 72mm; max-width: none; margin: 0; padding: 4mm 3mm; box-shadow: none; border-radius: 0; font-size: 11px; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <%-- DRAFT: sample slip. Phase 3 fills it from the saved job, its add-ons and its payments. --%>
        <div class="toolbar">
            <a class="btn btn-ghost" href="JobDetail.aspx"><%= Icons.Get("arrow-left", "ico-sm") %>Back to job</a>
            <div>
                <a class="btn" href="NewJob.aspx"><%= Icons.Get("plus", "ico-sm") %>New job</a>
                <button type="button" class="btn btn-primary" onclick="window.print();"><%= Icons.Get("printer", "ico-sm") %>Print</button>
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
</body>
</html>
