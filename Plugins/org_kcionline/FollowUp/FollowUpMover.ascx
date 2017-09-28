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
                        <div class="row">
                            <div class="col-md-12">
                                <ul class="nav nav-pills" >
                                    <li role="presentation" runat="server" id="lMyLine" class="active"><asp:LinkButton runat="server" OnClick="ChangeToMyLine">My Line</asp:LinkButton></li>
                                    <li role="presentation" runat="server" id="lAnotherLine" class=""><asp:LinkButton runat="server" OnClick="ChangeToAnotherLine">Another Line</asp:LinkButton></li>
                                </ul>
                            </div>
                        </div>
                        <div class="col-md-12">
                            <div id="lineTab" runat="server" Visible="True">
                                <KCIOnline:LinePersonPicker runat="server" ID="ppNewConsolidator" Label="New Consolidator" Help="The person who should adopt the follow up" OnSelectPerson="ppNewConsolidator_OnSelectPerson"/>
                                <asp:LinkButton runat="server" ID="lbMove" CssClass="btn btn-primary" Enabled="False" Text="Move" OnClick="lbTransferMyLine_OnClick" ></asp:LinkButton>
                            </div>
                             <div id="anotherLineTab" runat="server" Visible="False">
                                <KCIOnline:ConsolidatorLeaderPicker runat="server" ID="ppAnotherLineNewConsolidator" Label="New Consolidator" Help="The person who should adopt the follow up" OnSelectPerson="ppNewConsolidator_OnSelectPerson"/>
                                <asp:LinkButton runat="server" ID="lbTransfer" CssClass="btn btn-primary" Enabled="False" Text="Move" OnClick="lbTransferAnotherLine_OnClick" ></asp:LinkButton>
                            </div>
                        </div>
                    </div>
                    
                </div>
            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>