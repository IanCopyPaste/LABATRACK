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
                                    <button type="button" class="btn btn-sm" data-price-add="service"><%= Icons.Get("plus", "ico-sm") %>Add service</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table" data-price-table="service">
                                        <thead>
                                            <tr><th>Service</th><th class="right">Rate per kg</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Wash-Dry-Fold</td>
                                                <td class="right num">₱40.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Wash-Dry</td>
                                                <td class="right num">₱35.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Comforter / Bulky</td>
                                                <td class="right num">₱60.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main muted">Wash Only</td>
                                                <td class="right num muted">₱25.00</td>
                                                <td><span class="pill st-claimed">Inactive</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
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
                                    <button type="button" class="btn btn-sm" data-price-add="service-addon"><%= Icons.Get("plus", "ico-sm") %>Add service add-on</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table" data-price-table="service-addon">
                                        <thead>
                                            <tr><th>Service add-on</th><th class="right">Price</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Extra rinse</td>
                                                <td class="right num">₱20.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Stain treatment</td>
                                                <td class="right num">₱30.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
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
                                    <button type="button" class="btn btn-sm" data-price-add="product"><%= Icons.Get("plus", "ico-sm") %>Add product</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table" data-price-table="product">
                                        <thead>
                                            <tr><th>Product add-on</th><th class="right">Price each</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <tr>
                                                <td class="cell-main">Fabric conditioner</td>
                                                <td class="right num">₱15.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                            </tr>
                                            <tr>
                                                <td class="cell-main">Detergent sachet</td>
                                                <td class="right num">₱12.00</td>
                                                <td><span class="pill pill-ok">Active</span></td>
                                                <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%= Icons.Get("pencil", "ico-sm") %>Edit</button></td>
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

        <%-- One edit window for all three lists (services, service add-ons, products). Pricing.js fills it
             from the row that was clicked and sets the labels for that kind of price. There is no delete:
             a price that is no longer offered is set to Inactive, so old jobs still point at it. --%>
        <dialog id="priceDialog" class="modal" aria-labelledby="priceDialogTitle">
            <div class="modal-h">
                <div>
                    <h2 id="priceDialogTitle">Edit price</h2>
                    <div class="sub" id="priceDialogSub"></div>
                </div>
                <button type="button" class="icon-btn" data-modal-close="true" aria-label="Close"><%= Icons.Get("x") %></button>
            </div>
            <div class="modal-b form">
                <div class="field">
                    <label for="pdName">Name</label>
                    <input type="text" id="pdName" maxlength="60" autocomplete="off" />
                    <span class="error-text" id="pdNameError" hidden></span>
                </div>
                <div class="field">
                    <label for="pdPrice" id="pdPriceLabel">Price</label>
                    <div class="input-prefix"><span>₱</span><input type="text" id="pdPrice" inputmode="decimal" autocomplete="off" /></div>
                    <span class="hint" id="pdPriceHint"></span>
                    <span class="error-text" id="pdPriceError" hidden></span>
                </div>
                <div class="field">
                    <span class="label">Status</span>
                    <label class="check"><input type="checkbox" id="pdActive" /><span><strong>Active</strong> <span class="muted small">&middot; offered on new job orders</span></span></label>
                    <span class="hint">Untick to stop offering it. It stays on every job that already used it.</span>
                </div>
                <div class="notice notice-info">
                    <%= Icons.Get("info", "ico-sm") %>
                    <span>A new price applies to new job orders only. Jobs already made keep the price they were created with.</span>
                </div>
            </div>
            <div class="modal-f">
                <span class="draft-tag"><%= Icons.Get("circle-alert", "ico-sm") %>Draft: updates this page only, nothing is saved yet</span>
                <button type="button" class="btn btn-ghost" data-modal-close="true">Cancel</button>
                <button type="button" class="btn btn-primary" id="pdSave"><%= Icons.Get("save", "ico-sm") %><span id="pdSaveText">Save changes</span></button>
            </div>
        </dialog>
    </form>

    <script src="<%= AssetUrl.Get("~/Assets/js/Pricing.js") %>"></script>
</body>
</html>
