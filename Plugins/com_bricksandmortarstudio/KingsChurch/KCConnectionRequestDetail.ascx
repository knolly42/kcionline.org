﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KCConnectionRequestDetail.ascx.cs" Inherits="RockWeb.Plugins.KingsChurch.KCConnectionRequestDetail" %>
<%@ Register TagPrefix="KingsChurch" Assembly="org.kcionline.bricksandmortarstudio" Namespace="org.kcionline.bricksandmortarstudio.Web.UI" %>

<asp:UpdatePanel ID="upDetail" runat="server">
    <ContentTemplate>

        <asp:HiddenField ID="hfConnectionOpportunityId" runat="server" />
        <asp:HiddenField ID="hfConnectionRequestId" runat="server" />
        <asp:HiddenField ID="hfActiveDialog" runat="server" />

        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lConnectionOpportunityIconHtml" runat="server" />
                    <asp:Literal ID="lTitle" runat="server" />
                </h1>
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlCampus" runat="server" LabelType="Campus" />
                    <Rock:HighlightLabel ID="hlOpportunity" runat="server" LabelType="Info" />
                    <Rock:HighlightLabel ID="hlStatus" runat="server" LabelType="Default" Visible="false" />
                    <Rock:HighlightLabel ID="hlState" runat="server" Visible="false" />
                </div>
            </div>

            <asp:Panel ID="pnlReadDetails" runat="server">

                <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>

                <div class="panel-body">

                    <div class="row">
                        <div class="col-md-6">
                            <div class="row">
                                <div class="col-md-4">
                                    <div class="photo">
                                        <asp:Literal ID="lPortrait" runat="server" />
                                    </div>
                                </div>
                                <div class="col-md-8">
                                    <Rock:RockLiteral ID="lContactInfo" runat="server" Label="Contact Info" />
                                    <Rock:RockLiteral ID="lConnector" runat="server" Label="Connector" />
                                </div>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <Rock:RockLiteral ID="lRequestDate" runat="server" Label="Request Date" />
                            <Rock:RockLiteral ID="lPlacementGroup" runat="server" Label="Placement Group" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:RockLiteral ID="lComments" runat="server" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:ModalAlert ID="mdWorkflowLaunched" runat="server" />
                            <asp:Label ID="lblWorkflows" Text="Available Workflows" Font-Bold="true" runat="server" />
                            <div class="margin-b-md">
                                <asp:Repeater ID="rptRequestWorkflows" runat="server">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbRequestWorkflow" runat="server" CssClass="btn btn-default btn-xs" CommandArgument='<%# Eval("Id") %>' CommandName="LaunchWorkflow">
                                        <%# Eval("WorkflowType.Name") %>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <asp:Panel ID="pnlRequirements" runat="server">
                                <Rock:RockControlWrapper ID="rcwRequirements" runat="server" Label="Group Requirements">
                                    <Rock:NotificationBox ID="nbRequirementsErrors" runat="server" Dismissable="true" NotificationBoxType="Warning" />
                                    <Rock:RockCheckBoxList ID="cblManualRequirements" RepeatDirection="Vertical" runat="server" Label="" />
                                    <div class="labels">
                                        <asp:Literal ID="lRequirementsLabels" runat="server" />
                                    </div>
                                </Rock:RockControlWrapper>
                            </asp:Panel>
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="lbEdit" runat="server" Text="Edit" CssClass="btn btn-primary" OnClick="lbEdit_Click"></asp:LinkButton>
                        <asp:LinkButton ID="lbTransfer" runat="server" Text="Transfer" CssClass="btn btn-link" CausesValidation="false" OnClick="lbTransfer_Click"></asp:LinkButton>
                        <div class="pull-right">
                            <asp:LinkButton ID="lbConnect" runat="server" Text="Connect" CssClass="btn btn-success" CausesValidation="false" OnClick="lbConnect_Click"></asp:LinkButton>
                        </div>
                    </div>

                </div>

            </asp:Panel>

            <asp:Panel ID="pnlEditDetails" runat="server" Visible="false">

                <div class="panel-body">

                    <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                    <asp:CustomValidator ID="cvConnectionRequest" runat="server" Display="None" />
                    <Rock:NotificationBox ID="nbErrorMessage" runat="server" NotificationBoxType="Danger" />
                    <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral runat="server" ID="lRequestor" Label="Requestor" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lConnectorEdit" runat="server" Label="Connector" Visible="false" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:GroupPicker ID="gpGroup" runat="server" Label="Group" />
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click"></asp:LinkButton>
                        <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" OnClick="btnCancel_Click" CausesValidation="false"></asp:LinkButton>
                    </div>

                </div>

            </asp:Panel>

            <asp:Panel ID="pnlTransferDetails" runat="server" Visible="false">

                <div class="panel-body">
                    <div class="row">
                        <div class="col-md-6">
                           <KingsChurch:LinePersonPicker ID="ppConnector" runat="server" />
                        </div>
                    </div>
                    </br>
                    <div class="actions">
                        <asp:LinkButton ID="btnTransferSave" runat="server" AccessKey="s" Text="Transfer" CssClass="btn btn-primary" OnClick="btnTransferSave_Click"></asp:LinkButton>
                        <asp:LinkButton ID="btnTransferCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" OnClick="btnCancel_Click" CausesValidation="false"></asp:LinkButton>
                    </div>

                </div>
        </asp:Panel>

        </div>

        <Rock:PanelWidget ID="wpConnectionRequestWorkflow" runat="server" Title="Workflows" CssClass="clickable">
            <div class="grid">
                <Rock:Grid ID="gConnectionRequestWorkflows" runat="server" AllowPaging="false" DisplayType="Light" RowItemText="Location" OnRowSelected="gConnectionRequestWorkflows_RowSelected">
                    <Columns>
                        <Rock:RockBoundField DataField="WorkflowType" HeaderText="Workflow Type" />
                        <Rock:RockBoundField DataField="Trigger" HeaderText="Trigger" />
                        <Rock:RockBoundField DataField="CurrentActivity" HeaderText="Current Activity" />
                        <Rock:RockBoundField DataField="Date" HeaderText="Start Date" />
                        <Rock:RockBoundField DataField="Status" HeaderText="Status" HtmlEncode="false" />
                    </Columns>
                </Rock:Grid>
            </div>
        </Rock:PanelWidget>

        <Rock:PanelWidget ID="wpConnectionRequestActivities" runat="server" Title="Activities" Expanded="true" CssClass="clickable">
            <div class="grid">
                <Rock:Grid ID="gConnectionRequestActivities" runat="server" AllowPaging="false" DisplayType="Light"
                    RowItemText="Activity" OnRowDataBound="gConnectionRequestActivities_RowDataBound" OnRowSelected="gConnectionRequestActivities_Edit">
                    <Columns>
                        <Rock:RockBoundField DataField="Date" HeaderText="Date" />
                        <Rock:RockBoundField DataField="Activity" HeaderText="Activity" />
                        <Rock:RockBoundField DataField="Opportunity" HeaderText="Opportunity" />
                        <Rock:RockBoundField DataField="Connector" HeaderText="Connector" />
                        <Rock:RockBoundField DataField="Note" HeaderText="Note" />
                        <Rock:DeleteField OnClick="gConnectionRequestActivities_Delete" />
                    </Columns>
                </Rock:Grid>
            </div>
        </Rock:PanelWidget>

        <Rock:ModalDialog ID="dlgConnectionRequestActivities" runat="server" SaveButtonText="Add" OnSaveClick="btnAddConnectionRequestActivity_Click" Title="Add Activity" ValidationGroup="Activity">
            <Content>
                <asp:ValidationSummary ID="valConnectorGroup" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" ValidationGroup="Activity" />
                <asp:HiddenField ID="hfAddConnectionRequestActivityGuid" runat="server" />
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockDropDownList ID="ddlActivity" runat="server" Label="Activity" Required="true" ValidationGroup="Activity" />
                    </div>
                    <div class="col-md-6">
                        <Rock:RockDropDownList ID="ddlActivityConnector" runat="server" Label="Connector" Required="true" ValidationGroup="Activity" />
                    </div>
                </div>
                <Rock:RockTextBox ID="tbNote" runat="server" Label="Note" TextMode="MultiLine" Rows="4" ValidationGroup="Activity" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="dlgSearch" runat="server" ValidationGroup="Search" SaveButtonText="Search" OnSaveClick="dlgSearch_SaveClick" Title="Search Opportunities">
            <Content>
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockTextBox ID="tbSearchName" runat="server" Label="Name" />
                        <Rock:RockCheckBoxList ID="cblCampus" runat="server" Label="Campuses" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" />
                        <asp:PlaceHolder ID="phAttributeFilters" runat="server" />
                    </div>
                    <div class="col-md-6">
                        <asp:Repeater ID="rptSearchResult" runat="server">
                            <ItemTemplate>
                                <Rock:PanelWidget ID="SearchTermPanel" runat="server" CssClass="panel panel-block" TitleIconCssClass='<%# Eval("IconCssClass") %>' Title='<%# Eval("Name") %>'>
                                    <div class="row">
                                        <div class="col-md-4">
                                            <div class="photo">
                                                <img src='<%# Eval("PhotoUrl") %>'></img>
                                            </div>
                                        </div>
                                        <div class="col-md-8">
                                            <%# Eval("Description") %>
                                            </br>                                                
                                            <Rock:BootstrapButton ID="btnSearchSelect" runat="server" CommandArgument='<%# Eval("Id") %>' CommandName="Display" Text="Select" CssClass="btn btn-default btn-sm" />
                                        </div>
                                    </div>
                                </Rock:PanelWidget>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>