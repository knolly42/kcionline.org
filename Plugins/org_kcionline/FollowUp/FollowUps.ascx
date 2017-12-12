<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FollowUps.ascx.cs" Inherits="org_kcionline.FollowUp.FollowUps" %>
<%@ Register TagPrefix="KCIOnline" Assembly="org.kcionline.bricksandmortarstudio" Namespace="org.kcionline.bricksandmortarstudio.Web.UI" %>

<asp:UpdatePanel ID="upnlFollowUpList" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading clearfix">
                <h1 class="panel-title"><asp:Label runat="server" Text="<%# ddlChoices.SelectedValue %>"></asp:Label></h1>    
            </div>
            <div class="panel-body">
                <div class="row" runat="server" ID="leaderControlRow">
                    <div class="col-md-4">
                        <KCIOnline:LinePersonPicker Label="Filter" runat="server" ID="ppFilter" OnSelectPerson="ppFilter_OnSelectPerson"/>
                    </div>
                    <div class="col-md-4">
                        <Rock:RockDropDownList runat="server" ID="ddlChoices" OnSelectedIndexChanged="ddlChoices_OnSelectedIndexChanged"/>
                    </div>
                    <div class="col-md-4">
                        <Rock:Toggle ID="tViewLineType" Visible="True" runat="server" Label="Showing" OnText="My Follow Ups" OffText="My Line's Follow Ups" Checked="true" OnCheckedChanged="tViewLineType_OnCheckedChanged"   />
                    </div>
                    <div class="col-md-12">
                        <asp:Literal ID="lContent" runat="server"></asp:Literal>
                    </div>
                </div>
            </div>
         </div>
        <asp:Literal ID="lDebug" runat="server"></asp:Literal>
    </ContentTemplate>
</asp:UpdatePanel>
