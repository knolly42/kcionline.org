<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FollowUpMover.ascx.cs" Inherits="org_kcionline.FollowUp.FollowUpMover" %>
<%@ Register TagPrefix="KCIOnline" Assembly="org.kcionline.bricksandmortarstudio" Namespace="org.kcionline.bricksandmortarstudio.Web.UI" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            
            <Rock:NotificationBox runat="server" ID="nbInfo"></Rock:NotificationBox>
            <div class="panel-heading">
                <h1 class="panel-title">Move Follow Ups</h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-6">
                        <KCIOnline:FollowUpPersonPicker runat="server" Label="Follow Up" ID="ppFollowUp" Help="The person to move"  OnSelectPerson="ppFollowUp_OnSelectPerson" />
                        <Rock:RockLiteral runat="server" Visible="False" ID="lConsolidator" Label="Current Consolidator"></Rock:RockLiteral>
                    </div>
                    <div class="col-md-6">
                        <KCIOnline:LinePersonPicker runat="server" ID="ppNewConsolidator" Label="New Consolidator" Help="The person who should adopt the follow up" OnSelectPerson="ppNewConsolidator_OnSelectPerson"/>
                        <asp:LinkButton runat="server" ID="lbMove" CssClass="btn btn-primary" Enabled="False" Text="Move" OnClick="lbMove_OnClick" ></asp:LinkButton>
                    </div>
                </div>
            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>