<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Pricing.aspx.cs" Inherits="LABATRACK.admin.Pricing" %>

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
                    <asp:Literal ID="litNotice" runat="server" EnableViewState="false" />

                    <div class="grid grid-main-side">
                        <div class="stack">
                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>Services</h2>
                                        <div class="sub">Charged per kilo. Each service has its own stages. A service with a load still in the shop can't be changed until that load is claimed or voided.</div>
                                    </div>
                                    <button type="button" class="btn btn-sm" data-price-add="service"><%= Icons.Get("plus", "ico-sm") %>Add service</button>
                                </div>
                                <div class="table-wrap">
                                    <table class="table" data-price-table="service">
                                        <thead>
                                            <tr><th>Service</th><th class="right">Rate per kg</th><th>Stages</th><th>Status</th><th></th></tr>
                                        </thead>
                                        <tbody>
                                            <asp:Repeater ID="rptServices" runat="server">
                                                <ItemTemplate>
                                                    <tr data-id="<%# Eval("ServiceId") %>" data-stages="<%# Eval("StageCode") %>" class="<%# RowCss("service", (int)Eval("ServiceId")) %>">
                                                        <td class="cell-main<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="name"><%#: Eval("Name") %></td>
                                                        <td class="right num<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="price">₱<%# ((decimal)Eval("RatePerKg")).ToString("N2") %></td>
                                                        <td data-col="stages"><div class="stage-chips"><%# StageChips((string)Eval("StageCode")) %></div></td>
                                                        <td data-col="status">
                                                            <span class="pill <%# (bool)Eval("IsActive") ? "pill-ok" : "st-claimed" %>"><%# (bool)Eval("IsActive") ? "Active" : "Inactive" %></span>
                                                            <%# (bool)Eval("IsInUse") ? "<div class=\"in-use\">" + Icons.Get("lock", "ico-sm") + "In use &middot; " + Eval("LoadsInShop") + ((int)Eval("LoadsInShop") == 1 ? " load" : " loads") + " in the shop</div>" : "" %>
                                                        </td>
                                                        <td class="right">
                                                            <button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"<%# (bool)Eval("IsInUse") ? " disabled=\"disabled\" title=\"Can't be changed while a load using this service is in the shop\"" : "" %>><%# Icons.Get("pencil", "ico-sm") %>Edit</button>
                                                        </td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </tbody>
                                    </table>
                                </div>
                            </section>

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
                                            <asp:Repeater ID="rptServiceAddOns" runat="server">
                                                <ItemTemplate>
                                                    <tr data-id="<%# Eval("AddOnId") %>" class="<%# RowCss("service-addon", (int)Eval("AddOnId")) %>">
                                                        <td class="cell-main<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="name"><%#: Eval("Name") %></td>
                                                        <td class="right num<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="price">₱<%# ((decimal)Eval("Price")).ToString("N2") %></td>
                                                        <td data-col="status"><span class="pill <%# (bool)Eval("IsActive") ? "pill-ok" : "st-claimed" %>"><%# (bool)Eval("IsActive") ? "Active" : "Inactive" %></span></td>
                                                        <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%# Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
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
                                            <asp:Repeater ID="rptProducts" runat="server">
                                                <ItemTemplate>
                                                    <tr data-id="<%# Eval("AddOnId") %>" class="<%# RowCss("product", (int)Eval("AddOnId")) %>">
                                                        <td class="cell-main<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="name"><%#: Eval("Name") %></td>
                                                        <td class="right num<%# (bool)Eval("IsActive") ? "" : " muted" %>" data-col="price">₱<%# ((decimal)Eval("Price")).ToString("N2") %></td>
                                                        <td data-col="status"><span class="pill <%# (bool)Eval("IsActive") ? "pill-ok" : "st-claimed" %>"><%# (bool)Eval("IsActive") ? "Active" : "Inactive" %></span></td>
                                                        <td class="right"><button type="button" class="btn btn-sm btn-ghost" data-price-edit="true"><%# Icons.Get("pencil", "ico-sm") %>Edit</button></td>
                                                    </tr>
                                                </ItemTemplate>
                                            </asp:Repeater>
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
                                        <div class="input-prefix"><span>₱</span><asp:TextBox ID="txtMinimumCharge" runat="server" inputmode="decimal" MaxLength="9" /></div>
                                        <asp:Label ID="lblMinimumError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">A laundry charge below this is raised to it. Add-ons are added after.</span>
                                    </div>
                                    <div class="field">
                                        <label for="ddlRounding">Round weight up to the next</label>
                                        <asp:DropDownList ID="ddlRounding" runat="server" />
                                        <span class="hint">The weighed load is rounded up to a whole step. With a 0.1 kg step a 3.2 kg load bills as 3.2 kg; with 0.5 kg as 3.5 kg; with 1 kg as 4 kg.</span>
                                    </div>
                                    <div class="field">
                                        <label for="txtMaxWeight">Maximum weight per job (kg)</label>
                                        <asp:TextBox ID="txtMaxWeight" runat="server" inputmode="decimal" MaxLength="6" />
                                        <asp:Label ID="lblMaxWeightError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">A job heavier than this can't be saved. A bigger load is split into two or more jobs.</span>
                                    </div>
                                    <div class="field">
                                        <label for="txtMinDownPayment">Minimum down payment (% of the total)</label>
                                        <asp:TextBox ID="txtMinDownPayment" runat="server" inputmode="numeric" MaxLength="3" />
                                        <asp:Label ID="lblMinDownError" runat="server" CssClass="error-text" Visible="false" EnableViewState="false" />
                                        <span class="hint">The least paid at drop-off; the rest is paid before pick-up. 100 means full payment only, 0 lets a customer pay everything at pick-up.</span>
                                    </div>
                                </div>
                                <div class="card-f">
                                    <button type="submit" name="action" value="rules" class="btn btn-primary"><%= Icons.Get("save", "ico-sm") %>Save rules</button>
                                </div>
                            </section>

                            <section class="card">
                                <div class="card-h">
                                    <div>
                                        <h2>How a total is computed</h2>
                                        <div class="sub"><%: ExampleTitle %></div>
                                    </div>
                                </div>
                                <div class="card-b">
                                    <%= ExampleBreakdown %>
                                </div>
                            </section>
                        </div>
                    </div>
                </div>
            </main>
        </div>

        <dialog id="priceDialog" class="modal" aria-labelledby="priceDialogTitle">
            <input type="hidden" name="pdKind" id="pdKind" />
            <input type="hidden" name="pdId" id="pdId" />
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
                    <input type="text" id="pdName" name="pdName" maxlength="60" autocomplete="off" />
                    <span class="error-text" id="pdNameError" hidden></span>
                </div>
                <div class="field">
                    <label for="pdPrice" id="pdPriceLabel">Price</label>
                    <div class="input-prefix"><span>₱</span><input type="text" id="pdPrice" name="pdPrice" inputmode="decimal" autocomplete="off" /></div>
                    <span class="hint" id="pdPriceHint"></span>
                    <span class="error-text" id="pdPriceError" hidden></span>
                </div>
                <div class="field" id="pdStagesField" hidden>
                    <span class="label">Stages</span>
                    <div class="stage-picks">
                        <label class="check"><input type="checkbox" id="pdWash" name="pdWash" value="1" data-stage="Washing" />Washing</label>
                        <label class="check"><input type="checkbox" id="pdDry" name="pdDry" value="1" data-stage="Drying" />Drying</label>
                        <label class="check"><input type="checkbox" id="pdFold" name="pdFold" value="1" data-stage="Folding" />Folding</label>
                    </div>
                    <span class="hint route-preview" id="pdRoute"></span>
                    <span class="error-text" id="pdStagesError" hidden>Tick at least one stage.</span>
                </div>
                <div class="field">
                    <span class="label">Status</span>
                    <label class="check"><input type="checkbox" id="pdActive" name="pdActive" value="1" /><span><strong>Active</strong> <span class="muted small">&middot; offered on new job orders</span></span></label>
                    <span class="hint">Untick to stop offering it. It stays on every job that already used it.</span>
                </div>
                <div class="notice notice-info">
                    <%= Icons.Get("info", "ico-sm") %>
                    <span>A new price applies to new job orders only. Jobs already made keep the price they were created with.</span>
                </div>
            </div>
            <div class="modal-f">
                <button type="button" class="btn btn-ghost" data-modal-close="true">Cancel</button>
                <button type="submit" name="action" value="price" class="btn btn-primary" id="pdSave"><%= Icons.Get("save", "ico-sm") %><span id="pdSaveText">Save changes</span></button>
            </div>
        </dialog>
    </form>

    <script src="<%= AssetUrl.Get("~/assets/js/Loading.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/NumericInput.js") %>"></script>

    <script src="<%= AssetUrl.Get("~/assets/js/Sidebar.js") %>"></script>
    <script src="<%= AssetUrl.Get("~/assets/js/Pricing.js") %>"></script>
</body>
</html>
