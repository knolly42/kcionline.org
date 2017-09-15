<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ImportFollowUps.ascx.cs" Inherits="org_kcionline.FollowUp.FollowUpImport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            
            <Rock:NotificationBox runat="server" ID="nbInfo"></Rock:NotificationBox>
            <div class="panel-heading">
                <h1 class="panel-title">Import Follow Ups</h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-12">
                        <Rock:BootstrapButton ID="lbImport" CssClass="btn btn-primary" Text="Import" OnClick="lbImport_OnClick" DataLoadingText="Importing"  runat="server" />
                    </div>
                </div>
            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>