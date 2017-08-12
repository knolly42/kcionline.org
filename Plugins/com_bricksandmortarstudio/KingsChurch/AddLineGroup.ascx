<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AddLineGroup.ascx.cs" Inherits="RockWeb.Plugins.com_bricksandmortarstudio.KingsChurch.AddLineGroup" %>
<%@ Register TagPrefix="KingsChurch" Assembly="org.kcionline.bricksandmortarstudio" Namespace="org.kcionline.bricksandmortarstudio.Web.UI" %>

<script type="text/javascript">
    function clearActiveDialog() {
        $('#<%=hfActiveDialog.ClientID %>').val('');
    }

    Sys.Application.add_load(function () {
        $('.js-follow-status').tooltip();
    });
</script>

<asp:UpdatePanel ID="upnlGroupDetail" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlDetails" CssClass="js-group-panel" runat="server">
            <asp:HiddenField ID="hfGroupId" runat="server" />

            <div class="panel panel-block">

                <div class="panel-heading panel-follow clearfix">
                    <h1 class="panel-title pull-left">
                        <asp:Literal ID="lReadOnlyTitle" runat="server" />
                    </h1>
                </div>

                <div class="panel-body">
                    <Rock:NotificationBox ID="nbMessage" runat="server" NotificationBoxType="Danger" Visible="false" />
                    <asp:ValidationSummary ID="vsGroup" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                    <asp:CustomValidator ID="cvGroup" runat="server" Display="None" />

                    <div id="pnlEditDetails" runat="server">

                        <div class="row">
                            <div class="col-md-6">
                                <KingsChurch:LinePersonPicker ID="lppLeader" runat="server" Label="Leader" Required="true" />
                                <div class="grid">
                                    <Rock:Grid ID="gLocations" runat="server" AllowPaging="false" DisplayType="Light" RowItemText="Location">
                                        <Columns>
                                            <Rock:RockBoundField DataField="Location" HeaderText="Location" />
                                            <Rock:RockBoundField DataField="Type" HeaderText="Type" />
                                            <Rock:RockBoundField DataField="Schedules" HeaderText="Schedule(s)" />
                                            <Rock:EditField OnClick="gLocations_Edit" />
                                            <Rock:DeleteField OnClick="gLocations_Delete" />
                                        </Columns>
                                    </Rock:Grid>
                                </div>

                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:RockRadioButtonList ID="rblScheduleSelect" runat="server" Label="Group Schedule" CssClass="margin-b-sm" OnSelectedIndexChanged="rblScheduleSelect_SelectedIndexChanged" AutoPostBack="true" RepeatDirection="Horizontal" />
                                    </div>
                                    <div class="col-md-6">
                                        <div class="row">
                                            <div class="col-sm-6">
                                                <Rock:DayOfWeekPicker ID="dowWeekly" runat="server" CssClass="input-width-md" Visible="false" Label="Day of the Week" />
                                            </div>
                                            <div class="col-sm-6">
                                                <Rock:TimePicker ID="timeWeekly" runat="server" Visible="false" Label="Time of Day" />
                                            </div>
                                        </div>
                                        <Rock:SchedulePicker ID="spSchedule" runat="server" AllowMultiSelect="false" Visible="false" Label="Named Schedule" />
                                        <asp:HiddenField ID="hfUniqueScheduleId" runat="server" />
                                        <Rock:ScheduleBuilder ID="sbSchedule" runat="server" ShowDuration="false" ShowScheduleFriendlyTextAsToolTip="true" Visible="false" Label="Custom Schedule" />
                                    </div>
                                </div>
                                <Rock:DatePicker ID="dpStartDate" runat="server" Label="Start Date" />
                                <Rock:DataTextBox ID="tbDescription" runat="server" SourceTypeName="Rock.Model.Group, Rock" PropertyName="Description" TextMode="MultiLine" Rows="4" />

                                <Rock:PanelWidget ID="wpGroupAttributes" runat="server" Title="Group Attribute Values">
                                    <asp:PlaceHolder ID="phGroupAttributes" runat="server" EnableViewState="false"></asp:PlaceHolder>
                                </Rock:PanelWidget>
                            </div>
                        </div>

                        <div class="actions">
                            <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        </div>

                    </div>
                </div>
            </div>

        </asp:Panel>

        <asp:Panel ID="pnlSuccess" CssClass="js-group-panel" runat="server">
            <Rock:NotificationBox ID="nbSuccess" NotificationBoxType="Success" runat="server" />
        </asp:Panel>

        <asp:HiddenField ID="hfActiveDialog" runat="server" />

        <!-- Locations Modal Dialog -->
        <Rock:ModalDialog ID="dlgLocations" runat="server" Title="Group Location" OnSaveClick="dlgLocations_SaveClick" OnCancelScript="clearActiveDialog();" ValidationGroup="Location">
            <Content>

                <asp:HiddenField ID="hfAddLocationGroupGuid" runat="server" />

                <asp:ValidationSummary ID="valLocationSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" ValidationGroup="Location" />

                <ul id="ulNav" runat="server" class="nav nav-pills">
                    <asp:Repeater ID="rptLocationTypes" runat="server">
                        <ItemTemplate>
                            <li class='<%# GetTabClass(Container.DataItem) %>'>
                                <asp:LinkButton ID="lbLocationType" runat="server" Text='<%# Container.DataItem %>' OnClick="lbLocationType_Click" CausesValidation="false">
                                </asp:LinkButton>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>

                <div class="tabContent">
                    <asp:Panel ID="pnlMemberSelect" runat="server" Visible="true">
                        <Rock:RockDropDownList ID="ddlMember" runat="server" Label="Member" ValidationGroup="Location" />
                    </asp:Panel>
                    <asp:Panel ID="pnlLocationSelect" runat="server" Visible="false">
                        <Rock:LocationPicker ID="locpGroupLocation" runat="server" Label="Location" ValidationGroup="Location" />
                    </asp:Panel>
                </div>

                <Rock:RockDropDownList ID="ddlLocationType" runat="server" Label="Type" DataValueField="Id" DataTextField="Value" ValidationGroup="Location" />

                <Rock:SchedulePicker ID="spSchedules" runat="server" Label="Schedule(s)" ValidationGroup="Location" AllowMultiSelect="true" />

            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
