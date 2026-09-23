<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Pricing.aspx.cs" Inherits="LABATRACK.Admin.Pricing" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Pricing · LabaTrack</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <asp:Literal ID="litNav" runat="server" />

            <main class="main">
                <header class="page-head">
                    <div>
                        <h1>Pricing</h1>
                        <p class="sub">Rates apply to new job orders only. Existing jobs keep the rate they were created with.</p>
                    </div>
                </header>

                <div class="content">
                    <%-- DRAFT: sample rates. The real values are an open decision (CLAUDE.md, Open decisions 1). --%>
                    <div class="grid grid-main-side">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Services</h2>
                                        <div class="sub">Charged per kilo</div>
                                    </div>
                                    <button type="button" class="btn btn-sm"><%= Icons.Get("plus", "ico-sm") %>Add service</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table">
                                        <thead>
                                            <tr><th>Service</th><th class="right">Rate per kg</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Wash-Dry-Fold</td>
                                                <td class="right num">₱40.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Wash-Dry</td>
                                                <td class="right num">₱35.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Comforter / Bulky</td>
                                                <td class="right num">₱60.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main muted">Wash Only</td>
                                                <td class="right num muted">₱25.00</td>
                                                <td><span class="pill st-claimed">Inactive</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </section>

                            <%-- Add-ons are kept in two lists because they are charged differently.
                                 A service is work done to the load and is charged once. A product is a
                                 thing sold, so the job records how many and bills quantity x unit price. --%>
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Service add-ons</h2>
                                        <div class="sub">Extra work on the load. Charged once, whatever the weight.</div>
                                    </div>
                                    <button type="button" class="btn btn-sm"><%= Icons.Get("plus", "ico-sm") %>Add service</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table">
                                        <thead>
                                            <tr><th>Service add-on</th><th class="right">Price</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Extra rinse</td>
                                                <td class="right num">₱20.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Stain treatment</td>
                                                <td class="right num">₱30.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Product add-ons</h2>
                                        <div class="sub">Items sold with the load. Priced per piece and billed by quantity.</div>
                                    </div>
                                    <button type="button" class="btn btn-sm"><%= Icons.Get("plus", "ico-sm") %>Add product</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table">
                                        <thead>
                                            <tr><th>Product add-on</th><th class="right">Price each</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Fabric conditioner</td>
                                                <td class="right num">₱15.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Detergent sachet</td>
                                                <td class="right num">₱12.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                                <div class="card-f">
                                    <span class="hint">Quantity is recorded on the job, not tracked as stock. The shop does not count inventory here.</span>
                                </div>
                            </section>
                        </div>

                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Pricing rules</h2>
                                        <div class="sub">Used by every new job order</div>
                                    </div>
                                </div>
                                <div class="card-b form">
                                    <div class="field">
                                        <label for="txtMinimumCharge">Minimum laundry charge</label>
                                        <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtMinimumCharge" runat="server" Text="120.00" /></div>
                                        <span class="hint">A laundry charge below this is raised to it. Add-ons are added after.</span>
                                    </div>
                                    <div class="field">
                                        <label for="ddlRounding">Round weight up to the next</label>
                                        <asp:DropDownList ID="ddlRounding" runat="server">
                                            <asp:ListItem Value="0.5">0.5 kg</asp:ListItem>
                                            <asp:ListItem Value="1" Selected="True">1 kg</asp:ListItem>
                                        </asp:DropDownList>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="button" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save rules</button>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>How a total is computed</h2>
                                        <div class="sub">Example: Wash-Dry-Fold, 5.1 kg, one service and one product</div>
                                    </div>
                                </div>
                                <div class="card-b">
                                    <dl class="dl">
                                        <dt>Weight</dt><dd>5.1 kg</dd>
                                        <dt>Billable (rounded up)</dt><dd>6 kg</dd>
                                        <dt>6 kg &times; ₱40.00</dt><dd>₱240.00</dd>
                                        <dt>Minimum charge check</dt><dd class="muted">₱240.00 &ge; ₱120.00</dd>
                                        <dt>Extra rinse</dt><dd>₱20.00</dd>
                                        <dt>Fabric conditioner &times; 2</dt><dd>₱30.00</dd>
                                    </dl>
                                    <div class="divider" style="margin:14px 0;"></div>
                                    <div class="total-line"><span>Total</span><span class="num">₱290.00</span></div>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
