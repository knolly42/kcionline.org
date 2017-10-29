<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FollowUps.ascx.cs" Inherits="org_kcionline.FollowUp.FollowUps" %>
<%@ Register TagPrefix="KCIOnline" Assembly="org.kcionline.bricksandmortarstudio" Namespace="org.kcionline.bricksandmortarstudio.Web.UI" %>

<asp:UpdatePanel ID="upnlFollowUpList" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading clearfix">
                <h1 class="panel-title"></h1>    
                <Rock:Toggle ID="tViewLineType" runat="server" Label="Showing" OnText="My Follow Ups" OffText="My Line's Follow Ups" Checked="true" />
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-12">
                        <KCIOnline:LinePersonPicker Label="Filter By Consolidator" runat="server" ID="ppConsolidator" Visible="True"/>
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
